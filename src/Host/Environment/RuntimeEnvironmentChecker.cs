using System;
using System.Collections.Generic;
using System.IO;

namespace QiTuCDR.Host.Environment
{
    public sealed class RuntimeEnvironmentChecker : IRuntimeEnvironmentChecker
    {
        public RuntimeCheckResult Check(object? corelApplication)
        {
            var items = new List<RuntimeCheckItem>
            {
                CheckCorelHostObject(corelApplication),
                CheckWebView2Runtime(),
                CheckCorelDrawTypeLib(),
                CheckWebUiAssets()
            };

            return new RuntimeCheckResult(items);
        }

        private static RuntimeCheckItem CheckCorelHostObject(object? corelApplication)
        {
            return corelApplication == null
                ? new RuntimeCheckItem(RuntimeCheckNames.CorelHostObject, false, RuntimeCheckSeverity.Warning, "CorelDRAW host object is not available; host operations will fail until injected by CorelDRAW.")
                : new RuntimeCheckItem(RuntimeCheckNames.CorelHostObject, true, RuntimeCheckSeverity.Info, "CorelDRAW host object is available.");
        }

        private static RuntimeCheckItem CheckWebView2Runtime()
        {
            var paths = new[]
            {
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView", "Application"),
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "Microsoft", "EdgeWebView", "Application")
            };

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    return new RuntimeCheckItem(RuntimeCheckNames.WebView2Runtime, true, RuntimeCheckSeverity.Info, "WebView2 Runtime is installed.");
                }
            }

            return new RuntimeCheckItem(RuntimeCheckNames.WebView2Runtime, false, RuntimeCheckSeverity.Fatal, "WebView2 Runtime is missing; native fallback panel will be used.");
        }

        private static RuntimeCheckItem CheckCorelDrawTypeLib()
        {
            var roots = new[]
            {
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "Corel"),
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86), "Corel")
            };

            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                if (Directory.GetFiles(root, "CorelDRAW.tlb", SearchOption.AllDirectories).Length > 0)
                {
                    return new RuntimeCheckItem(RuntimeCheckNames.CorelDrawTypeLib, true, RuntimeCheckSeverity.Info, "CorelDRAW TypeLib was found.");
                }
            }

            return new RuntimeCheckItem(RuntimeCheckNames.CorelDrawTypeLib, false, RuntimeCheckSeverity.Warning, "CorelDRAW TypeLib was not found; typed interop generation may not be available.");
        }

        private static RuntimeCheckItem CheckWebUiAssets()
        {
            return File.Exists(FindWebUiIndex())
                ? new RuntimeCheckItem(RuntimeCheckNames.WebUiAssets, true, RuntimeCheckSeverity.Info, "WebUI assets are available.")
                : new RuntimeCheckItem(RuntimeCheckNames.WebUiAssets, false, RuntimeCheckSeverity.Warning, "WebUI assets are missing; WebView will show a minimal placeholder.");
        }

        private static string FindWebUiIndex()
        {
            var assemblyDirectory = Path.GetDirectoryName(typeof(RuntimeEnvironmentChecker).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(assemblyDirectory, "WebUI", "index.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebUI", "index.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "WebUI", "index.html"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "WebUI", "index.html"),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.GetFullPath(candidates[0]);
        }
    }
}
