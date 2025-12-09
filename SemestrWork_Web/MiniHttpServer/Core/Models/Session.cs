using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Core.Models
{
    public class Session
    {
        public int id { get; set; }
        public int user_id { get; set; }
        public string session_token { get; set; }
        public DateTime expires_at { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
    }
}
