using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.App.Pages;

public partial class PromotedWinnerProvenanceView : UserControl
{
    private int _refreshRevision;

    public PromotedWinnerProvenanceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
        => await RefreshAsync();

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _refreshRevision++;
        Apply(UniversalPromotedProfileProvenancePresentation.FromProjection(null));
    }

    private async void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded) await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var revision = ++_refreshRevision;
        Apply(UniversalPromotedProfileProvenancePresentation.FromProjection(null));

        if (DataContext is not PerformanceProfile profile)
            return;

        var requestedId = profile.Id;
        var projection = await App.Services
            .ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync(requestedId);

        if (revision != _refreshRevision
            || !IsLoaded
            || DataContext is not PerformanceProfile current
            || current.Id != requestedId)
            return;

        Apply(UniversalPromotedProfileProvenancePresentation.FromProjection(projection));
    }

    private void Apply(UniversalPromotedProfileProvenancePresentation presentation)
    {
        Visibility = presentation.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        if (!presentation.IsVisible)
        {
            WinnerText.Text = string.Empty;
            IdentityText.Text = string.Empty;
            CandidateText.Text = string.Empty;
            return;
        }

        WinnerText.Text = presentation.ProfileKind is null
            ? presentation.ProfileName
            : $"{presentation.ProfileName} · {presentation.ProfileKind.Value}";
        IdentityText.Text = $"GameId {presentation.GameId} · Adapter {presentation.AdapterId}";
        CandidateText.Text = string.Join("\n", presentation.CandidateLines);
    }
}
