using Microsoft.Data.SqlClient;
using MiniHttpServer.Core.abstracts;
using MiniHttpServer.Core.Models;
using MiniHttpServer.Core.Services;
using MiniHttpServer.Shared;
using MiniHttpServer.Sharer.Core.Attributes;
using MyORMLibrary;
using Npgsql;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace MiniHttpServer.Endpoints
{
    [EndPoint]
    internal class HomeControllerEndpoint
    {
        private readonly ORMContext _databaseService;
        private readonly IHtmlTemplateRenderer _templateRenderer;
        private readonly AuthService _authService;

        public HomeControllerEndpoint()
        {
            var connectionString = "Host=localhost;Port=5433;Database=usersdb;Username=postgres;Password=postgres";
            _databaseService = new ORMContext(connectionString);
            _templateRenderer = new HtmlTemplateRenderer();
            _authService = new AuthService(_databaseService);
            
            _databaseService.Create<User>("Users");
            _databaseService.Create<Session>("Sessions");
        }

        [HttpGet]
        public string Index()
        {
            try
            {
                var tours = _databaseService.ReadByAll<Tour>("Tours");
                var templatePath = "static/index.html";
                return _templateRenderer.RenderFromFile(templatePath, new { HotTours = tours });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в Index(): {ex.Message}");
                return $"<h1>Ошибка загрузки страницы</h1><p>{ex.Message}</p>";
            }
        }

        [HttpPost("auth")]
        public void HandleAuth(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                var username = data.GetValueOrDefault("username") ?? "";
                var email = data.GetValueOrDefault("email") ?? "";
                var password = data.GetValueOrDefault("password") ?? "";

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    SendJsonResponse(response, new { success = false, message = "Email и пароль обязательны." });
                    return;
                }

                var existingUser = _databaseService.FirstOrDefault<User>(u => u.email == email);

                if (existingUser == null)
                {
                    // Регистрация нового пользователя
                    if (string.IsNullOrWhiteSpace(username))
                    {
                        SendJsonResponse(response, new { success = false, message = "Имя обязательно при регистрации." });
                        return;
                    }

                    var newUser = new User
                    {
                        username = username,
                        email = email,
                        password = password,
                        role = "user"
                    };

                    var createdUser = _databaseService.Create(newUser, "Users");
                    if (createdUser == null)
                    {
                        SendJsonResponse(response, new { success = false, message = "Ошибка создания пользователя." });
                        return;
                    }

                    // Создаем сессию для нового пользователя
                    var token = GenerateToken();
                    var session = new Session
                    {
                        user_id = createdUser.id,
                        session_token = token,
                        expires_at = DateTime.UtcNow.AddDays(7)
                    };
                    _databaseService.Create(session, "Sessions");
                    
                    response.SetCookie(new Cookie("session_token", token)
                    {
                        HttpOnly = true,
                        Secure = false,
                        Path = "/",
                        Expires = DateTime.UtcNow.AddDays(7)
                    });

                    SendJsonResponse(response, new { success = true, user = new { createdUser.username, createdUser.role } });
                }
                else
                {
                    // Пользователь с таким email существует — проверяем username и password
                    if (existingUser.username != username)
                    {
                        SendJsonResponse(response, new { success = false, message = "Пользователь с таким email уже существует, но имя не совпадает." });
                        return;
                    }

                    if (existingUser.password != password)
                    {
                        SendJsonResponse(response, new { success = false, message = "Неверный пароль." });
                        return;
                    }

                    // Вход — создаем новую сессию (удаляем старые)
                    var oldSessions = _databaseService.Where<Session>(s => s.user_id == existingUser.id);
                    foreach (var oldSession in oldSessions)
                    {
                        _databaseService.Delete(oldSession.id, "Sessions");
                    }

                    var token = GenerateToken();
                    var session = new Session
                    {
                        user_id = existingUser.id,
                        session_token = token,
                        expires_at = DateTime.UtcNow.AddDays(7)
                    };
                    _databaseService.Create(session, "Sessions");
             
                    response.SetCookie(new Cookie("session_token", token)
                    {
                        HttpOnly = true,
                        Secure = false,
                        Path = "/",
                        Expires = DateTime.UtcNow.AddDays(7)
                    });

                    SendJsonResponse(response, new { success = true, user = new { existingUser.username, existingUser.role } });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в HandleAuth: {ex.Message}");
                SendJsonResponse(response, new { success = false, message = "Ошибка сервера." }, 500);
            }
        }

        private string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        [HttpGet("check-auth")]
        public void CheckAuth(HttpListenerContext context)
        {
            var token = context.Request.Cookies["session_token"]?.Value;
            var (valid, user) = _authService.ValidateSession(token);

            if (!valid)
            {                
                context.Response.SetCookie(new Cookie("session_token", "")
                {
                    HttpOnly = true,
                    Secure = false,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(-1)
                });
                SendJsonResponse(context.Response, new { authenticated = false });
            }
            else
            {
                SendJsonResponse(context.Response, new { authenticated = true, user = new { user.username, user.role } });
            }
        }

        [HttpPost("add-tour")]
        public void AddTour(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {                
                var token = request.Cookies["session_token"]?.Value;
                if (string.IsNullOrEmpty(token))
                {
                    SendJsonResponse(response, new { success = false, message = "Требуется авторизация" }, 401);
                    return;
                }

                var (valid, user) = _authService.ValidateSession(token);
                if (!valid || user?.role != "admin")
                {
                    SendJsonResponse(response, new { success = false, message = "Доступ запрещён" }, 403);
                    return;
                }

                // Чтение JSON
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var body = reader.ReadToEnd();
                var jsonDoc = JsonDocument.Parse(body);

                var tour = new Tour
                {
                    image_url = jsonDoc.RootElement.GetProperty("image_url").GetString(),
                    tour_name = jsonDoc.RootElement.GetProperty("tour_name").GetString(),
                    departure_city = jsonDoc.RootElement.GetProperty("departure_city").GetString(),
                    arrival_city = jsonDoc.RootElement.GetProperty("arrival_city").GetString(),
                    departure_date = DateTime.Parse(jsonDoc.RootElement.GetProperty("departure_date").GetString()),
                    nights_count = jsonDoc.RootElement.GetProperty("nights_count").GetInt32(),
                    people_count = jsonDoc.RootElement.GetProperty("people_count").GetInt32(),
                    tour_price = jsonDoc.RootElement.GetProperty("tour_price").GetDecimal(),
                    hotel_name = jsonDoc.RootElement.GetProperty("hotel_name").GetString(),
                    location_description = jsonDoc.RootElement.GetProperty("location_description").GetString(),
                    rating = jsonDoc.RootElement.GetProperty("rating").GetInt32(),
                    meal_plan = jsonDoc.RootElement.GetProperty("meal_plan").GetString(),
                    end_date = DateTime.Parse(jsonDoc.RootElement.GetProperty("end_date").GetString()),
                    nearby_attractions = jsonDoc.RootElement.GetProperty("nearby_attractions").GetString(),
                    hotel_facilities = jsonDoc.RootElement.GetProperty("hotel_facilities").GetString(),
                    adult_pools_count = jsonDoc.RootElement.GetProperty("adult_pools_count").GetInt32(),
                    children_pools_count = jsonDoc.RootElement.GetProperty("children_pools_count").GetInt32(),
                    beach_info = jsonDoc.RootElement.GetProperty("beach_info").GetString(),
                    contact_info = jsonDoc.RootElement.GetProperty("contact_info").GetString()
                };

                // Сохраняем в БД
                var createdTour = _databaseService.Create(tour, "Tours");
                if (createdTour != null)
                {
                    SendJsonResponse(response, new { success = true, message = "Тур добавлен", id = createdTour.id });
                }
                else
                {
                    SendJsonResponse(response, new { success = false, message = "Ошибка сохранения" }, 500);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка AddTour: {ex.Message}");
                SendJsonResponse(response, new { success = false, message = "Ошибка сервера" }, 500);
            }
        }

        [HttpGet("tours")]
        public object GetToursApi(string departure_city = null, string arrival_city = null,
                                  DateTime? departure_date = null, int? nights_count = null,
                                  int? people_count = null)
        {
            try
            {
                var toursFromDb = _databaseService.ReadByAll<Tour>("Tours");
                
                if (!string.IsNullOrEmpty(departure_city))
                {
                    toursFromDb = toursFromDb.Where(t => t.departure_city.Contains(departure_city, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(arrival_city))
                {
                    toursFromDb = toursFromDb.Where(t => t.arrival_city.Contains(arrival_city, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (departure_date.HasValue)
                {                    
                    toursFromDb = toursFromDb.Where(t => t.departure_date.Date == departure_date.Value.Date).ToList();
                }

                if (nights_count.HasValue)
                {
                    toursFromDb = toursFromDb.Where(t => t.nights_count == nights_count.Value).ToList();
                }

                if (people_count.HasValue)
                {
                    toursFromDb = toursFromDb.Where(t => t.people_count == people_count.Value).ToList();
                }

                var result = new
                {
                    success = true,
                    Message = "Данные из базы",
                    tours = toursFromDb
                };

                Console.WriteLine($"Успешно получено {toursFromDb.Count} туров из базы после фильтрации");
                return result;
            }
            catch (NpgsqlException npgEx)
            {
                Console.WriteLine($"Ошибка SQL Server: {npgEx.Message}");
                return new
                {
                    success = false,
                    error = $"Ошибка подключения к базе данных: {npgEx.Message}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Неизвестная ошибка: {ex.Message}");
                Console.WriteLine($"Стек: {ex.StackTrace}");
                return new
                {
                    success = false,
                    error = $"Ошибка сервера: {ex.Message}"
                };
            }
        }

        [HttpGet("details")]
        public string TourDetails(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return "<h1>Неверный ID тура</h1>";
                }

                var tour = _databaseService.ReadById<Tour>(id, "Tours");
                if (tour == null)
                    return "<h1>Тур не найден</h1>";

                var templatePath = "static/tour-details.html";
                return _templateRenderer.RenderFromFile(templatePath, new { Tour = tour });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в TourDetails: {ex.Message}");
                return $"<h1>Ошибка загрузки тура</h1><p>{ex.Message}</p>";
            }
        }

        private void SendJsonResponse(HttpListenerResponse response, object data, int statusCode = 200)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close(); 
        }
    }
}