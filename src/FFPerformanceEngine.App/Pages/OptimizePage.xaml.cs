using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

namespace FFPerformanceEngine.App.Pages;

public partial class OptimizePage : UserControl
{
    private AutoTunerMode _mode;
    private GameKind _game;
    private CancellationTokenSource? _tuningCts;
    private CancellationTokenSource? _capabilityCts;
    private bool _isRunning;
    private bool _pcOperationRunning;
    private bool _capabilityOperationRunning;
    private bool _capabilityInitializing;
    private bool _capabilityRecommendationPublished;
    private bool _initializing = true;
    private PersistentPcOptimizationPreview? _pcPreview;
    private Guid? _pcRestorePointId;
    private WindowsCapabilityCandidatePlan? _capabilityPlan;
    private WindowsCapabilityExperimentPresentation? _capabilityPresentation;

    private bool IsBusy => _isRunning || _pcOperationRunning || _capabilityOperationRunning || _capabilityInitializing;

    public OptimizePage()
    {
        InitializeComponent();

        _mode = App.Services.Settings.KeepDeepAsDefault
            ? AutoTunerMode.Deep
            : App.Services.Settings.DefaultTunerMode;
        _game = App.Services.Settings.PreferredGame is GameKind.FreeFire or GameKind.FreeFireMax
            ? App.Services.Settings.PreferredGame
            : GameKind.FreeFireMax;

        KeepDeepCheck.IsChecked = App.Services.Settings.KeepDeepAsDefault;
        PcAnalyzeButton.Click += PcAnalyze_Click;
        PcApplyButton.Click += PcApply_Click;
        PcRestoreButton.Click += PcRestore_Click;
        CapabilityExperimentCombo.SelectionChanged += CapabilityExperimentCombo_SelectionChanged;
        CapabilityCandidateCombo.SelectionChanged += CapabilityCandidateCombo_SelectionChanged;
        CapabilityRefreshButton.Click += CapabilityRefresh_Click;
        CapabilityRunButton.Click += CapabilityRun_Click;
        CapabilityCancelButton.Click += CapabilityCancel_Click;
        CapabilityValidateButton.Click += CapabilityValidate_Click;
        CapabilityPublishButton.Click += CapabilityPublish_Click;
        Loaded += OptimizePage_Loaded;
        ApplyChoiceVisuals();
        _initializing = false;
    }

