using System;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Docking
{
    public static class DockPanelHostFactorySelector
    {
        public static IDockPanelHostFactory Create(string? dockHostMode, ILogger logger)
        {
            return Create(dockHostMode, logger, allowOfficialCorelDockerAdapter: false);
        }

        public static IDockPanelHostFactory Create(string? dockHostMode, ILogger logger, bool allowOfficialCorelDockerAdapter)
        {
            var normalized = Normalize(dockHostMode);
            if (string.Equals(normalized, DockHostModes.CorelDocker, StringComparison.OrdinalIgnoreCase))
            {
                if (allowOfficialCorelDockerAdapter)
                {
                    logger.Warn("Official CorelDRAW Docker adapter was explicitly enabled, but it is still an unimplemented API shell.");
                    return new CorelDockPanelHostFactory(new CorelDockerAdapterFactory());
                }

                logger.Warn("CorelDocker DockHostMode requested, but official Docker adapter is not enabled; using placeholder adapter.");
                return new CorelDockPanelHostFactory(new PlaceholderCorelDockerAdapterFactory());
            }

            if (!string.Equals(normalized, DockHostModes.Debug, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warn("Unknown DockHostMode; falling back to Debug: " + normalized);
            }

            return new DebugDockPanelHostFactory();
        }

        public static string Normalize(string? dockHostMode)
        {
            return string.IsNullOrWhiteSpace(dockHostMode)
                ? DockHostModes.Debug
                : dockHostMode!.Trim();
        }
    }
}
