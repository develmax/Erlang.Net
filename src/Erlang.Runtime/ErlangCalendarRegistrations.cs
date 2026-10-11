namespace Erlang;

internal static class ErlangCalendarRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, int arity, Func<IReadOnlyList<Term>, Term> body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            arity,
            (_, arguments) => ValueTask.FromResult(body(arguments))
        );
        foreach (string operation in new[] { ErlangCalendarNames.Date, ErlangCalendarNames.Time, ErlangCalendarNames.Local, ErlangCalendarNames.Universal })
            Add(operation, 0, _ => ErlangCalendar.Current(operation));
        Add(ErlangCalendarNames.LocalToUniversal, 1, ErlangCalendar.ToUniversal);
        Add(ErlangCalendarNames.LocalToUniversal, 2, ErlangCalendar.ToUniversal);
        Add(ErlangCalendarNames.UniversalToLocal, 1, arguments => ErlangCalendar.ToLocal(arguments[0]));
        Add(ErlangCalendarNames.UniversalToEpoch, 1, arguments => ErlangCalendar.ToEpoch(arguments[0]));
        Add(ErlangCalendarNames.EpochToUniversal, 1, arguments => ErlangCalendar.FromEpoch(arguments[0]));
    }
}
