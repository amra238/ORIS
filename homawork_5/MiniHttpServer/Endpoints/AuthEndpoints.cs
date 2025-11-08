using MiniHttpServer.Sharer.Core.Attributes;
using MiniHttpServer.Sharer.Services;
using System.Net;
using System.Text;

namespace MiniHttpServer.Sharer.Endpoints
{
    [EndPoint]
    internal class AuthEndpoint
    {
        [HttpPost("login")]
        public void Login(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string responseText = "";

            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();

                var formData = System.Web.HttpUtility.ParseQueryString(body);
                string email = formData["email"] ?? "";
                string password = formData["password"] ?? "";

                Console.WriteLine($"Form data - Email: {email}, Password: {password}");

                if (string.IsNullOrEmpty(email))
                {
                    responseText = "Error: Email is required";
                }
                else
                {
                    string subject = "ORIS project file";
                    string message = $"Gimadeev Amirkhan 11-408";

                    EmailService.SendEmail(email, subject, message);
                    responseText = "Login successful - email sent";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Login: {ex.Message}");                
            }
            finally
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                    response.ContentType = "text/plain; charset=utf-8";
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();

                    Console.WriteLine($"Response sent: {responseText}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send response: {ex.Message}");
                }
            }
        }
    }
}