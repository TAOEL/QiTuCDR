using QiTuCDR.Core.Host;
using QiTuCDR.Infrastructure.COM;
using QiTuCDR.Infrastructure.Config;
using QiTuCDR.Infrastructure.Logging;

namespace QiTuCDR.Host.Environment
{
    public interface ICorelDocumentAdapterFactory
    {
        ICorelDocumentAdapter Create(object? corelApplication, IComDispatcher comDispatcher, PluginConfig config, ILogger logger);
    }
}
