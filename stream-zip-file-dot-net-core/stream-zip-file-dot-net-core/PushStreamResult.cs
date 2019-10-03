﻿namespace stream_zip_file_dot_net_core
{
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class PushStreamResult: FileResult
    {
        private readonly Func<Stream, Task> _onStreamAvailabe;

        private readonly string _fileDownloadName;

        public PushStreamResult(Func<Stream, Task> onStreamAvailabe,
            string contentType, string fileDownloadName) : base(contentType)
        {
            _onStreamAvailabe = onStreamAvailabe;

            _fileDownloadName = string.IsNullOrEmpty(fileDownloadName) ? Guid.NewGuid().ToString("n") : fileDownloadName;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;

            response.Headers["Content-Disposition"] = $"attachment; filename= {_fileDownloadName}";

            response.ContentType = ContentType.ToString();

            await _onStreamAvailabe(context.HttpContext.Response.Body);

        }
    }
}
