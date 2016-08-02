using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using WatcherService.Services;

namespace WatcherService
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new FileSystemService());
            //FileSystemService service = new FileSystemService();
            //service.Start();
        }
    }
}
