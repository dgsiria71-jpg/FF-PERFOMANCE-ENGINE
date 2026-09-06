using System.IO;
using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App.Pages;

public partial class HistoryPage : UserControl
{
    private IReadOnlyList<PerformanceComparisonHistoryRecord> _comparisons = Array.Empty<PerformanceComparisonHistoryRecord>();

    public HistoryPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
        => await RefreshAsync();

    private async Task RefreshAsync()
    {
        HistoryList.ItemsSource = await App.Services.History.LoadAsync();
        _comparisons = await App.Services.History.LoadPerformanceComparisonsAsync();
        ComparisonHistoryList.ItemsSource = _comparisons;
        ComparisonHistoryEmptyText.Visibility = _comparisons.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshCurrentComparison();
    }

    private void RefreshCurrentComparison()
    {
        var comparison = App.Services.PerformanceComparison.CurrentComparison;
        SaveCurrentComparisonButton.IsEnabled = comparison is not null;
        if (comparison is null)
        {
            CurrentComparisonText.Text = "Nenhuma comparação A/B ativa.";
            CurrentComparisonDetailText.Text = "Capture A e B em Performance; depois salve aqui para reutilizar em outras sessões.";
            return;
        }

        CurrentComparisonText.Text = $"{comparison.Baseline.Name}  →  {comparison.Candidate.Name}";
        var delta = PerformanceIntervalPresentation.FromComparison(comparison.Metrics);
        CurrentComparisonDetailText.Text =
            $"B − A · Δ FPS {delta.AverageFpsDelta} · Δ Frame Time {delta.AverageFrameTimeDelta}. " +
            "Salvar preserva os snapshots como evidência observada; isso não cria um perfil validado.";
    }

    private async void SaveCurrentComparison_Click(object sender, RoutedEventArgs e)
    {
        var comparison = App.Services.PerformanceComparison.CurrentComparison;
        if (comparison is null)
        {
            MessageBox.Show("Capture A e B em Performance antes de salvar uma comparação.", "FF Performance Engine");
            return;
        }

        SaveCurrentComparisonButton.IsEnabled = false;
        try
        {
            var label = $"A/B · {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            var record = await App.Services.History.SavePerformanceComparisonAsync(label, comparison);
            CurrentComparisonDetailText.Text =
                $"{record.Label} salvo localmente como {record.ValidationStatus}. Uma validação medida separada ainda é necessária antes de originar perfil.";
            await RefreshAsync();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show($"Não foi possível salvar a comparação: {exception.Message}", "History");
            RefreshCurrentComparison();
        }
    }

    private void OpenComparison_Click(object sender, RoutedEventArgs e)
    {
        var record = FindRecord(sender);
        if (record is null) return;
        App.Services.PerformanceComparison.SetBaseline(record.Baseline);
        App.Services.PerformanceComparison.SetCandidate(record.Candidate);
        RefreshCurrentComparison();
        CurrentComparisonDetailText.Text = $"{record.Label} reaberto. A e B históricos foram copiados para a comparação ativa.";
    }

    private void UseCandidateAsBaseline_Click(object sender, RoutedEventArgs e)
    {
        var record = FindRecord(sender);
        if (record is null) return;
        App.Services.PerformanceComparison.SetBaseline(record.Candidate);
        RefreshCurrentComparison();
        CurrentComparisonDetailText.Text = $"B de {record.Label} foi carregado como A. Escolha outro B histórico ou capture um novo B para comparar sessões.";
    }

    private void UseCandidateAsCandidate_Click(object sender, RoutedEventArgs e)
    {
        var record = FindRecord(sender);
        if (record is null) return;
        App.Services.PerformanceComparison.SetCandidate(record.Candidate);
        RefreshCurrentComparison();
        CurrentComparisonDetailText.Text = $"B de {record.Label} foi carregado como B. O delta ativo usa apenas métricas realmente presentes nos dois snapshots.";
    }

    private async void RequestValidation_Click(object sender, RoutedEventArgs e)
    {
        var record = FindRecord(sender);
        if (record is null) return;

        try
        {
            var pending = await App.Services.History.RequestPerformanceValidationAsync(record.Id);
            await RefreshAsync();
            MessageBox.Show(
                $"{pending.Label} entrou em PendingValidation. Capture um novo B totalmente medido em Performance e depois use 'Validar com B atual'.",
                "Validação A/B");
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Não foi possível solicitar a validação: {exception.Message}", "Validação A/B");
        }
    }

    private async void CompleteValidation_Click(object sender, RoutedEventArgs e)
    {
        var record = FindRecord(sender);
        if (record is null) return;
        var currentCandidate = App.Services.PerformanceComparison.Candidate;
        if (currentCandidate is null)
        {
            MessageBox.Show(
                "Não existe B atual. Capture um novo B em Performance. O History não reutiliza o candidato original como validação.",
                "Validação A/B");
            return;
        }

        try
        {
            var validated = await App.Services.History.CompletePerformanceValidationAsync(record.Id, currentCandidate);
            await RefreshAsync();
            var projection = PerformanceProfileEvidenceBridge.FromValidatedRecord(validated);
            MessageBox.Show(
                $"Validação concluída com evidência {projection.Evidence}. FPS medido {Format(projection.AverageFps)} · Frame Time {Format(projection.FrameTimeMs)} ms. " +
                "A comparação agora é elegível para uma futura origem explícita de perfil; nenhum perfil foi criado automaticamente.",
                "Validação A/B");
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"A validação não foi concluída: {exception.Message}\n\nUse uma captura B posterior e com qualidade Measured; telemetria parcial nunca é promovida.",
                "Validação A/B");
        }
    }

    private PerformanceComparisonHistoryRecord? FindRecord(object sender)
    {
        if (sender is not Button { Tag: Guid id }) return null;
        return _comparisons.FirstOrDefault(record => record.Id == id);
    }

    private static string Format(double? value)
        => value is double number && double.IsFinite(number) ? number.ToString("0.0") : "—";
}
