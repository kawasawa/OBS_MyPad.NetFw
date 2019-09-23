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
