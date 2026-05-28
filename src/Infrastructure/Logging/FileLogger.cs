using System;
using System.IO;
using QiTuCDR.Infrastructure.Config;

namespace QiTuCDR.Infrastructure.Logging
{
    public sealed class FileLogger : ILogger
    {
        private readonly object _sync = new object();
        private readonly string _logFile;

        public FileLogger(string? baseDirectory = null)
        {
            var root = baseDirectory ?? PluginPaths.GetLogDirectory();

            Directory.CreateDirectory(root);
            _logFile = Path.Combine(root, DateTime.UtcNow.ToString("yyyyMMdd") + ".log");
        }

        public void Info(string message) => Write("INFO", message, null);

        public void Warn(string message) => Write("WARN", message, null);

        public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

        private void Write(string level, string message, Exception? exception)
        {
            var line = $"{DateTimeOffset.Now:O} [{level}] {message}";
            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_sync)
            {
                File.AppendAllText(_logFile, line + Environment.NewLine);
            }
        }
    }
}
