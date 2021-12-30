using FilesWatcherRunner.ServiceProcess;
using FilesWatcherService;
using FilesWatcherService.BLL;
using FilesWatcherService.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;

namespace FilesWatcherRunner
{
    /// <summary>
    /// TO INSTALL THROUGH POWERSHELL:
    /// New-Service -Name "FilesWatcherRunner" -BinaryPathName FilesWatcherRunner.exe
    /// 
    /// TO REMOVE:
    /// Remove-Service -Name "FilesWatcherRunner"
    /// sc.exe delete "FilesWatcherRunner"
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            ConfigValueProvider.ConfigureLog();

            Log.Information("FilesWatcherRunner iniciado.");

            if (Debugger.IsAttached)
            {
                new FilesWatcherServiceProcess().StartMonitoring();
                Console.ReadKey();
            }
            else
                ServiceBase.Run(new FilesWatcherServiceProcess());
        }
    }
}

