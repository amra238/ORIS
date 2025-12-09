using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public SettingSample SettingModel { get; private set; }

        private SettingsModel()
        {
            LoadSettings();
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
                SettingModel = JsonSerializer.Deserialize<SettingSample>(settings);
                if (SettingModel != null)
                {
                    var validationContext = new ValidationContext(SettingModel);
                    var validationResults = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(SettingModel, validationContext, validationResults, true))
                    {
                        foreach (var result in validationResults)
                        {
                            if (result.ErrorMessage != string.Empty)
                                throw new ArgumentException(result.ErrorMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"settings file loading error: {ex.Message}");
            }
        }
    }

    public class SettingSample
    {
        public string StaticDirectoryPath { get; set; } = "static/";
        public string Domain { get; set; } = "localhost";
        public string Port { get; set; } = "1234";
        public SmtpSettings SmtpSettings { get; set; } = new SmtpSettings();
        public string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=usersdb;" +
            "Integrated Security=True;TrustServerCertificate=true";
    }

    public class SmtpSettings
    {
        public string Server { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}