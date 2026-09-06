using System.IO;
using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App.Pages;

public partial class ProfilesPage : UserControl
{
    private sealed record ChallengeRoleOption(ProfileKind Kind, string Name);

    private IReadOnlyList<PerformanceProfile> _profiles = Array.Empty<PerformanceProfile>();
    private bool _challengeRunning;

    public ProfilesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshProfilesAsync();
        RefreshABComparison();
        await RefreshHistoricalValidationAsync();
    }

    private async Task RefreshProfilesAsync(Guid? preferredCustomId = null)
    {
        _profiles = await App.Services.Profiles.LoadAsync();
        ProfilesList.ItemsSource = _profiles;
        var recommended = _profiles.FirstOrDefault(x => x.Kind == ProfileKind.Recommended);
        RecommendedText.Text = recommended is null
            ? "Ainda não há um perfil Recomendado validado."
            : $"{recommended.AverageFps:0} FPS · 1% Low {recommended.OnePercentLow:0} · confiança {recommended.Confidence:P0}";

        var custom = _profiles
            .Where(profile => profile.Kind == ProfileKind.Custom
                              && profile.Evidence == EvidenceLevel.Validated
                              && profile.SourceComparisonId is not null
                              && !string.IsNullOrWhiteSpace(profile.EnvironmentFingerprint))
            .OrderBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        ChallengeProfileComboBox.ItemsSource = custom;
        ChallengeProfileComboBox.SelectedItem = preferredCustomId is Guid id
            ? custom.FirstOrDefault(profile => profile.Id == id) ?? custom.FirstOrDefault()
            : custom.FirstOrDefault();
        RefreshChallengeRoles();
        UpdateChallengeButton();
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

    private void ChallengeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender == ChallengeProfileComboBox) RefreshChallengeRoles();
        UpdateChallengeButton();
    }

    private void RefreshChallengeRoles()
    {
        var selectedCustom = ChallengeProfileComboBox.SelectedItem as PerformanceProfile;
        if (selectedCustom is null)
        {
            ChallengeRoleComboBox.ItemsSource = Array.Empty<ChallengeRoleOption>();
            ChallengeRoleComboBox.SelectedItem = null;
            ChallengeStatusText.Text = "Nenhum Custom validado com procedência auditável está disponível para desafiar vencedores.";
            return;
        }

        var roles = _profiles
            .Where(profile => profile.Kind != ProfileKind.Custom
                              && profile.Evidence == EvidenceLevel.Validated
                              && profile.Game == selectedCustom.Game
                              && string.Equals(profile.InstanceName, selectedCustom.InstanceName, StringComparison.OrdinalIgnoreCase))
            .Select(profile => profile.Kind)
            .Distinct()
            .Where(kind => kind is ProfileKind.Recommended
                or ProfileKind.MaximumFps
                or ProfileKind.LowestLatency
                or ProfileKind.Stability
                or ProfileKind.Quality)
            .Select(kind => new ChallengeRoleOption(kind, RoleName(kind)))
            .OrderBy(option => RoleOrder(option.Kind))
            .ToList();

        var previous = (ChallengeRoleComboBox.SelectedItem as ChallengeRoleOption)?.Kind;
        ChallengeRoleComboBox.ItemsSource = roles;
        ChallengeRoleComboBox.SelectedItem = previous is ProfileKind previousKind
            ? roles.FirstOrDefault(option => option.Kind == previousKind) ?? roles.FirstOrDefault()
            : roles.FirstOrDefault();
        ChallengeStatusText.Text = roles.Count == 0
            ? "Este Custom não possui um vencedor validado compatível para desafiar nesta instância/jogo."
            : "Faça duas rodadas A/B completas em Performance e salve cada uma em History. Nenhuma promoção ocorre apenas por selecionar os perfis.";
    }

    private void UpdateChallengeButton()
    {
        RunChallengeButton.IsEnabled = !_challengeRunning
                                       && ChallengeProfileComboBox.SelectedItem is PerformanceProfile
                                       && ChallengeRoleComboBox.SelectedItem is ChallengeRoleOption;
    }

    private async void RunChallenge_Click(object sender, RoutedEventArgs e)
    {
        if (_challengeRunning
            || ChallengeProfileComboBox.SelectedItem is not PerformanceProfile challenger
            || ChallengeRoleComboBox.SelectedItem is not ChallengeRoleOption role)
            return;

        _challengeRunning = true;
        UpdateChallengeButton();
        ChallengeStatusText.Text = $"Analisando as rodadas medidas de {challenger.Name} contra {role.Name}...";
        try
        {
            var result = await App.Services.ProfileChallenges.AssessAndPromoteLatestAsync(
                challenger.Id,
                role.Kind,
                App.Services.Environment.Capture());
            ChallengeStatusText.Text = $"{result.Status} · {result.EvidenceRounds} rodada(s) compatível(is). {result.Message}";
            if (result.Promoted)
            {
                await RefreshProfilesAsync(challenger.Id);
                await RefreshHistoricalValidationAsync();
                MessageBox.Show(
                    result.Message + "\n\nO Custom de origem foi preservado e o History registrou a promoção.",
                    "Desafio de vencedor");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
        {
            ChallengeStatusText.Text = $"Desafio não executado: {exception.Message}";
            MessageBox.Show(exception.Message, "Desafio de vencedor");
        }
        finally
        {
            _challengeRunning = false;
            UpdateChallengeButton();
        }
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

    private static string RoleName(ProfileKind kind)
        => kind switch
        {
            ProfileKind.Recommended => "Recomendado",
            ProfileKind.MaximumFps => "Máximo FPS",
            ProfileKind.LowestLatency => "Menor Latência",
            ProfileKind.Stability => "Estabilidade",
            ProfileKind.Quality => "Qualidade",
            _ => kind.ToString()
        };

    private static int RoleOrder(ProfileKind kind)
        => kind switch
        {
            ProfileKind.Recommended => 0,
            ProfileKind.MaximumFps => 1,
            ProfileKind.LowestLatency => 2,
            ProfileKind.Stability => 3,
            ProfileKind.Quality => 4,
            _ => 99
        };

    private static string Format(double? value)
        => value is double number && double.IsFinite(number) ? number.ToString("0.0") : "—";
}
