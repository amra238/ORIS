using MiniHttpServer.Core.Models;
using System;
using System.Security.Cryptography;

namespace MiniHttpServer.Core.Services
{
    public class AuthService
    {
        private readonly ORMContext _db;

        public AuthService(ORMContext db) => _db = db;

        private string GenerateToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public (bool Success, string Message, User User) Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
                return (false, "Все поля обязательны.", null);

            var existing = _db.FirstOrDefault<User>(u => u.email == email);
            if (existing != null)
                return (false, "Пользователь с таким email уже существует.", null);

            var user = new User
            {
                username = username,
                email = email,
                password = password 
            };

            var created = _db.Create(user, "Users");
            return (true, "Регистрация успешна!", created);
        }

        public (bool Success, string Message, User User, string Token) Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return (false, "Email и пароль обязательны.", null, null);

            var user = _db.FirstOrDefault<User>(u => u.email == email);
            if (user == null)
                return (false, "Пользователь не найден.", null, null);

            if (user.password != password) 
                return (false, "Неверный пароль.", null, null);

            var token = GenerateToken();
            var session = new Session
            {
                user_id = user.id,
                session_token = token,
                expires_at = DateTime.UtcNow.AddDays(7)
            };
            _db.Create(session, "Sessions");

            return (true, "Вход выполнен.", user, token);
        }

        public (bool Valid, User User) ValidateSession(string token)
        {
            if (string.IsNullOrEmpty(token)) return (false, null);

            var session = _db.FirstOrDefault<Session>(s => s.session_token == token && s.expires_at > DateTime.Now);
            if (session == null) return (false, null);

            var user = _db.ReadById<User>(session.user_id, "Users");
            return (true, user);
        }

        public void Logout(string token)
        {
            var session = _db.FirstOrDefault<Session>(s => s.session_token == token);
            if (session != null)
                _db.Delete(session.id, "Sessions");
        }
    }
}