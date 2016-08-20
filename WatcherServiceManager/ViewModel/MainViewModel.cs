using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Xml.Serialization;
using WatcherService.Common.Config;

namespace WatcherServiceManager.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private const string PATH_CONFIG_DIR = "Config";
        private const string PATH_CONFIG_FILE = "Watchers.xml";

        private ObservableCollection<ConfigPath> configurations = null;
        private ServiceController service = null;
        public ObservableCollection<ConfigPath> Configurations
        {
            get
            {
                return configurations;
            }
            set
            {
                if (configurations == value)
                    return;
                configurations = value;
                RaisePropertyChanged("Configurations");
            }
        }
        public ServiceController Service
        {
            get
            {
                return service;
            }
            set
            {
                if (service == value)
                    return;
                service = value;
                RaisePropertyChanged("Service");
            }
        }
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
            Init();
        }


        private void Init()
        {
            string currentPath = Directory.GetCurrentDirectory();
            string path = Path.Combine(currentPath, PATH_CONFIG_DIR, PATH_CONFIG_FILE);
            Configurations = GetTasks(path);
        }

        public ObservableCollection<ConfigPath> GetTasks(string path)
        {
            ObservableCollection<ConfigPath> configPaths = null;
            try
            {
                if (File.Exists(path))
                {
                    ConfigurationFile configFile = null;
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigurationFile));
                    //logger.Info(string.Format("Start reading file: {0}", filePath));
                    using (StreamReader sReader = new StreamReader(path))
                    {
                        configFile = (ConfigurationFile)xmlSerializer.Deserialize(sReader);
                        if (configFile != null)
                        {
                            if (configFile.Configs != null && configFile.Configs.Count > 0)
                                if (configFile.Configs != null)
                                {
                                    configPaths = new ObservableCollection<ConfigPath>(from x in configFile.Configs
                                                  orderby x.Name ascending
                                                  select x);
                                }
                        }
                    }
                }
                else
                {
                    //logger.Warn(string.Format("The file {0} does not exist.",filePath));
                }
            }
            catch(Exception exc)
            {

            }
            return configPaths;
        }
        public ServiceController GetService()
        {
            ServiceController service = new ServiceController("Watcher Service");
            return service;
        }
    }
}