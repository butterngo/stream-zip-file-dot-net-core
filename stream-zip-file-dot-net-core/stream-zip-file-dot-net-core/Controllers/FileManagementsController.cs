namespace stream_zip_file_dot_net_core.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;

    [Route("api")]
    [ApiController]
    public class FileManagementsController : ControllerBase
    {
        private readonly string _directoryPath;

        private readonly IList<FileDownload> _listFiles;

        private readonly HttpClient _client;

        public FileManagementsController(IHostingEnvironment env)
        {
            _directoryPath = $@"{env.ContentRootPath}\temp";

            _listFiles = new List<FileDownload>
            {
                new FileDownload("https://drive.google.com/file/d/1nUkQE_6brI5Qa2xqVYXdhf0K9uXHQxLv/view", "onedrive_1.json"),
                new FileDownload("https://drive.google.com/file/d/1nUkQE_6brI5Qa2xqVYXdhf0K9uXHQxLv/view", "onedrive_2.json"),
                new FileDownload("https://drive.google.com/file/d/1nUkQE_6brI5Qa2xqVYXdhf0K9uXHQxLv/view", "onedrive_3.json"),
                new FileDownload($@"{_directoryPath}\1.xlsx", "internal_1.xlsx", isInternal:true),
                new FileDownload($@"{_directoryPath}\2.xlsx", "internal_2.xlsx", isInternal:true),
                new FileDownload($@"{_directoryPath}\3.xlsx", "internal_3.xlsx", isInternal:true)
            };

            _client = new HttpClient();
        }

        [HttpGet("download-zip-file")]
        public IActionResult DownloadZipFile()
        {
            var tasks = new List<Task<FileDownload>>();

            tasks.AddRange(_listFiles
                .Where(x => x.IsInternal == false)
                .Select(file => DownloadFromOneDriveAsync(file)));

            tasks.AddRange(_listFiles
                .Where(x => x.IsInternal)
                .Select(file => Task.Run(() =>
                {
                    file.Buffer = StreamFile(file.Url);

                    return file;
                })));

            return new PushStreamResult(async (outputStream) =>
            {
                using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
                {
                    foreach (var task in tasks)
                    {
                        var data = await task;

                        ZipArchiveEntry zipEntry = zipArchive.CreateEntry(data.FileName);
                        using (var zipStream = zipEntry.Open())
                        using (var stream = new MemoryStream(data.Buffer))
                            await stream.CopyToAsync(zipStream);
                    }
                }
            },
            "application/octet-stream",
            $"{Guid.NewGuid().ToString("n")}.zip");
        }

        private async Task<FileDownload> DownloadFromOneDriveAsync(FileDownload file)
        {
            HttpResponseMessage responseMessage = await _client.GetAsync(file.Url);

            var stream = await responseMessage.Content.ReadAsStreamAsync();

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                file.Buffer = ms.ToArray();
            }

            return file;
        }

        private byte[] StreamFile(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

            byte[] bytes = new byte[fs.Length];

            fs.Read(bytes, 0, System.Convert.ToInt32(fs.Length));

            fs.Close();
            return bytes;
        }
    }

    internal class FileDownload
    {
        public FileDownload() { }

        public FileDownload(string url, string fileName, bool isInternal = false)
        {
            Url = url;
            FileName = fileName;
            IsInternal = isInternal;
        }

        public byte[] Buffer { get; set; }

        public string Url { get; set; }

        public string FileName { get; set; }

        public bool IsInternal { get; set; }
    }
}
