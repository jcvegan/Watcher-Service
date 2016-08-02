using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Xml.Serialization;
using System.Linq;
using WatcherService.Common.Config;
using System.Diagnostics;

namespace WatcherService.Services
{
    public class FileSystemService : ServiceBase
    {
        List<FileSystemWatcher> fWatchers = null;
        string path = string.Empty;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public FileSystemService()
        {
            ServiceName = "Watcher Service";
            fWatchers = new List<FileSystemWatcher>();
        }

        public void Start()
        {
            OnStart(null);
        }
        protected override void OnStart(string[] args)
        {
            try
            {
                FileInfo fInfo = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);

                path = Path.Combine(fInfo.Directory.FullName, "Config", "Watchers.xml");
                base.OnStart(args);
                PopulateWatchers(GetPaths(path));
            }
            catch(Exception exc)
            {
                logger.Error(exc, exc.Message, null);
            }

        }
        protected override void OnStop()
        {
            base.OnStop();
            path = "";
            CleanWatchers();
        }
        protected override void OnShutdown()
        {

            base.OnShutdown();
            path = "";
            CleanWatchers();
        }
        protected override void OnContinue()
        {
            base.OnContinue();
            PopulateWatchers(GetPaths(path));
        }
        protected override void OnPause()
        {
            base.OnPause();
            CleanWatchers();
        }
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        private FileSystemWatcher GetWatcher(ConfigPath config)
        {
            FileSystemWatcher fWatcher = null;
            try
            {

                FileAttributes fAttributtes = File.GetAttributes(config.Path);
                if ((fAttributtes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //Is Directory
                    fWatcher = new FileSystemWatcher(config.Path);
                }
                else
                {

                    fWatcher = new FileSystemWatcher(Path.GetDirectoryName(config.Path));
                    fWatcher.Filter = Path.GetFileName(config.Path);
                }
                fWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
                fWatcher.IncludeSubdirectories = config.IncludeSubDirectories;
                logger.Info(string.Format("Starting {0} Watcher.", config.Name));
                switch (config.Action)
                {
                    case ActionOnPath.Create:
                        logger.Info(string.Format("Attaching {0} Watcher to CREATE action.", config.Name));
                        fWatcher.Created += (sender, args) =>
                        {
                            OnProcessCreated(config, fWatcher, args);
                        };
                        break;
                    case ActionOnPath.Delete:
                        logger.Info(string.Format("Attaching {0} Watcher to DELETE action.", config.Name));
                        fWatcher.Deleted += (sender, args) =>
                        {
                            OnProcessDelete(config, fWatcher, args);
                        };
                        break;
                    case ActionOnPath.Update:
                        logger.Info(string.Format("Attaching {0} Watcher to UPDATE action.", config.Name));
                        fWatcher.Changed += (sender, args) =>
                        {
                            //if (args.ChangeType == WatcherChangeTypes.Changed)
                                OnProcessUpdate(config, fWatcher, args);
                        };
                        break;
                    case ActionOnPath.Rename:
                        logger.Info(string.Format("Attaching {0} Watcher to RENAME action.", config.Name));
                        fWatcher.Renamed += (sender, args) =>
                        {
                            OnProcessDelete(config, fWatcher, args);
                        };
                        break;
                    default:
                        break;
                }
                fWatcher.EnableRaisingEvents = true;
                logger.Info(string.Format("Watcher {0} started.", config.Name));
            }
            catch (Exception exc)
            {
                logger.Error(exc, string.Format("{0}: {1}", config.Name,exc.Message), null);
            }
            
            return fWatcher;
        }
        

        private void OnProcessUpdate(ConfigPath config, FileSystemWatcher fWatcher, FileSystemEventArgs args)
        {
            logger.Info(string.Format("{0}: UPDATE => {1}", config.Name, args.Name));
            ExecuteScript(config, fWatcher, GetScriptLines(GetScript(config.Script, args)));
        }

        private void OnProcessDelete(ConfigPath config, FileSystemWatcher fWatcher, FileSystemEventArgs args)
        {
            logger.Info(string.Format("{0}: DELETE => {1}", config.Name, args.FullPath));
            ExecuteScript(config, fWatcher, GetScriptLines(GetScript(config.Script, args)));
        }

        private void OnProcessCreated(ConfigPath config, FileSystemWatcher fWatcher, FileSystemEventArgs args)
        {
            logger.Info(string.Format("{0}: CREATE => {1}", config.Name, args.FullPath));
            ExecuteScript(config, fWatcher, GetScriptLines(GetScript(config.Script, args)));
        }

        private string GetScript(string rawScript,FileSystemEventArgs args)
        {
            string raw = rawScript.ToLower();
            raw = raw.Replace("{{dirname}}", Path.GetDirectoryName(args.FullPath));
            raw = raw.Replace("{{filename}}", args.Name);
            raw = raw.Replace("{{fullname}}", args.FullPath);
            return raw;
        }
        private void ExecuteScript(ConfigPath config, FileSystemWatcher fWatcher, string[] script)
        {
            try
            {
                foreach (string scriptLine in script)
                {
                    logger.Info(string.Format("{0} => SCRIPT => {1}", config.Name, scriptLine));
                }
                ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd.exe");
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.UseShellExecute = false;
                Process process = Process.Start(processStartInfo);
                if(process != null)
                {
                    foreach (string scriptLine in script)
                    {
                        process.StandardInput.WriteLine(scriptLine);
                    }
                    process.StandardInput.Close();
                    string outputString = process.StandardOutput.ReadToEnd();
                    logger.Info(string.Format("{0} => RESULT => {1}", config.Name, outputString));
                }
            }
            catch(Exception exc)
            {
                logger.Error(exc, string.Format("{0}: {1}", config.Name, exc.Message), null);
            }
        }
        private string[] GetScriptLines(string script)
        {
            return script.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }

        private List<ConfigPath> GetPaths(string filePath)
        {
            List<ConfigPath> configPaths = null;
            try
            {
                if (File.Exists(filePath))
                {
                    ConfigurationFile configFile = null;
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigurationFile));
                    logger.Info(string.Format("Start reading file: {0}", filePath));
                    using (StreamReader sReader = new StreamReader(filePath))
                    {
                        configFile = (ConfigurationFile)xmlSerializer.Deserialize(sReader);
                        if (configFile != null)
                        {
                            if (configFile.Configs != null && configFile.Configs.Count > 0)
                                configPaths = configFile.Configs;
                        }
                    }
                }
                else
                {
                    logger.Warn(string.Format("The file {0} does not exist.",filePath));
                }
            }
            catch(Exception exc)
            {
                configPaths = null;
                logger.Error(exc, string.Format("Failed to load configurations on path:{0}", filePath), null);
            }
            return configPaths;
        }
        private void PopulateWatchers(List<ConfigPath> configs)
        {
            if(configs != null && configs.Count > 0)
                foreach (ConfigPath config in configs)
                {
                    fWatchers.Add(GetWatcher(config));
                }
        }
        private void CleanWatchers()
        {

        }

    }
}
