using FilesWatcherService;
using FilesWatcherService.BLL;
using FilesWatcherService.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FilesWatcherRunner.ServiceProcess
{
    public class FilesWatcherServiceProcess: ServiceBase
    {
        public FilesWatcherServiceProcess()
        {
            ServiceName = "FilesWatcherService";
            AutoLog = true;
        }

        public void StartMonitoring()
        {
            //get folder to watch path from appsettings.json
            var folderToWatch = ConfigValueProvider.Get("FSW:FSWSource");

            //get file types from appsettings.json
            List<string> fileTypes = ConfigValueProvider.GetList("FSW:FileTypes");

            //register our events
            FilesWatcher.OnFileReady += OnFileReady;
            FilesWatcher.OnNewMessage += OnNewMessage;
            FilesWatcher.Instance.Watch(folderToWatch, fileTypes);
        }

        protected override void OnStart(string[] args)
        {
            StartMonitoring();
        }

        private void OnNewMessage(object sender, string str)
        {
            Console.WriteLine(str); //or write to a logger...
        }

        private void OnFileReady(object sender, FileChangeEventArgs e)
        {
            Console.WriteLine($"File changed: {e.FullPath}");
            NotifyTeamsWebHook(e);

            //case 1 : you can log file ready event
            //case 2: you can call web api to report
            //case 3 : you can write to the screen        
        }

        private void NotifyTeamsWebHook(FileChangeEventArgs e)
        {
            try
            {
                var urlWebHook = ConfigValueProvider.Get("FSW:TargetAPIURL");

                if (!string.IsNullOrWhiteSpace(urlWebHook))
                {
                    var pathEncoded = e.FullPath.Replace("\\", "/");
                    var jsonContent = "{\"@context\":\"http://schema.org/extensions\",\"@type\": \"MessageCard\", \"text\": \"Arquivo alterado: " + pathEncoded + "\"}";

                    using (var client = new HttpClient())
                    {
                        var httpRequestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var r = client.PostAsync(urlWebHook, httpRequestContent).GetAwaiter().GetResult();

                        if (r.Content != null)
                        {
                            var respStr = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            Log.Information(respStr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("NotifyTeamsWebHook", ex);
            }
        }
    }
}
