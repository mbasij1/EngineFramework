using EngineFramework.Setting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EngineFramework.Logging
{
    public static class EngineFrameworkLoggerFactory
    {
        public static ILoggerFactory loggerFactory;

        static EngineFrameworkLoggerFactory()
        {
            var temp = AppSettings.GetLoggingSetting();
            if (temp == null)
                temp = new LoggingSetting()
                {
                    LogName = "Application",
                    SourceName = "Application"
                };

            loggerFactory = new LoggerFactory().AddConsole().AddDebug().AddEventLog(
                new Microsoft.Extensions.Logging.EventLog.EventLogSettings()
                {
                    LogName = temp.LogName,
                    SourceName = temp.SourceName
                }); // Should Add TraceSource Maybe Later!
        }

        public static ILogger CreateLogger(Type type)
        {
            return loggerFactory.CreateLogger(type);
        }
    }
}
