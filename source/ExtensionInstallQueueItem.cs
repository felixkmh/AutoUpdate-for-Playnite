using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoUpdate.AutoUpdate;

namespace AutoUpdate
{
    public enum ExtInstallType
    {
        Install,
        Uninstall
    }

    public class ExtensionInstallQueueItem
    {
        public ExtInstallType InstallType { get; set; }
        public string Path { get; set; }

        [JsonIgnore]
        public UpdateInfo Info { get; set; } = null;

        public ExtensionInstallQueueItem() {}

        public ExtensionInstallQueueItem(string path, ExtInstallType type)
        {
            Path = path;
            InstallType = type;
        }

        public override string ToString()
        {
            return Path;
        }
    }
}
