using MiniHttpServer.Core;
using MiniHttpServer.Core.handlers;
using MiniHttpServer.Sharer.Core.abstracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;

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

                Handler endpointHandler = new EndpointHandler();
                Handler staticFilesHandler = new StaticFilesHandler();
                endpointHandler.Successor = staticFilesHandler;

                Server.Prefixes.Add($"http://{settingsModel.SettingModel.Domain}:{settingsModel.SettingModel.Port}/");
                Server.Start();

                Console.WriteLine("Server is started");
                Console.WriteLine("Server is awaiting for request");

                while (!CycleBreakFlag)
                {
                    var context = await Server.GetContextAsync();

                    await endpointHandler.HandleRequest(context);
                    Console.WriteLine("Server is awaiting for request");
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}