using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class SettingsService
{
    private readonly JsonStore<AppSettings> _store;
    public SettingsService(string? path = null) => _store = new JsonStore<AppSettings>(path ?? AppPaths.Settings);
    public Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default) => _store.LoadAsync(cancellationToken);
    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default) => _store.SaveAsync(settings, cancellationToken);
}
