using System.Net;
using System.Net.Mail;
using MiniHttpServer.Sharer;

namespace MiniHttpServer.Sharer.Services
{
    internal class EmailService
    {
        public static void SendEmail(string to, string subject, string message)
        {
            try
            {
                var smtpSettings = SettingsModel.Instance.SettingModel.SmtpSettings;
                string? zipFile = FindZipFile("Attachments");

                if (zipFile == null)
                {
                    throw new FileNotFoundException(".zip file don't exist in Attachments folder");
                }

                Console.WriteLine($"Found ZIP file: {zipFile}");

                using var smtpClient = new SmtpClient(smtpSettings.Server, smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(smtpSettings.Email, smtpSettings.Password),
                    EnableSsl = smtpSettings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 10000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpSettings.Email),
                    Subject = subject,
                    Body = message
                };

                mailMessage.To.Add(to);
                mailMessage.Attachments.Add(new Attachment(zipFile));

                Console.WriteLine($"Sending email to: {to}");
                smtpClient.Send(mailMessage);
                Console.WriteLine($"Email sent successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                throw;
            }
        }

        private static string? FindZipFile(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Console.WriteLine($"Folder not found: {folder}");
                return null;
            }

            var zipFiles = Directory.GetFiles(folder, "*.zip");
            return zipFiles.Length > 0 ? zipFiles[0] : null;
        }
    }
}