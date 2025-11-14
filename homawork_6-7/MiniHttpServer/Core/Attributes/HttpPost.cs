using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Sharer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class HttpPost : Attribute
    {
        public string? Route { get; }

        public HttpPost() { }

        public HttpPost(string? route)
        {
            Route = route;
        }
    }
}
