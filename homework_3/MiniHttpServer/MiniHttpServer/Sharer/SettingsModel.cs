using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Sharer
{
    internal class SettingsModel
    {
        private static SettingsModel? _instance;
        private static readonly object _lock = new();

        public string StaticDirectoryPath { get; set; } = "static/";
        public string Domain { get; set; } = "localhost";
        public string Port { get; set; } = "1234";

        public SettingsModel()
        {            
        }

        public static SettingsModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new SettingsModel();
                            _instance.LoadSettings();
                        }
                    }
                }
                return _instance;
            }
        }

        private void LoadSettings()
        {
            try
            {
                string settings = File.ReadAllText("settings.json");
                var model = JsonSerializer.Deserialize<SettingsModel>(settings);
                if (model != null)
                {
                    StaticDirectoryPath = model.StaticDirectoryPath;
                    Domain = model.Domain;
                    Port = model.Port;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration loading error: {ex.Message}");
            }
        }
    }
}
