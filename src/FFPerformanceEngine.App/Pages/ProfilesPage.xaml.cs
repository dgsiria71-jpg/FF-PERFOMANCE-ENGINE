using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

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

        RefreshABComparison();
        await RefreshHistoricalValidationAsync();
    }

    private void RefreshABComparison()
    {
        var comparison = App.Services.PerformanceComparison.CurrentComparison;
        if (comparison is null)
        {
            ABComparisonText.Text = "Nenhuma comparação A/B ativa.";
            ABEvidenceText.Text = "Performance captura A e B; History também pode reabrir comparações antigas sem transformar observação em validação.";
            ABDeltaText.Text = "—";
            return;
        }

        var candidateEvidence = PerformanceProfileEvidenceBridge.FromSnapshot(comparison.Candidate);
        var delta = PerformanceIntervalPresentation.FromComparison(comparison.Metrics);
        ABComparisonText.Text = $"{comparison.Baseline.Name}  →  {comparison.Candidate.Name}";
        ABEvidenceText.Text =
            $"B: {candidateEvidence.Quality} · FPS {candidateEvidence.FpsEvidenceSamples}/{candidateEvidence.TelemetrySamples} · " +
            $"Frame Time {candidateEvidence.FrameTimeEvidenceSamples}/{candidateEvidence.TelemetrySamples} · " +
            $"Latência {candidateEvidence.LatencyEvidenceSamples}/{candidateEvidence.TelemetrySamples} · Evidência {candidateEvidence.Evidence}.";
        ABDeltaText.Text = $"B − A · Δ FPS {delta.AverageFpsDelta} · Δ Frame Time {delta.AverageFrameTimeDelta}";
    }

    private async Task RefreshHistoricalValidationAsync()
    {
        var history = await App.Services.History.LoadPerformanceComparisonsAsync();
        var latestValidated = history.FirstOrDefault(record => record.CanOriginateProfile);
        if (latestValidated is null)
        {
            HistoricalValidationText.Text = "Nenhuma comparação histórica concluiu validação medida separada.";
            HistoricalValidationMetricsText.Text = "History mantém candidatos observados isolados dos vencedores validados.";
            return;
        }

        var evidence = PerformanceProfileEvidenceBridge.FromValidatedRecord(latestValidated);
        HistoricalValidationText.Text = $"{latestValidated.Label} · Evidência {evidence.Evidence}";
        HistoricalValidationMetricsText.Text =
            $"Validação: FPS {Format(evidence.AverageFps)} · Frame Time {Format(evidence.FrameTimeMs)} ms · Latência {Format(evidence.LatencyMs)} ms · " +
            $"amostras FPS {evidence.FpsEvidenceSamples}/{evidence.TelemetrySamples}. Elegível para origem explícita de perfil; não convertido automaticamente.";
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

    private static string Format(double? value)
        => value is double number && double.IsFinite(number) ? number.ToString("0.0") : "—";
}
