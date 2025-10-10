using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Sharer
{
    internal class HttpServer
    {
        private static HttpListener Server { get; set; }

        static bool CycleBreakFlag { get; set; } = false;        

        public void Stop()
        {
            Server?.Stop();
            Console.WriteLine("Server is stopped");
            CycleBreakFlag = true;
        }

        public async Task Start()
        {
            try
            {
                var settingsModel = SettingsModel.Instance;
                Server = new HttpListener();

                // установка адресов прослушки
                Server.Prefixes.Add("http://" + settingsModel.Domain + ":" + settingsModel.Port + "/");
                Server.Start(); // начинаем прослушивать входящие подключения

                Console.WriteLine("Server is started");
                Console.WriteLine("Server is awaiting for request");

                while (!CycleBreakFlag)
                {                    
                    // получаем контекст
                    var context = await Server.GetContextAsync();
                    var response = context.Response;
                    var request = context.Request;
                    // отправляемый в ответ код html возвращает

                    try
                    {
                        string requestedPath = request.Url.AbsolutePath;

                        if (requestedPath[requestedPath.Length - 1] == '/')
                            requestedPath += "index.html";

                        if (requestedPath == "" || requestedPath == "/")
                            requestedPath = "static/index.html";

                        requestedPath = requestedPath.Substring(1);

                        if (!File.Exists(requestedPath))
                        {
                            response.StatusCode = 404;
                            string errorHtml = "<h1>404 Not Found</h1><p>Resource not found.</p>";
                            byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
                            response.ContentType = "text/html; charset=utf-8";
                            response.ContentLength64 = buffer.Length;
                            using Stream output = response.OutputStream;
                            await output.WriteAsync(buffer);
                            await output.FlushAsync();

                            Console.WriteLine($"Файл не найден: {requestedPath}");
                            continue;
                        }

                        var file = new FileInfo(requestedPath);
                        response.ContentType = ContentType.GetContentType(file.Extension.Substring(1));

                        response.ContentLength64 = file.Length;
                        response.Headers.Add("Server", "MyHttpServer/1.0");
                        var fileStream = new FileStream(requestedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        using (fileStream)
                        {
                            using Stream output = response.OutputStream;
                            await fileStream.CopyToAsync(output);
                            await output.FlushAsync();
                            Console.WriteLine("Request has been processed");
                        }

                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine("static folder not found");
                        Stop();
                    }
                    catch (FileNotFoundException e)
                    {
                        Console.WriteLine("index.html is not found in static folder");
                        Stop();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Stop();
                    }
                }
            }
            catch (Exception e) when (e is DirectoryNotFoundException or FileNotFoundException)
            {
                Console.WriteLine("settings are not found");
            }
            catch (JsonException e)
            {
                Console.WriteLine("settings.json is incorrect");
            }
            catch (Exception e) { Console.WriteLine(e.Message); }
        }
    }
}