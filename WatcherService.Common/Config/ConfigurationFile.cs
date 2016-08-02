using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WatcherService.Common.Config
{
    [Serializable]
    public class ConfigurationFile
    {
        public List<ConfigPath> Configs { get; set; }
    }
}
