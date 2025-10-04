using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using MiniHttpServer.Sharer;

namespace MiniHttpServer.Sharer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string settings = File.ReadAllText("settings.json");
            SettingsModel settingsModel = JsonSerializer.Deserialize<SettingsModel>(settings);

            var server = new HttpServer();
            var serverTask = server.Start();

            Thread consoleThread = new Thread(() =>
            {
                if (Console.ReadLine() == "stop")
                {
                    server.Stop();
                }
            });
            
            consoleThread.Start();
            await serverTask;
        }
    }
}