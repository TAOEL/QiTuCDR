using System;
using System.IO;

namespace QiTuCDR.Infrastructure.Config
{
    public static class PluginPaths
    {
        public const string ProductDirectoryName = "QiTuCDR";
        public const string ConfigDirectoryName = "Config";
        public const string LogsDirectoryName = "Logs";
        public const string SettingsFileName = "settings.json";

        public static string GetRootDirectory()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ProductDirectoryName);
        }

        public static string GetConfigDirectory()
        {
            return Path.Combine(GetRootDirectory(), ConfigDirectoryName);
        }

        public static string GetLogDirectory()
        {
            return Path.Combine(GetRootDirectory(), LogsDirectoryName);
        }

        public static string GetSettingsFilePath()
        {
            return Path.Combine(GetConfigDirectory(), SettingsFileName);
        }
    }
}
