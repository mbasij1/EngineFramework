using EngineFramework.Setting.Engine;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EngineFramework.Setting
{
    public static class AppSettings
    {
        public static IConfigurationRoot _Configuration { get; set; }

        static AppSettings()
        {
            if (!File.Exists("appsettings.json"))
                throw new Exception("Config File 'appsettings.json' Can't Find.");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _Configuration = builder.Build();
        }

        public static string GetConnectionString(string name) => _Configuration.GetConnectionString(name);

        public static List<string> EngineNames => _Configuration.GetSection("Engines").Get<List<string>>() ?? new List<string>();

        public static T GetSetting<T>(string sectionName) => _Configuration.GetSection(sectionName).Get<T>();

        public static IEnumerable<EngineNode> GetFailOverSetting() => _Configuration.GetSection("FailOverSettings").Get<IEnumerable<EngineNode>>();

        public static LoggingSetting GetLoggingSetting() => _Configuration.GetSection("LoggingSetting").Get<LoggingSetting>();
    }
}