    private async void OptimizePage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadEnvironmentSelection();
        RefreshReadiness();
        await RefreshCapabilityExperimentCatalogAsync();
    }

    private void LoadEnvironmentSelection()
    {
        _initializing = true;
        try
        {
            var environment = App.Services.Environment.Capture();
            if (App.Services.Settings.PreferredGame == GameKind.None &&
                environment.ActiveGame is GameKind.FreeFire or GameKind.FreeFireMax)
            {
                _game = environment.ActiveGame;
            }

            var previousInstance = InstanceCombo.SelectedItem as string;
            var names = environment.Instances.Select(instance => instance.Name).ToList();
            InstanceCombo.ItemsSource = names;

            if (!string.IsNullOrWhiteSpace(previousInstance) && names.Contains(previousInstance, StringComparer.OrdinalIgnoreCase))
                InstanceCombo.SelectedItem = names.First(name => string.Equals(name, previousInstance, StringComparison.OrdinalIgnoreCase));
            else if (names.Count > 0)
                InstanceCombo.SelectedIndex = 0;

            ApplyChoiceVisuals();
        }
        finally
        {
            _initializing = false;
        }
    }

    private async Task RefreshCapabilityExperimentCatalogAsync()
    {
        if (IsBusy) return;

        _capabilityInitializing = true;
        ApplyBusyState();
        CapabilityStatusText.Text = "Atualizando capabilities do Windows";
        CapabilityDetailText.Text = "Lendo o estado atual pelos adapters concretos antes de formar qualquer candidato.";
        CapabilityRunButton.IsEnabled = false;

        try
        {
            var previousId = (CapabilityExperimentCombo.SelectedItem as WindowsPerformanceCapability)?.CapabilityId;
            var capabilities = await App.Services.RefreshWindowsCapabilitiesAsync();
            var experimentable = capabilities
                .Where(capability => capability.Availability == CapabilityAvailability.Available
                                     && capability.Execution.SupportsApply
                                     && capability.Execution.SupportsRollback
                                     && capability.PersistenceScope != CapabilityPersistenceScope.SessionOnly)
                .OrderBy(capability => capability.Domain)
                .ThenBy(capability => capability.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            CapabilityExperimentCombo.ItemsSource = experimentable;
            if (!string.IsNullOrWhiteSpace(previousId))
            {
                CapabilityExperimentCombo.SelectedItem = experimentable.FirstOrDefault(capability =>
                    string.Equals(capability.CapabilityId, previousId, StringComparison.OrdinalIgnoreCase));
            }

            if (CapabilityExperimentCombo.SelectedItem is null && experimentable.Length > 0)
                CapabilityExperimentCombo.SelectedIndex = 0;

            if (experimentable.Length == 0)
            {
                ResetCapabilityPlan(
                    "Nenhuma capability persistente está disponível para experimento",
                    "Discovery não encontrou uma capability com apply + rollback comprovados neste Windows.");
                return;
            }
        }
        catch (Exception ex)
        {
            ResetCapabilityPlan("Falha ao atualizar capabilities", ex.Message);
            return;
        }
        finally
        {
            _capabilityInitializing = false;
            ApplyBusyState();
            RefreshReadiness();
        }

        await RefreshCapabilityPlanForSelectionAsync();
    }

    private async Task RefreshCapabilityPlanForSelectionAsync()
    {
        if (IsBusy) return;

        if (CapabilityExperimentCombo.SelectedItem is not WindowsPerformanceCapability capability)
        {
            ResetCapabilityPlan("Selecione uma capability", "Nenhum plano de experimento está ativo.");
            return;
        }

        _capabilityInitializing = true;
        ApplyBusyState();
        CapabilityExperimentIdText.Text = capability.CapabilityId;
        CapabilityStatusText.Text = "Construindo plano de exploração";
        CapabilityDetailText.Text = "O Core está atualizando o estado e limitando o espaço aos targets suportados pelo adapter.";
        ResetCapabilityEvidencePresentation();

        try
        {
            var plan = await App.Services.PlanWindowsCapabilityExperimentAsync(capability.CapabilityId);
            _capabilityPlan = plan;
            CapabilityCandidateCombo.ItemsSource = plan.Candidates;
            CapabilityCandidateCombo.SelectedIndex = plan.Candidates.Count > 0 ? 0 : -1;
            CapabilityBaselineText.Text = $"Baseline: {plan.CurrentValue ?? "—"}";
            CapabilityStatusText.Text = plan.CanExplore
                ? "Plano pronto para A/B controlado"
                : "Capability sem candidato executável";
            CapabilityDetailText.Text = plan.Reason;
        }
        catch (Exception ex)
        {
            ResetCapabilityPlan("Não foi possível planejar esta capability", ex.Message);
        }
        finally
        {
            _capabilityInitializing = false;
            ApplyBusyState();
            RefreshReadiness();
        }
    }

    private async void CapabilityExperimentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_capabilityInitializing || IsBusy) return;
        await RefreshCapabilityPlanForSelectionAsync();
    }

    private void CapabilityCandidateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_capabilityInitializing || IsBusy) return;
        ResetCapabilityEvidencePresentation();
        ApplyBusyState();
    }

    private async void CapabilityRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        await RefreshCapabilityExperimentCatalogAsync();
    }

    private async void CapabilityRun_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || CapabilityCandidateCombo.SelectedItem is not WindowsCapabilityCandidate candidate) return;

        BeginCapabilityOperation(
            "Executando A/B controlado",
            "Guardian será suspenso pela lease global; baseline e candidato serão medidos no mesmo workload antes da restauração obrigatória.");

        try
        {
            var run = await App.Services.RunWindowsCapabilityExperimentAsync(candidate, _capabilityCts!.Token);
            ApplyCapabilityPresentation(WindowsCapabilityExperimentPresentation.FromRun(run));
            CapabilityStatusText.Text = "Rodada controlada concluída e restaurada";
            CapabilityDetailText.Text = run.Validation.Reason;
        }
        catch (OperationCanceledException)
        {
            CapabilityStatusText.Text = "Experimento cancelado com segurança";
            CapabilityDetailText.Text = "O cancelamento foi observado pelo pipeline controlado; nenhuma recomendação foi criada.";
        }
        catch (Exception ex)
        {
            CapabilityStatusText.Text = "Experimento não concluído";
            CapabilityDetailText.Text = ex.Message;
        }
        finally
        {
            EndCapabilityOperation();
        }
    }

    private async void CapabilityValidate_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _capabilityPresentation?.PendingValidation is not WindowsCapabilityValidationDecision pending) return;

        BeginCapabilityOperation(
            "Validando candidato com rodada fresca",
            "A evidência repetida já passou pelo gate. Agora uma nova rodada controlada deve confirmar o mesmo tuple antes de existir ValidatedEvidence.");

        try
        {
            var evidence = await App.Services.ValidateWindowsCapabilityEvidenceAsync(pending, _capabilityCts!.Token);
            ApplyCapabilityPresentation(WindowsCapabilityExperimentPresentation.FromValidated(evidence));
            CapabilityStatusText.Text = "ValidatedEvidence criada e persistida";
            CapabilityDetailText.Text = "A terceira rodada fresca confirmou o candidato. A evidência durável ainda não altera o Windows nem publica recomendação sozinha.";
        }
        catch (OperationCanceledException)
        {
            CapabilityStatusText.Text = "Validação cancelada com segurança";
            CapabilityDetailText.Text = "Nenhuma ValidatedEvidence nova foi exposta sem concluir a rodada fresca e sua persistência.";
        }
        catch (Exception ex)
        {
            CapabilityStatusText.Text = "Candidato não validado";
            CapabilityDetailText.Text = ex.Message;
        }
        finally
        {
            EndCapabilityOperation();
        }
    }

    private async void CapabilityPublish_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _capabilityPresentation?.ValidatedEvidence is not WindowsCapabilityValidatedEvidence evidence) return;

        BeginCapabilityOperation(
            "Revalidando evidência para recomendação",
            "O Core vai confirmar latest evidence, fingerprint, workload, baseline e candidate-space antes da autoridade persistente publicar o target.");

        try
        {
            var result = await App.Services.PublishValidatedWindowsCapabilityRecommendationAsync(
                evidence,
                _capabilityCts!.Token);
            if (!result.IsPublished)
                throw new InvalidOperationException(result.Reason);

            _capabilityRecommendationPublished = true;
            CapabilityStatusText.Text = "Recomendação validada publicada";
            CapabilityDetailText.Text = "O target entrou no Registry como ValidatedEvidence. Nenhuma mutation foi aplicada automaticamente.";
            CapabilityRecommendationStatusText.Text = "Recomendação publicada · agora revise o preview em Otimizar este PC antes de aplicar.";
            CapabilityPublishButton.Content = "RECOMENDAÇÃO PUBLICADA";

            var preview = await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = preview.CanApply
                ? "Nova recomendação pronta para revisão"
                : "Recomendação publicada, sem mutation pendente";
            PcOptimizationDetailText.Text = preview.CanApply
                ? "O preview foi reconstruído com estado atual comprovado. Revise a capability antes de aplicar."
                : "O estado atual já pode coincidir com o target ou outro gate persistente bloqueou a mutation.";
        }
        catch (OperationCanceledException)
        {
            CapabilityStatusText.Text = "Publicação cancelada";
            CapabilityDetailText.Text = "A recomendação não foi publicada nesta tentativa.";
        }
        catch (Exception ex)
        {
            CapabilityStatusText.Text = "Recomendação não publicada";
            CapabilityDetailText.Text = ex.Message;
        }
        finally
        {
            EndCapabilityOperation();
        }
    }

    private void CapabilityCancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_capabilityOperationRunning || _capabilityCts is null) return;

        CapabilityCancelButton.IsEnabled = false;
        CapabilityStatusText.Text = "Cancelando com segurança";
        CapabilityDetailText.Text = "A solicitação foi enviada. O pipeline terminará cleanup/rollback obrigatório antes de encerrar a operação.";
        _capabilityCts.Cancel();
    }

    private void BeginCapabilityOperation(string status, string detail)
    {
        _capabilityCts = new CancellationTokenSource();
        _capabilityOperationRunning = true;
        CapabilityStatusText.Text = status;
        CapabilityDetailText.Text = detail;
        ApplyBusyState();
    }

    private void EndCapabilityOperation()
    {
        _capabilityCts?.Dispose();
        _capabilityCts = null;
        _capabilityOperationRunning = false;
        ApplyBusyState();
        RefreshReadiness();
    }

    private void ApplyCapabilityPresentation(WindowsCapabilityExperimentPresentation presentation)
    {
        _capabilityPresentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        _capabilityRecommendationPublished = false;
        CapabilityPublishButton.Content = "USAR COMO RECOMENDAÇÃO";
        CapabilityEvidencePanel.Visibility = Visibility.Visible;
        CapabilityEvidenceStateText.Text = presentation.StateLabel;
        CapabilityEvidenceReasonText.Text = presentation.Reason;
        CapabilityEvidenceTargetText.Text = $"{presentation.BaselineValue} → {presentation.CandidateTarget}";
        CapabilityEvidenceTupleText.Text = presentation.CapabilityId;
        CapabilityRoundsText.Text = presentation.ObservationCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        CapabilityConsistencyText.Text = presentation.Consistency.ToString("P0", System.Globalization.CultureInfo.InvariantCulture);
        CapabilityFpsDeltaText.Text = FormatSignedPercent(presentation.FpsDeltaPercent);
        CapabilityFrameTimeDeltaText.Text = FormatSignedPercent(presentation.FrameTimeImprovementPercent);
        CapabilityLatencyDeltaText.Text = FormatSignedPercent(presentation.LatencyImprovementPercent);
        CapabilityValidateButton.Visibility = presentation.CanValidate ? Visibility.Visible : Visibility.Collapsed;
        CapabilityPublishButton.Visibility = presentation.CanPublishRecommendation ? Visibility.Visible : Visibility.Collapsed;
        CapabilityRecommendationStatusText.Text = presentation.CanValidate
            ? "Evidência repetida pronta · falta uma rodada fresca de validação explícita."
            : presentation.CanPublishRecommendation
                ? "ValidatedEvidence durável pronta · publicação ainda revalida contexto e não aplica mutation."
                : "Continue repetindo A/B apenas se o Core mantiver o candidato no espaço suportado.";
        ApplyBusyState();
    }

    private void ResetCapabilityEvidencePresentation()
    {
        _capabilityPresentation = null;
        _capabilityRecommendationPublished = false;
        CapabilityEvidencePanel.Visibility = Visibility.Collapsed;
        CapabilityValidateButton.Visibility = Visibility.Collapsed;
        CapabilityPublishButton.Visibility = Visibility.Collapsed;
        CapabilityValidateButton.IsEnabled = false;
        CapabilityPublishButton.IsEnabled = false;
        CapabilityPublishButton.Content = "USAR COMO RECOMENDAÇÃO";
    }

    private void ResetCapabilityPlan(string status, string detail)
    {
        _capabilityPlan = null;
        CapabilityCandidateCombo.ItemsSource = null;
        CapabilityCandidateCombo.SelectedItem = null;
        CapabilityBaselineText.Text = "Baseline: —";
        CapabilityExperimentIdText.Text = (CapabilityExperimentCombo.SelectedItem as WindowsPerformanceCapability)?.CapabilityId ?? "—";
        CapabilityStatusText.Text = status;
        CapabilityDetailText.Text = detail;
        CapabilityRunButton.IsEnabled = false;
        ResetCapabilityEvidencePresentation();
    }

    private async void PcAnalyze_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;

        SetPcOperationState(true);
        PcOptimizationStatusText.Text = "Analisando estado real do Windows";
        PcOptimizationDetailText.Text = "Atualizando capabilities e construindo o preview a partir do estado atual comprovado.";

        try
        {
            var presentation = await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = presentation.CanApply
                ? "Preview pronto para revisão"
                : "Nenhuma alteração persistente elegível";
            PcOptimizationDetailText.Text = presentation.CanApply
                ? "Revise cada capability abaixo. Aplicar usa exatamente este preview e revalida tudo antes do primeiro snapshot/mutation."
                : "O Core manteve todas as capabilities como SKIP. Nenhuma alteração será aplicada até existir recomendação elegível para este PC.";
        }
        catch (Exception ex)
        {
            ResetPersistentPcPreview();
            PcOptimizationStatusText.Text = "Não foi possível analisar este PC";
            PcOptimizationDetailText.Text = ex.Message;
        }
        finally
        {
            SetPcOperationState(false);
            RefreshReadiness();
        }
    }

    private async void PcApply_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _pcPreview?.CanApply != true) return;

        var preview = _pcPreview;
        SetPcOperationState(true);
        PcOptimizationStatusText.Text = "Aplicando preview com proteção transacional";
        PcOptimizationDetailText.Text = "Aguardando a lease global, revalidando fingerprint/estado e criando restore point antes de qualquer mutation.";

        try
        {
            var result = await App.Services.PersistentPcOptimization.ApplyAsync(preview);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);

            _pcRestorePointId = result.RestorePointId;
            PcRestorePointText.Text = $"Restore point: {ShortId(result.RestorePointId)} · disponível para desfazer esta aplicação";
            PcRestoreButton.Visibility = Visibility.Visible;

            var refreshed = await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = "Otimização persistente aplicada";
            PcOptimizationDetailText.Text = refreshed.CanApply
                ? "A aplicação foi verificada e registrada. O estado mudou novamente durante a atualização do preview; revise antes de qualquer nova aplicação."
                : "A aplicação foi verificada e registrada no History. O restore point preserva o estado exato anterior.";
        }
        catch (PersistentPcOptimizationDriftException ex)
        {
            ResetPersistentPcPreview();
            PcOptimizationStatusText.Text = "Preview ficou desatualizado";
            PcOptimizationDetailText.Text = $"{ex.Message} Analise este PC novamente antes de aplicar.";
        }
        catch (Exception ex)
        {
            PcOptimizationStatusText.Text = "Aplicação não concluída";
            PcOptimizationDetailText.Text = $"{ex.Message} O backend transacional mantém rollback/History como autoridade de recuperação.";
        }
        finally
        {
            SetPcOperationState(false);
            RefreshReadiness();
        }
    }

    private async void PcRestore_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _pcRestorePointId is not Guid restorePointId) return;

        SetPcOperationState(true);
        PcOptimizationStatusText.Text = "Restaurando estado anterior";
        PcOptimizationDetailText.Text = "A restauração usa a mesma lease global e o restore point durável criado antes da aplicação.";

        try
        {
            var result = await App.Services.PersistentPcOptimization.RestoreAsync(restorePointId);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);

            _pcRestorePointId = null;
            PcRestorePointText.Text = string.Empty;
            PcRestoreButton.Visibility = Visibility.Collapsed;
            await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = "Estado anterior restaurado";
            PcOptimizationDetailText.Text = "As capabilities do restore point foram verificadas após o rollback. Um novo preview pode ser aplicado somente depois da revalidação atual.";
        }
        catch (Exception ex)
        {
            PcOptimizationStatusText.Text = "Restauração não concluída";
            PcOptimizationDetailText.Text = $"{ex.Message} Consulte History para o estado auditado da transação.";
        }
        finally
        {
            SetPcOperationState(false);
            RefreshReadiness();
        }
    }

    private async Task<PersistentPcOptimizationPresentation> RefreshPersistentPcPreviewCoreAsync()
    {
        var preview = await App.Services.PersistentPcOptimization.AnalyzeAsync();
        _pcPreview = preview;

        var presentation = PersistentPcOptimizationPresentation.Create(preview);
        PcOptimizationEntries.ItemsSource = presentation.Entries;
        PcOptimizationEntries.Visibility = presentation.TotalCount > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        PcOptimizationCountsText.Text = $"{presentation.ReadyCount} pronta(s) · {presentation.SkippedCount} ignorada(s) · {presentation.TotalCount} total";
        PcApplyButton.IsEnabled = presentation.CanApply && !IsBusy;
        return presentation;
    }

    private void ResetPersistentPcPreview()
    {
        _pcPreview = null;
        PcOptimizationEntries.ItemsSource = null;
        PcOptimizationEntries.Visibility = Visibility.Collapsed;
        PcOptimizationCountsText.Text = "Preview indisponível";
        PcApplyButton.IsEnabled = false;
    }

    private void SetPcOperationState(bool running)
    {
        _pcOperationRunning = running;
        ApplyBusyState();
    }

    private void ApplyBusyState()
    {
        var idle = !IsBusy;

        PcAnalyzeButton.IsEnabled = idle;
        PcApplyButton.IsEnabled = idle && _pcPreview?.CanApply == true;
        PcRestoreButton.IsEnabled = idle && _pcRestorePointId.HasValue;

        AdaptiveModeButton.IsEnabled = idle;
        DeepModeButton.IsEnabled = idle;
        FreeFireButton.IsEnabled = idle;
        FreeFireMaxButton.IsEnabled = idle;
        InstanceCombo.IsEnabled = idle;
        KeepDeepCheck.IsEnabled = idle;
        if (!idle) StartButton.IsEnabled = false;

        CapabilityExperimentCombo.IsEnabled = idle;
        CapabilityCandidateCombo.IsEnabled = idle;
        CapabilityRefreshButton.IsEnabled = idle;
        CapabilityRunButton.IsEnabled = idle
                                        && _capabilityPlan?.CanExplore == true
                                        && CapabilityCandidateCombo.SelectedItem is WindowsCapabilityCandidate;
        CapabilityValidateButton.IsEnabled = idle && _capabilityPresentation?.CanValidate == true;
        CapabilityPublishButton.IsEnabled = idle
                                            && _capabilityPresentation?.CanPublishRecommendation == true
                                            && !_capabilityRecommendationPublished;
        CapabilityCancelButton.Visibility = _capabilityOperationRunning ? Visibility.Visible : Visibility.Collapsed;
        CapabilityCancelButton.IsEnabled = _capabilityOperationRunning;

        CancelButton.Visibility = _isRunning ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.IsEnabled = _isRunning;
    }

    private void AdaptiveMode_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _mode = AutoTunerMode.Adaptive;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void DeepMode_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _mode = AutoTunerMode.Deep;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void FreeFire_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _game = GameKind.FreeFire;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void FreeFireMax_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _game = GameKind.FreeFireMax;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void InstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || IsBusy) return;
        RefreshReadiness();
    }

    private async void KeepDeepCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing || IsBusy) return;

        var keepDeep = KeepDeepCheck.IsChecked == true;
        if (keepDeep)
            _mode = AutoTunerMode.Deep;

        ApplyChoiceVisuals();
        RefreshReadiness();

        var updated = App.Services.Settings with
        {
            KeepDeepAsDefault = keepDeep,
            DefaultTunerMode = keepDeep ? AutoTunerMode.Deep : _mode
        };

        try
        {
            await App.Services.SaveSettingsAsync(updated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReadinessStatusText.Text = "Não foi possível salvar a preferência";
            ReadinessDetailText.Text = ex.Message;
        }
    }

    private void RefreshReadiness()
    {
        if (_initializing || IsBusy) return;

        var selectedInstance = InstanceCombo.SelectedItem as string;
        OptimizeReadiness readiness;
        try
        {
            readiness = App.Services.OptimizeWorkflow.Analyze(_game, _mode, selectedInstance);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReadinessStatusText.Text = "Não foi possível analisar o ambiente";
            ReadinessDetailText.Text = ex.Message;
            InstanceStatusText.Text = "Instância: —";
            TelemetryStatusText.Text = "PresentMon: —";
            CandidateCountText.Text = "0 candidatos";
            StartButton.IsEnabled = false;
            return;
        }

        ReadinessStatusText.Text = readiness.CanStart ? "Pronto para otimizar" : "Atenção necessária";
        ReadinessDetailText.Text = readiness.Message;
        InstanceStatusText.Text = readiness.Instance is null
            ? "Instância: —"
            : $"Instância: {readiness.Instance.Name}";
        TelemetryStatusText.Text = App.Services.PresentMon.FindExecutable() is null
            ? "PresentMon: ausente"
            : "PresentMon: pronto";
        CandidateCountText.Text = $"{readiness.Candidates.Count} candidato(s)";
        StartButton.IsEnabled = readiness.CanStart && !IsBusy;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;

        var selectedInstance = InstanceCombo.SelectedItem as string;
        var readiness = App.Services.OptimizeWorkflow.Analyze(_game, _mode, selectedInstance);
        if (!readiness.CanStart || readiness.Instance is null)
        {
            ReadinessStatusText.Text = "Atenção necessária";
            ReadinessDetailText.Text = readiness.Message;
            StartButton.IsEnabled = false;
            return;
        }

        _tuningCts = new CancellationTokenSource();
        _isRunning = true;
        SetRunningState(true);
        ResultCard.Visibility = Visibility.Collapsed;
        RunCard.Visibility = Visibility.Visible;
        ApplyProgress(new AutoTunerProgressPresentation(0, "Preparando", "Otimizando seu sistema", "Criando snapshot e preparando a primeira medição segura."));

        try
        {
            var result = await App.Services.OptimizeWorkflow.RunAsync(
                _game,
                _mode,
                readiness.Instance.Name,
                visual => Dispatcher.BeginInvoke(new Action(() => ApplyProgress(visual))),
                _tuningCts.Token);

            ShowResult(result);
        }
        catch (OperationCanceledException)
        {
            StageLabelText.Text = "CANCELADO";
            RunTitleText.Text = "Otimização cancelada com segurança";
            RunDetailText.Text = "A solicitação foi concluída somente após a restauração obrigatória da configuração de referência.";
        }
        catch (Exception ex)
        {
            StageLabelText.Text = "INTERROMPIDO";
            RunTitleText.Text = "Sessão encerrada";
            RunDetailText.Text = $"{ex.Message} Verifique History antes de iniciar outra sessão.";
        }
        finally
        {
            _tuningCts.Dispose();
            _tuningCts = null;
            _isRunning = false;
            SetRunningState(false);
            LoadEnvironmentSelection();
            RefreshReadiness();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRunning || _tuningCts is null) return;

        CancelButton.IsEnabled = false;
        StageLabelText.Text = "CANCELANDO";
        RunTitleText.Text = "Cancelando com segurança";
        RunDetailText.Text = "A solicitação foi recebida. O rollback obrigatório terminará antes de a sessão ser encerrada.";
        _tuningCts.Cancel();
    }

    private void SetRunningState(bool running)
    {
        _ = running;
        ApplyBusyState();
    }

    private void ApplyProgress(AutoTunerProgressPresentation visual)
    {
        RunCard.Visibility = Visibility.Visible;
        StageLabelText.Text = visual.StageLabel.ToUpperInvariant();
        RunTitleText.Text = visual.Title;
        RunDetailText.Text = visual.Detail;
        RunProgress.Value = visual.Percent;
        ProgressPercentText.Text = $"{visual.Percent}%";
        RunPercentHero.Text = $"{visual.Percent}%";
    }

    private void ShowResult(OptimizeWorkflowResult result)
    {
        ApplyProgress(new AutoTunerProgressPresentation(
            100,
            result.Session.ProfilesPersisted ? "Concluído e salvo" : "Concluído",
            result.Session.ProfilesPersisted ? "Otimização validada" : "Otimização concluída sem novo vencedor",
            result.Session.ProfilesPersisted
                ? "O benchmark terminou, o baseline foi restaurado e os perfis vencedores validados foram persistidos."
                : "Nenhum resultado inconclusivo substituiu os perfis conhecidos."));

        ResultCard.Visibility = Visibility.Visible;
        ResultSummaryText.Text = result.Summary;
        PersistenceStatusText.Text = result.Session.ProfilesPersisted
            ? "VALIDADO E SALVO"
            : "PERFIS PRESERVADOS";

        var recommended = result.Recommended;
        if (recommended is null)
        {
            ResultFpsText.Text = "—";
            ResultLowText.Text = "—";
            ResultFrameTimeText.Text = "—";
            ResultLatencyText.Text = "—";
            ResultConfidenceText.Text = "—";
            RecommendedConfigText.Text = "Nenhum perfil novo atingiu evidência suficiente para substituir a configuração conhecida.";
        }
        else
        {
            ResultFpsText.Text = FormatNumber(recommended.AverageFps, "0.0");
            ResultLowText.Text = FormatNumber(recommended.OnePercentLow, "0.0");
            ResultFrameTimeText.Text = FormatMetric(recommended.FrameTimeMs, "0.00", "ms");
            ResultLatencyText.Text = FormatMetric(recommended.LatencyMs, "0.0", "ms");
            ResultConfidenceText.Text = $"{Math.Round(Math.Clamp(recommended.Confidence, 0, 1) * 100, MidpointRounding.AwayFromZero):0}%";
            RecommendedConfigText.Text = $"{recommended.CpuCores} cores · {recommended.RamMb / 1024d:0.#} GB RAM · {recommended.Renderer} · alvo {recommended.FpsTarget} FPS · {recommended.Resolution}";
        }

        var winnerNames = result.Session.Tuning.Winners
            .Where(profile => profile.Evidence == EvidenceLevel.Validated)
            .Select(profile => profile.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        WinnersText.Text = winnerNames.Count == 0
            ? "Nenhum vencedor novo foi persistido; o último conjunto confiável permanece intacto."
            : $"Vencedores validados: {string.Join(" · ", winnerNames)}";
    }

    private void ApplyChoiceVisuals()
    {
        SetChoice(AdaptiveModeButton, _mode == AutoTunerMode.Adaptive);
        SetChoice(DeepModeButton, _mode == AutoTunerMode.Deep);
        SetSmallChoice(FreeFireButton, _game == GameKind.FreeFire);
        SetSmallChoice(FreeFireMaxButton, _game == GameKind.FreeFireMax);
    }

    private void SetChoice(Button button, bool selected)
    {
        button.Background = (Brush)FindResource(selected ? "GlassStrongBrush" : "GlassBrush");
        button.BorderBrush = (Brush)FindResource(selected ? "AccentBrush" : "GlassBorderBrush");
        button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
    }

    private void SetSmallChoice(Button button, bool selected)
    {
        button.Background = (Brush)FindResource(selected ? "AccentSoftBrush" : "GlassBrush");
        button.Foreground = (Brush)FindResource("TextBrush");
        button.BorderBrush = (Brush)FindResource(selected ? "AccentBrush" : "GlassBorderBrush");
        button.BorderThickness = new Thickness(1);
    }

    private static string ShortId(Guid value)
        => value.ToString("N")[..8].ToUpperInvariant();

    private static string FormatNumber(double? value, string format)
        => value is double number ? number.ToString(format, System.Globalization.CultureInfo.InvariantCulture) : "—";

    private static string FormatMetric(double? value, string format, string unit)
        => value is double number
            ? $"{number.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {unit}"
            : "—";

    private static string FormatSignedPercent(double value)
        => $"{(value > 0 ? "+" : string.Empty)}{value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}%";
}
