namespace Erlang.Otp;

public sealed record SupervisorConfiguration(
    IReadOnlyList<ChildSpec> Children,
    RestartStrategy Strategy = RestartStrategy.OneForOne,
    int Intensity = BehaviourDefaults.RestartIntensity,
    TimeSpan? Period = null,
    bool Ignore = false
);
