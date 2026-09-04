using System.Windows;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App;

public partial class App : Application
{
    public static AppServices Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Services = new AppServices();
        await Services.InitializeAsync();
        ThemeManager.Apply(Services.Settings.Theme);
        base.OnStartup(e);
        var main = new MainWindow();
        MainWindow = main;
        main.Show();
    }
}
