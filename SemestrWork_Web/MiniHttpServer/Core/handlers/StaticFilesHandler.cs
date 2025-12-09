using HtmlAgilityPack;
using MiniHttpServer.Sharer;
using MiniHttpServer.Sharer.Core.abstracts;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.handlers
{
    internal class StaticFilesHandler : Handler
    {
        public override async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string path = request.Url.AbsolutePath;

                if (path == "" || path == "/")
                    path = "/static/index.html";

                if (path[path.Length - 1] == '/')
                    path += "index.html";

                path = "." + path;

                if (!File.Exists(path))
                {
                    response.StatusCode = 404;
                    await WriteResponse(response, "File not found");
                    return;
                }

                var file = new FileInfo(path);
                response.ContentType = ContentType.GetContentType(file.Extension.TrimStart('.'));
                response.ContentLength64 = file.Length;
                response.Headers.Add("Server", "MyHttpServer/1.0");

                using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await fileStream.CopyToAsync(response.OutputStream);
                await response.OutputStream.FlushAsync();
                response.Close();

                Console.WriteLine($"Static file served: {path}");
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await WriteResponse(response, $"Error: {ex.Message}");
                Console.WriteLine($"Error serving static file: {ex.Message}");
            }
        }

        private async Task WriteResponse(HttpListenerResponse response, string message)
        {
            response.ContentType = "text/plain; charset=utf-8";
            var buffer = Encoding.UTF8.GetBytes(message);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}