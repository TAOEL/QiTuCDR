using System;
using System.Windows;

namespace QiTuCDR.Host.NativePanels
{
    internal static class NativePanelResourceLoader
    {
        private static readonly Uri[] StyleResourceUris =
        {
            new Uri("/QiTuCDR.Host;component/NativePanels/Styles/NativeButtonStyles.xaml", UriKind.Relative),
            new Uri("/QiTuCDR.Host;component/NativePanels/Styles/NativeFormStyles.xaml", UriKind.Relative),
            new Uri("/QiTuCDR.Host;component/NativePanels/Styles/NativeWindowStyles.xaml", UriKind.Relative)
        };

        public static NativePanelThemeMode CurrentTheme { get; private set; } = NativePanelThemeMode.Light;

        public static void SetTheme(NativePanelThemeMode theme)
        {
            CurrentTheme = theme;
        }

        public static void MergeInto(FrameworkElement element)
        {
            MergeInto(element, CurrentTheme);
        }

        public static void MergeInto(FrameworkElement element, NativePanelThemeMode theme)
        {
            element.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = GetThemeUri(theme) });

            foreach (var uri in StyleResourceUris)
            {
                element.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
            }
        }

        private static Uri GetThemeUri(NativePanelThemeMode theme)
        {
            var fileName = theme == NativePanelThemeMode.Dark ? "NativeDarkTheme.xaml" : "NativeLightTheme.xaml";
            return new Uri("/QiTuCDR.Host;component/NativePanels/Themes/" + fileName, UriKind.Relative);
        }
    }
}
