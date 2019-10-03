using MyLib;
using System;
using System.Configuration;
using System.Linq;

namespace MyPad
{
    public static partial class AppConfig
    {
        public static LogLevel MinLogLevel
        {
            get
            {
                var val = ConfigurationManager.AppSettings[nameof(MinLogLevel)];
                return val?.Any() == true && Enum.TryParse<LogLevel>(val, true, out var level) ? level : LogLevel.Info;
            }
        }

        public static int LifetimeOfTempsLeftBehind
            => int.TryParse(ConfigurationManager.AppSettings[nameof(LifetimeOfTempsLeftBehind)], out var value) && 0 <= value ? value : 7;

        public static int TerminalBufferSize
            => int.TryParse(ConfigurationManager.AppSettings[nameof(TerminalBufferSize)], out var value) && 300 <= value ? value : 10000;

        public static int GrepBufferSize
            => int.TryParse(ConfigurationManager.AppSettings[nameof(GrepBufferSize)], out var value) && 1 <= value ? value : 30;

        public static string InitialFileName
        {
            get
            {
                var val = ConfigurationManager.AppSettings[nameof(InitialFileName)];
                return string.IsNullOrEmpty(val) == false ? val : "NoName";
            }
        }

        public static string ProjectSite
            => ConfigurationManager.AppSettings[nameof(ProjectSite)];
    }
}
