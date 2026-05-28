using System;
using System.Windows;

namespace QiTuCDR.NativePanelPreview
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            var app = new Application
            {
                ShutdownMode = ShutdownMode.OnMainWindowClose
            };

            app.Run(new PreviewWindow());
        }
    }
}
