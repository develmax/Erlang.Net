namespace Erlang.Differential;

public static class EarlyPlatformCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "early-platform-supervised-record-timer-collections",
            File.ReadAllText("examples/HelloHybrid/platform_demo.erl"),
            Term.Tuple(Term.A("ok"), Term.Tuple(Term.A("platform_ready"), Term.I(100), Term.List(Term.I(2), Term.I(3))))
        )
    ];
}
