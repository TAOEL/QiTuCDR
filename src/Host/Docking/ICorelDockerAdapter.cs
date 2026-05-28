using System;

namespace QiTuCDR.Host.Docking
{
    public interface ICorelDockerAdapter : IDisposable
    {
        bool IsVisible { get; }
        void CreateContainer(object? corelApplication);
        void AttachPanel(QiTuDockPanel panel);
        void Show();
        void Hide();
        void Release();
    }
}
