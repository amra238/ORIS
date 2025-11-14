using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Sharer.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class EndPointAttribute : Attribute
    {
        public EndPointAttribute() { }
    }
}
