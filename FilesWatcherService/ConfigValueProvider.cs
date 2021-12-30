using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FilesWatcherService
{
    public static class ConfigValueProvider
    {
        private static readonly IConfigurationRoot Configuration;

        static ConfigValueProvider()
        {
            ConfigureLog();

            var builder = new ConfigurationBuilder()
             .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
             .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
        }

        public static void ConfigureLog()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Log\\Log{DateTime.Now:yyyy-MM-dd-HH-mm}.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                .CreateLogger();
        }

        public static string Get(string name)
        {
            var result = string.Empty;

            try
            {
                result = Configuration[name];
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting configuration value for '{name}'", ex);
            }

            return result;
        }


        public static List<string> GetList(string name)
        {
            var result = new List<string>();

            try
            {
                var myArray = Configuration.GetSection(name).AsEnumerable();
                result = myArray.Select(pair => pair.Value).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting configuration value for '{name}'", ex);
            }

            return result;
        }
    }
}
