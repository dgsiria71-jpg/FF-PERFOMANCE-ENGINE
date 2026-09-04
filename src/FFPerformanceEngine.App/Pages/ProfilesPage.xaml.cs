using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.App.Pages;

public partial class ProfilesPage : UserControl
{
    private IReadOnlyList<PerformanceProfile> _profiles = Array.Empty<PerformanceProfile>();

    public ProfilesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _profiles = await App.Services.Profiles.LoadAsync();
        ProfilesList.ItemsSource = _profiles;
        var recommended = _profiles.FirstOrDefault(x => x.Kind == ProfileKind.Recommended);
        RecommendedText.Text = recommended is null
            ? "Ainda não há um perfil Recomendado validado."
            : $"{recommended.AverageFps:0} FPS · 1% Low {recommended.OnePercentLow:0} · confiança {recommended.Confidence:P0}";
    }

    private async void ApplyProfile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid id }) return;
        var profile = _profiles.FirstOrDefault(x => x.Id == id);
        if (profile is null) return;
        var environment = App.Services.Environment.Capture();
        var instance = !string.IsNullOrWhiteSpace(profile.InstanceName)
            ? environment.Instances.FirstOrDefault(x => string.Equals(x.Name, profile.InstanceName, StringComparison.OrdinalIgnoreCase))
            : environment.Instances.FirstOrDefault();
        if (instance is null)
        {
            MessageBox.Show("Nenhuma instância BlueStacks compatível foi encontrada.", "FF Performance Engine");
            return;
        }

        var result = await App.Services.ProfileApplication.ApplyAsync(profile, instance);
        if (result.RequiresPlayerStop)
        {
            MessageBox.Show($"{result.Message}\n\nFeche o BlueStacks e aplique novamente. A sessão atual não foi interrompida.", "Reinício necessário");
            return;
        }
        MessageBox.Show(result.Message, result.Success ? "Perfil aplicado" : "Perfil não aplicado");
    }
}
