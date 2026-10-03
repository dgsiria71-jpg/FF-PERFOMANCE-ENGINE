using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App.Pages;

public partial class GuardianPage : UserControl
{
    private bool _updatingControls;
    private bool _subscribed;

    public GuardianPage()
    {
        InitializeComponent();
    }

    private void GuardianPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_subscribed)
        {
            App.Services.GuardianHost.StatusChanged += GuardianHost_StatusChanged;
            _subscribed = true;
        }

        PopulateControls();

        if (!App.Services.Settings.GuardianEnabled)
        {
            RenderDisabled();
            return;
        }

        RenderStatus(App.Services.GuardianHost.CurrentStatus ?? new GuardianLiveSessionStatus
        {
            Message = App.Services.GuardianHost.IsRunning
                ? "Guardian ativo. Aguardando a primeira medição real da sessão."
                : "Guardian aguardando uma instância BlueStacks explícita para iniciar."
        });
    }

    private void GuardianPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!_subscribed) return;
        App.Services.GuardianHost.StatusChanged -= GuardianHost_StatusChanged;
        _subscribed = false;
    }

    private void GuardianHost_StatusChanged(object? sender, GuardianLiveSessionStatus status)
    {
        _ = Dispatcher.BeginInvoke(() =>
        {
            if (App.Services.Settings.GuardianEnabled) RenderStatus(status);
        });
    }

    private void PopulateControls()
    {
        _updatingControls = true;
        try
        {
            GuardianEnabledToggle.IsChecked = App.Services.Settings.GuardianEnabled;
            SelectMode(App.Services.Settings.GuardianMode);

            var environment = App.Services.CaptureEnvironment();
            InstanceCombo.Items.Clear();
            foreach (var instance in environment.Instances)
            {
                InstanceCombo.Items.Add(new ComboBoxItem
                {
                    Content = instance.Name,
                    Tag = instance.Name
                });
            }

            var explicitName = App.Services.Settings.GuardianInstanceName;
            var activeName = App.Services.GuardianHost.InstanceName;
            var preferredName = !string.IsNullOrWhiteSpace(explicitName) ? explicitName : activeName;
            var selected = InstanceCombo.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(item.Tag as string, preferredName, StringComparison.OrdinalIgnoreCase));

            if (selected is not null)
            {
                InstanceCombo.SelectedItem = selected;
            }
            else if (environment.Instances.Count == 1)
            {
                InstanceCombo.SelectedIndex = 0;
            }

            InstanceHintText.Text = environment.Instances.Count switch
            {
                0 => "Nenhuma instância BlueStacks foi detectada.",
                1 => "Instância única detectada. O Guardian pode vinculá-la automaticamente.",
                _ when !string.IsNullOrWhiteSpace(explicitName) => "Seleção explícita salva. O Guardian não troca de instância silenciosamente.",
                _ => "Há várias instâncias. Escolha explicitamente qual o Guardian deve monitorar."
            };
        }
        finally
        {
            _updatingControls = false;
        }
    }

    private async void ModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingControls || !IsLoaded || ModeCombo.SelectedItem is not ComboBoxItem { Tag: string raw }) return;
        if (!Enum.TryParse<GuardianMode>(raw, out var mode)) return;

        await SaveGuardianSettingsAsync(App.Services.Settings with { GuardianMode = mode });
    }

    private async void GuardianEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_updatingControls || !IsLoaded) return;
        var enabled = GuardianEnabledToggle.IsChecked == true;
        await SaveGuardianSettingsAsync(App.Services.Settings with { GuardianEnabled = enabled });

        if (!enabled) RenderDisabled();
        else RenderStatus(App.Services.GuardianHost.CurrentStatus ?? new GuardianLiveSessionStatus
        {
            Message = App.Services.GuardianHost.IsRunning
                ? "Guardian ativo. Aguardando a primeira medição real da sessão."
                : "Selecione uma instância BlueStacks quando houver mais de uma disponível."
        });
    }

    private async void InstanceCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingControls || !IsLoaded || InstanceCombo.SelectedItem is not ComboBoxItem { Tag: string instanceName }) return;

        await SaveGuardianSettingsAsync(App.Services.Settings with
        {
            GuardianEnabled = true,
            GuardianInstanceName = instanceName
        });

        _updatingControls = true;
        GuardianEnabledToggle.IsChecked = true;
        _updatingControls = false;
        InstanceHintText.Text = "Seleção explícita salva. Monitoramento vinculado somente a esta instância.";

        RenderStatus(App.Services.GuardianHost.CurrentStatus ?? new GuardianLiveSessionStatus
        {
            Instance = new BlueStacksInstance { Name = instanceName },
            Message = "Guardian iniciando a medição da instância selecionada."
        });
    }

    private async Task SaveGuardianSettingsAsync(AppSettings settings)
    {
        try
        {
            await App.Services.SaveSettingsAsync(settings);
            ModeText.Text = GuardianPresentation.FromStatus(
                App.Services.GuardianHost.CurrentStatus ?? new GuardianLiveSessionStatus(),
                settings.GuardianMode).Mode;
        }
        catch (Exception ex)
        {
            DecisionText.Text = $"Não foi possível atualizar o Guardian: {ex.Message}";
        }
    }

    private void RenderDisabled()
    {
        RenderStatus(new GuardianLiveSessionStatus { Message = "Guardian desativado. Nenhuma alteração automática será executada." });
        StateText.Text = "○ Pausado";
        SessionText.Text = "Monitoramento desativado";
    }

    private void RenderStatus(GuardianLiveSessionStatus status)
    {
        var view = GuardianPresentation.FromStatus(status, App.Services.Settings.GuardianMode);
        StateText.Text = StateGlyph(view.StateLabel) + " " + view.StateLabel;
        ModeText.Text = view.Mode;
        SessionText.Text = view.SessionState;
        GameText.Text = view.Game;
        InstanceText.Text = view.Instance;
        PidText.Text = view.ProcessId;
        FpsText.Text = view.Fps;
        OneLowText.Text = view.OnePercentLow;
        FrameTimeText.Text = view.FrameTime;
        LatencyText.Text = view.Latency;
        BaselineText.Text = view.BaselineFps;
        BaselineConfidenceText.Text = view.BaselineConfidence;
        DecisionConfidenceText.Text = view.DecisionConfidence;
        DataQualityText.Text = view.DataQuality;
        InterventionText.Text = view.Intervention;
        DecisionText.Text = view.Detail;
    }

    private void SelectMode(GuardianMode mode)
    {
        foreach (var item in ModeCombo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string raw && Enum.TryParse<GuardianMode>(raw, out var candidate) && candidate == mode)
            {
                ModeCombo.SelectedItem = item;
                return;
            }
        }
    }

    private static string StateGlyph(string state) => state switch
    {
        "Recuperado" => "✓",
        "Investigando" or "Validando alteração" => "◉",
        "Atenção" => "!",
        _ => "●"
    };
}
