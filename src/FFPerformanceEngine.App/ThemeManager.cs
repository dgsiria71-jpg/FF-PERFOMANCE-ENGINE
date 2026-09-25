using System.Windows;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.App;

public static class ThemeManager
{
    public static void Apply(ThemeMode mode)
    {
        var app = Application.Current;
        if (app is null) return;
        var merged = app.Resources.MergedDictionaries;
        var old = merged.FirstOrDefault(x => x.Source?.OriginalString.Contains("Themes/Clean.xaml", StringComparison.OrdinalIgnoreCase) == true
                                          || x.Source?.OriginalString.Contains("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase) == true);
        if (old is not null) merged.Remove(old);
        merged.Add(new ResourceDictionary { Source = new Uri(mode == ThemeMode.Dark ? "Themes/Dark.xaml" : "Themes/Clean.xaml", UriKind.Relative) });
    }
}
