using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public static class GuardianStartupPolicy
{
    public static string? SelectInstanceName(AppSettings settings, EnvironmentSnapshot environment)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(environment);

        if (!settings.GuardianEnabled) return null;

        if (!string.IsNullOrWhiteSpace(settings.GuardianInstanceName))
        {
            return environment.Instances
                .FirstOrDefault(instance => string.Equals(
                    instance.Name,
                    settings.GuardianInstanceName,
                    StringComparison.OrdinalIgnoreCase))
                ?.Name;
        }

        return environment.Instances.Count == 1
            ? environment.Instances[0].Name
            : null;
    }
}
