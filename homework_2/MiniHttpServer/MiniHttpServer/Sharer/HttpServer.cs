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

        //private readonly static Dictionary<string, string> DictExtension = new Dictionary<string, string>()
        //{
        //    {"html", "text/html"}
        //};

        //public static HttpServer serv = new HttpServer();

        //private HttpServer() 
        //{

        //}

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
                string settings = File.ReadAllText("settings.json");
                SettingsModel settingsModel = JsonSerializer.Deserialize<SettingsModel>(settings);

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

                    // отправляемый в ответ код html возвращает
                    try
                    {
                        //var request = context.Request;
                        //var path = "." + request.Url.AbsolutePath;
                        //var file = new FileInfo(path);
                        //if (DictExtension.ContainsKey(file.Extension.Substring(1)))
                        //    context.Response.ContentType = DictExtension[file.Extension.Substring(1)];

                        var request = context.Request;
                        string requestedPath = request.Url.AbsolutePath;

                        if (requestedPath == "/" || requestedPath == "")
                        {

                            string responseText = File.ReadAllText(settingsModel.StaticDirectoryPath + "index.html");
                            byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                            response.ContentType = "text/html; charset=utf-8";
                            response.ContentLength64 = buffer.Length;
                            using Stream output = response.OutputStream;
                            await output.WriteAsync(buffer);
                            await output.FlushAsync();

                            Console.WriteLine("Запрос обработан");
                            Console.WriteLine("Server is awaiting for request");
                        }
                        else
                        {
                            response.StatusCode = 404;
                            string errorHtml = "<h1>404 Not Found</h1><p>Only root path is supported.</p>";
                            byte[] buffer = Encoding.UTF8.GetBytes(errorHtml);
                            response.ContentType = "text/html; charset=utf-8";
                            response.ContentLength64 = buffer.Length;
                            using Stream output = response.OutputStream;
                            await output.WriteAsync(buffer);
                            await output.FlushAsync();
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