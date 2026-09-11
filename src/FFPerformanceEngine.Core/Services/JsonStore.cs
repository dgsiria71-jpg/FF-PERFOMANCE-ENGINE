using System.Text.Json;

namespace FFPerformanceEngine.Core.Services;

public sealed class JsonStore<T> where T : class, new()
{
    private readonly string _path;
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public JsonStore(string path) => _path = path;

    public async Task<T> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path)) return new T();
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _options, cancellationToken).ConfigureAwait(false) ?? new T();
    }

    public async Task SaveAsync(T value, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer.SerializeAsync(stream, value, _options, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        File.Move(temp, _path, true);
    }
}
