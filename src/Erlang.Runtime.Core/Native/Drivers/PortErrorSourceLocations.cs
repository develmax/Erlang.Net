namespace Erlang;

internal static class PortErrorSourceLocations
{
    internal static Term ErrorWithInfo { get; } = Create(PortErrorSourceLocationNames.ErrorWithInfoLine);
    internal static Term BadArgumentWithCause { get; } = Create(PortErrorSourceLocationNames.BadArgumentWithCauseLine);

    internal static Term Open(Term originalReason)
        => originalReason is Atom { Name: PortNames.BadOption }
            ? BadArgumentWithCause : ErrorWithInfo;

    private static Term Create(int line)
        => Term.List(
            Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(PortErrorSourceLocationNames.FileName)),
            Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
        );
}
