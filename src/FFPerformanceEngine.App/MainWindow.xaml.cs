using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FFPerformanceEngine.App.Pages;

namespace FFPerformanceEngine.App;

public partial class MainWindow : Window
{
    private MiniModeWindow? _mini;
    public MainWindow() { InitializeComponent(); Navigate("Home"); }
    private void Navigate_Click(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string page }) Navigate(page); }
    private void Navigate(string page)
    {
        PageTitle.Text = page;
        ContentHost.Content = page switch
        {
            "Optimize" => new OptimizePage(), "Profiles" => new ProfilesPage(), "Guardian" => new GuardianPage(), "Performance" => new PerformancePage(), "Expert" => new ExpertPage(), "History" => new HistoryPage(), "Settings" => new SettingsPage(), _ => new HomePage()
        };
    }
    private void MiniMode_Click(object sender, RoutedEventArgs e) { _mini ??= new MiniModeWindow(); if (!_mini.IsVisible) _mini.Show(); else _mini.Activate(); }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) ToggleMaximize(); else DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
