namespace QiTuCDR.Infrastructure.Config
{
    public interface IPluginConfigStore
    {
        PluginConfig Load();
        bool Save(PluginConfig config);
    }
}
