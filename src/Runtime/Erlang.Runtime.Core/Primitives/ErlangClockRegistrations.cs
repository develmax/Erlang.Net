namespace Erlang;

internal static class ErlangClockRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<IReadOnlyList<Term>, Term> body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            arity,
            (_, arguments) => ValueTask.FromResult(body(arguments))
        );
        Add(ErlangClockNames.Convert, 3, ErlangClockState.Convert);
        foreach (string clock in new[] { ErlangClockNames.MonotonicTime, ErlangClockNames.SystemTime, ErlangClockNames.TimeOffset })
        {
            Add(clock, 0, arguments => ErlangClockState.Clock(clock, arguments));
            Add(clock, 1, arguments => ErlangClockState.Clock(clock, arguments));
        }
        Add(ErlangClockNames.Timestamp, 0, _ => ErlangClockState.Timestamp(false));
        Add(ErlangClockNames.Now, 0, _ => ErlangClockState.Timestamp(true));
        foreach (int arity in new[] { 0, 1 })
            registry.Register(
                ErlangModuleNames.Module,
                ErlangClockNames.UniqueInteger,
                arity,
                (context, arguments) => ValueTask.FromResult(ErlangClockState.Unique(context, arguments))
            );
    }
}
