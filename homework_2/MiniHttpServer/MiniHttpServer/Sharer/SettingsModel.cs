using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Sharer
{
    internal class SettingsModel
    {
        public string StaticDirectoryPath { get; set; }
        public string Domain { get; set; }
        public string Port { get; set; }
    }
}
