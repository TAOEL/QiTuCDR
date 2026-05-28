using QiTuCDR.Host.Lifecycle;

namespace QiTuCDR.Host.Startup
{
    public sealed class QiTuCdrAddIn
    {
        private readonly PluginLifecycleManager _lifecycleManager = new PluginLifecycleManager();

        public void OnConnection(object corelApplication)
        {
            _lifecycleManager.Start(corelApplication);
        }

        public void ShowPanel()
        {
            _lifecycleManager.ShowPanel();
        }

        public void OnDocumentBeforeClose()
        {
            _lifecycleManager.NotifyDocumentClosing();
        }

        public void OnDocumentActivated()
        {
            _lifecycleManager.NotifyDocumentActivated();
        }

        public void OnSelectionChanged()
        {
            _lifecycleManager.NotifySelectionChanged();
        }

        public void OnApplicationQuit()
        {
            _lifecycleManager.NotifyHostShuttingDown();
        }

        public void OnDisconnection()
        {
            _lifecycleManager.NotifyHostShuttingDown();
            _lifecycleManager.Dispose();
        }
    }
}
