using System;
using System.IO;
using Newtonsoft.Json;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Infrastructure.Config
{
    public sealed class JsonPluginConfigStore : IPluginConfigStore
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly string _settingsFilePath;
        private readonly ILogger? _logger;

        public JsonPluginConfigStore(string? settingsFilePath = null, ILogger? logger = null)
        {
            _settingsFilePath = settingsFilePath ?? PluginPaths.GetSettingsFilePath();
            _logger = logger;
        }

        public PluginConfig Load()
        {
            try
            {
                EnsureConfigDirectory();

                if (!File.Exists(_settingsFilePath))
                {
                    var defaultConfig = new PluginConfig();
                    Save(defaultConfig);
                    return defaultConfig;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var config = JsonConvert.DeserializeObject<PluginConfig>(json, JsonSettings);
                if (config == null)
                {
                    throw new JsonSerializationException("Plugin config JSON was empty.");
                }

                config.Normalize();
                return config;
            }
            catch (Exception ex)
            {
                _logger?.Error("Load plugin config failed. Fallback to default config.", ex);
                BackupBrokenConfig();
                var defaultConfig = new PluginConfig();
                Save(defaultConfig);
                return defaultConfig;
            }
        }

        public bool Save(PluginConfig config)
        {
            try
            {
                config.Normalize();
                EnsureConfigDirectory();
                var json = JsonConvert.SerializeObject(config, JsonSettings);
                File.WriteAllText(_settingsFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error("Save plugin config failed.", ex);
                return false;
            }
        }

        private void EnsureConfigDirectory()
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void BackupBrokenConfig()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    return;
                }

                var backupPath = _settingsFilePath + ".bad." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                File.Copy(_settingsFilePath, backupPath, overwrite: false);
            }
            catch (Exception ex)
            {
                _logger?.Warn("Backup broken plugin config failed: " + ex.Message);
            }
        }
    }
}
