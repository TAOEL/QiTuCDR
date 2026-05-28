using System;

namespace QiTuCDR.Host.Docking
{
    public interface IDockPanelHost : IDisposable
    {
        bool IsVisible { get; }
        string DiagnosticHostKind { get; }
        string? DiagnosticAdapterType { get; }
        bool DiagnosticIsAttached { get; }
        void Show();
        void Hide();
        void ShowWebView();
        void ShowFallback();
    }
}
