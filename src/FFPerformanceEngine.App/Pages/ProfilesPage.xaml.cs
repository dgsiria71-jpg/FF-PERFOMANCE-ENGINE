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
    private ProfileChallengeProgress? _challengeProgress;
    private bool _challengeRunning;
    private int _challengeProgressRevision;

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
        await RefreshChallengeProgressAsync();
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

    private async void ChallengeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender == ChallengeProfileComboBox) RefreshChallengeRoles();
        await RefreshChallengeProgressAsync();
    }

    private void RefreshChallengeRoles()
    {
        _challengeProgress = null;
        var selectedCustom = ChallengeProfileComboBox.SelectedItem as PerformanceProfile;
        if (selectedCustom is null)
        {
            ChallengeRoleComboBox.ItemsSource = Array.Empty<ChallengeRoleOption>();
            ChallengeRoleComboBox.SelectedItem = null;
            ChallengeRoundsText.Text = "0/2 rodadas elegíveis";
            ChallengeStatusText.Text = "Nenhum Custom validado com procedência auditável está disponível para desafiar vencedores.";
            UpdateChallengeButton();
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
        ChallengeRoundsText.Text = "Lendo evidência...";
        ChallengeStatusText.Text = roles.Count == 0
            ? "Este Custom não possui um vencedor validado compatível para desafiar nesta instância/jogo."
            : "Verificando as rodadas A/B salvas em History sem modificar nenhum perfil.";
        UpdateChallengeButton();
    }

    private async Task RefreshChallengeProgressAsync()
    {
        var revision = ++_challengeProgressRevision;
        _challengeProgress = null;
        UpdateChallengeButton();

        if (ChallengeProfileComboBox.SelectedItem is not PerformanceProfile challenger
            || ChallengeRoleComboBox.SelectedItem is not ChallengeRoleOption role)
        {
            ChallengeRoundsText.Text = "0/2 rodadas elegíveis";
            UpdateChallengeButton();
            return;
        }

        ChallengeRoundsText.Text = "Lendo evidência...";
        ChallengeStatusText.Text = $"Verificando {challenger.Name} contra {role.Name}...";
        try
        {
            var progress = await App.Services.ProfileChallengeProgress.GetAsync(
                challenger.Id,
                role.Kind,
                App.Services.Environment.Capture());
            if (revision != _challengeProgressRevision) return;

            _challengeProgress = progress;
            ChallengeRoundsText.Text = FormatChallengeRounds(progress);
            ChallengeStatusText.Text = $"{ProgressStatusName(progress.Status)} · {progress.Message}";
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
        {
            if (revision != _challengeProgressRevision) return;
            _challengeProgress = null;
            ChallengeRoundsText.Text = "Evidência indisponível";
            ChallengeStatusText.Text = $"Não foi possível avaliar o desafio: {exception.Message}";
        }
        finally
        {
            if (revision == _challengeProgressRevision) UpdateChallengeButton();
        }
    }

    private void UpdateChallengeButton()
    {
        var hasSelection = ChallengeProfileComboBox.SelectedItem is PerformanceProfile
                           && ChallengeRoleComboBox.SelectedItem is ChallengeRoleOption;
        RunChallengeButton.IsEnabled = !_challengeRunning
                                       && _challengeProgress?.CanPromote == true
                                       && hasSelection;
        RefreshChallengeButton.IsEnabled = !_challengeRunning && hasSelection;

        if (FindName("RunAutomatedChallengeButton") is Button automatedButton)
        {
            automatedButton.IsEnabled = !_challengeRunning
                                        && _challengeProgress?.CanPromote != true
                                        && hasSelection;
        }
    }

    private async void RefreshChallenge_Click(object sender, RoutedEventArgs e)
        => await RefreshChallengeProgressAsync();

    private async void RunAutomatedChallenge_Click(object sender, RoutedEventArgs e)
    {
        if (_challengeRunning
            || _challengeProgress?.CanPromote == true
            || ChallengeProfileComboBox.SelectedItem is not PerformanceProfile challenger
            || ChallengeRoleComboBox.SelectedItem is not ChallengeRoleOption role)
            return;

        var environment = App.Services.Environment.Capture();
        var instance = environment.Instances.FirstOrDefault(item =>
            string.Equals(item.Name, challenger.InstanceName, StringComparison.OrdinalIgnoreCase));
        if (instance is null)
        {
            ChallengeStatusText.Text = "A rodada automática não pode iniciar porque a instância BlueStacks do Custom não está disponível.";
            SetChallengeAutomationText("Instância BlueStacks compatível não encontrada.");
            return;
        }

        _challengeRunning = true;
        UpdateChallengeButton();
        ChallengeStatusText.Text = $"Executando 1 rodada A/B automática de {challenger.Name} contra {role.Name}.";
        SetChallengeAutomationText("Iniciando rodada controlada A/B...");
        SetChallengeAutomationProgress(indeterminate: true, value: 0, maximum: 2);

        try
        {
            var result = await App.Services.ProfileChallengeRounds.RunAsync(
                challenger.Id,
                role.Kind,
                environment,
                instance,
                ReportChallengeAutomationProgress);

            SetChallengeAutomationProgress(indeterminate: false, value: result.Success ? 2 : 0, maximum: 2);
            SetChallengeAutomationText(result.Success
                ? $"Rodada concluída · A {result.BaselineAcceptedSamples}/2 · B {result.CandidateAcceptedSamples}/2. {result.Message}"
                : $"Rodada interrompida com rollback seguro · A {result.BaselineAcceptedSamples}/2 · B {result.CandidateAcceptedSamples}/2. {result.Message}");
            ChallengeStatusText.Text = result.Success
                ? "Rodada A/B medida e salva no History. Atualizando automaticamente o progresso do desafio..."
                : $"A rodada A/B não gerou evidência elegível. {result.Message}";

            RefreshABComparison();
            await RefreshHistoricalValidationAsync();
            await RefreshChallengeProgressAsync();
        }
        catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            SetChallengeAutomationProgress(indeterminate: false, value: 0, maximum: 2);
            SetChallengeAutomationText($"Rodada não executada: {exception.Message}");
            ChallengeStatusText.Text = $"Rodada automática não executada: {exception.Message}";
            MessageBox.Show(exception.Message, "Rodada A/B automática");
        }
        finally
        {
            _challengeRunning = false;
            UpdateChallengeButton();
        }
    }

    private void ReportChallengeAutomationProgress(ProfileChallengeAutomationProgress progress)
    {
        void Update()
        {
            SetChallengeAutomationText(ProfileChallengeAutomationPresentation.Format(progress));
            var measuring = progress.Stage is ProfileChallengeRoundStage.MeasuringBaseline
                or ProfileChallengeRoundStage.MeasuringCandidate;
            SetChallengeAutomationProgress(
                indeterminate: !measuring && progress.Stage != ProfileChallengeRoundStage.Completed,
                value: measuring ? progress.AcceptedSamples : progress.Stage == ProfileChallengeRoundStage.Completed ? progress.RequiredSamples : 0,
                maximum: Math.Max(1, progress.RequiredSamples));
        }

        if (Dispatcher.CheckAccess()) Update();
        else Dispatcher.BeginInvoke(new Action(Update));
    }

    private void SetChallengeAutomationText(string text)
    {
        if (FindName("ChallengeAutomationText") is TextBlock statusText)
            statusText.Text = text;
    }

    private void SetChallengeAutomationProgress(bool indeterminate, int value, int maximum)
    {
        if (FindName("ChallengeAutomationProgressBar") is not ProgressBar progressBar) return;
        progressBar.Maximum = Math.Max(1, maximum);
        progressBar.IsIndeterminate = indeterminate;
        if (!indeterminate) progressBar.Value = Math.Clamp(value, 0, maximum);
    }

    private async void RunChallenge_Click(object sender, RoutedEventArgs e)
    {
        if (_challengeRunning
            || _challengeProgress?.CanPromote != true
            || ChallengeProfileComboBox.SelectedItem is not PerformanceProfile challenger
            || ChallengeRoleComboBox.SelectedItem is not ChallengeRoleOption role)
            return;

        _challengeRunning = true;
        UpdateChallengeButton();
        ChallengeStatusText.Text = $"Confirmando as duas rodadas medidas de {challenger.Name} contra {role.Name}...";
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
            else
            {
                await RefreshChallengeProgressAsync();
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

    private static string FormatChallengeRounds(ProfileChallengeProgress progress)
    {
        var prefix = progress.EligibleRounds <= 2
            ? $"{progress.EligibleRounds}/2 rodadas elegíveis"
            : $"{progress.EligibleRounds} rodadas elegíveis · usando as 2 mais recentes";
        if (progress.RecentRounds.Count == 0) return prefix;

        var rounds = string.Join(" · ", progress.RecentRounds.Select((round, index) =>
            $"R{index + 1} {VerdictName(round.Verdict)}"));
        return $"{prefix} · {rounds}";
    }

    private static string ProgressStatusName(ProfileChallengeProgressStatus status)
        => status switch
        {
            ProfileChallengeProgressStatus.AwaitingEvidence => "Aguardando evidência",
            ProfileChallengeProgressStatus.EnvironmentDrift => "Ambiente alterado",
            ProfileChallengeProgressStatus.Inconclusive => "Inconclusivo",
            ProfileChallengeProgressStatus.IncumbentHeld => "Vencedor atual mantido",
            ProfileChallengeProgressStatus.ReadyToPromote => "Pronto para promoção",
            _ => status.ToString()
        };

    private static string VerdictName(ProfileChallengeVerdict verdict)
        => verdict switch
        {
            ProfileChallengeVerdict.ChallengerWins => "Custom venceu",
            ProfileChallengeVerdict.IncumbentHolds => "vencedor manteve",
            ProfileChallengeVerdict.Inconclusive => "inconclusiva",
            _ => verdict.ToString()
        };

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
