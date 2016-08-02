using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Xml.Serialization;
using WatcherService.Common.Config;

namespace WatcherService.Installer
{
    [RunInstaller(true)]
    public class WatcherServiceInstaller : System.Configuration.Install.Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceInstaller serviceInstaller;

        public WatcherServiceInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.ServiceName = "Watcher Service";
            serviceInstaller.Description = "Service for managing files or folder";
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            FileInfo fInfo = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string directoryPath = Path.Combine(fInfo.Directory.FullName,"Config");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            string file = "Watchers.xml";
            string filePath = Path.Combine(directoryPath, file);
            ConfigurationFile configFile = new ConfigurationFile();
            configFile.Configs = new List<ConfigPath>();
            string currentPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            configFile.Configs.Add(new ConfigPath()
            {
                Name = "System Execution",
                Action = ActionOnPath.Update,
                Desccription = "Do not modify",
                IncludeSubDirectories = false,
                Path = Path.Combine(currentPath, filePath),
                Script = new StringBuilder().AppendLine("net stop \"Watcher Service\"").AppendLine("net start \"Watcher Service\"").ToString()
            });
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigurationFile));
            TextWriter tWriter = new StreamWriter(filePath);
            xmlSerializer.Serialize(tWriter, configFile);
        }
    }
}