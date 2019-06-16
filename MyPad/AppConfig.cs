using MyLib;
using System.Configuration;

namespace MyPad
{
    public static partial class AppConfig
    {
        public static LogLevel LogLevel
        {
            get
            {
                var val = ConfigurationManager.AppSettings[nameof(LogLevel)]?.ToUpper() ?? string.Empty;
                if (LogLevel.Info.ToString().ToUpper().Equals(val))
                    return LogLevel.Info;
                if (LogLevel.Warn.ToString().ToUpper().Equals(val))
                    return LogLevel.Warn;
                if (LogLevel.Error.ToString().ToUpper().Equals(val))
                    return LogLevel.Error;
                return LogLevel.Debug;
            }
        }

        public static string InitialFileName
        {
            get
            {
                var val = ConfigurationManager.AppSettings[nameof(InitialFileName)];
                return string.IsNullOrEmpty(val) == false ? val : "NoName";
            }
        }

        public static int CacheLifetime
            => int.TryParse(ConfigurationManager.AppSettings[nameof(CacheLifetime)], out var value) && 1 <= value ? value : 7;

        public static long LargeFileSize
            => long.TryParse(ConfigurationManager.AppSettings[nameof(LargeFileSize)], out var value) && 0 < value ? value : 100L * 1024 * 1024;

        public static int TerminalLineSize
            => int.TryParse(ConfigurationManager.AppSettings[nameof(TerminalLineSize)], out var value) && 1 <= value ? value : 10000;

        public static string ProjectSite
            => ConfigurationManager.AppSettings[nameof(ProjectSite)];
    }
}
