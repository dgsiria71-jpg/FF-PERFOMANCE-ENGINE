namespace FFPerformanceEngine.Core.Services;

public static class AppPaths
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FFPerformanceEngine");
    public static string Settings => Path.Combine(Root, "settings.json");
    public static string Profiles => Path.Combine(Root, "profiles.json");
    public static string History => Path.Combine(Root, "history.json");
    public static string Snapshots => Path.Combine(Root, "snapshots.json");
}
