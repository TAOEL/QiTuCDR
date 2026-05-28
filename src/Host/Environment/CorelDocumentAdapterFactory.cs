using System;
using QiTuCDR.Core.Host;
using QiTuCDR.Infrastructure.COM;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Environment
{
    public sealed class CorelDocumentAdapterFactory : ICorelDocumentAdapterFactory
    {
        public ICorelDocumentAdapter Create(object? corelApplication, IComDispatcher comDispatcher, PluginConfig config, ILogger logger)
        {
#if QITUCDR_TYPED_INTEROP
            if (config.PreferTypedCorelInterop && corelApplication != null)
            {
                try
                {
                    logger.Info("Creating typed CorelDRAW document adapter.");
                    return new TypedCorelDocumentAdapter(corelApplication, comDispatcher, logger);
                }
                catch (Exception ex)
                {
                    logger.Warn("Typed CorelDRAW document adapter unavailable. Falling back to dynamic adapter: " + ex.Message);
                }
            }

            if (!config.PreferTypedCorelInterop)
            {
                logger.Info("Typed CorelDRAW document adapter is compiled but disabled by config.");
            }
#endif

            logger.Info("Creating dynamic CorelDRAW document adapter.");
            var hostContext = new CorelHostContext(corelApplication);
            return new DynamicCorelDocumentAdapter(hostContext, comDispatcher, logger);
        }
    }
}
