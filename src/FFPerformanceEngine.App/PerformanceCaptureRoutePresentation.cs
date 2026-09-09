namespace FFPerformanceEngine.App;

public sealed record PerformanceCaptureRoutePresentation
{
    public bool CanMeasure { get; init; }
    public string TargetText { get; init; } = string.Empty;
    public string TargetDetail { get; init; } = string.Empty;
    public string CaptureDetail { get; init; } = string.Empty;

    public static PerformanceCaptureRoutePresentation FromRoute(PerformanceCaptureRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (route.UsesSelectedWorkload)
        {
            var gameId = string.IsNullOrWhiteSpace(route.SelectedGameId)
                ? "workload selecionado"
                : route.SelectedGameId;
            var target = route.WorkloadTarget;
            if (target?.CanCaptureProcess == true && target.ProcessId is int processId)
            {
                return new PerformanceCaptureRoutePresentation
                {
                    CanMeasure = true,
                    TargetText = $"Jogo {gameId} · PID {processId}",
                    TargetDetail = "RunningProcess exato vinculado ao GameId selecionado será medido; não há fallback para Guardian nem seleção aproximada de outro processo.",
                    CaptureDetail = $"Capturando o PID {processId} vinculado ao workload {gameId}."
                };
            }

            return new PerformanceCaptureRoutePresentation
            {
                CanMeasure = false,
                TargetText = $"Jogo {gameId} · processo indisponível",
                TargetDetail = $"O workload {gameId} permanece selecionado, mas não existe um único RunningProcess exato. A captura fica bloqueada e não cai para Guardian.",
                CaptureDetail = $"Captura bloqueada para {gameId}: aguardando um único RunningProcess exato."
            };
        }

        var guardianTarget = route.GuardianTarget;
        if (guardianTarget?.CanCapture == true)
        {
            return new PerformanceCaptureRoutePresentation
            {
                CanMeasure = true,
                TargetText = $"Instância {guardianTarget.InstanceName} · PID {guardianTarget.ProcessId}",
                TargetDetail = "O mesmo processo BlueStacks vinculado pelo Guardian será medido; não há seleção aproximada de outro HD-Player.",
                CaptureDetail = "Capturando o PID vinculado pelo Guardian."
            };
        }

        return new PerformanceCaptureRoutePresentation
        {
            CanMeasure = false,
            TargetText = "Aguardando vínculo exato do Guardian",
            TargetDetail = "A captura permanece bloqueada até existir uma instância e um PID BlueStacks inequívocos no Guardian.",
            CaptureDetail = "Aguardando vínculo exato do Guardian para iniciar a captura."
        };
    }
}
