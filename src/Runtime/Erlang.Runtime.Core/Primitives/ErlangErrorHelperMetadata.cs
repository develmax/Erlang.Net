namespace Erlang;

internal static class ErlangErrorHelperMetadata
{
    internal static MapTerm Info { get; } = new([
        new(Term.A(RuntimeExceptionNames.Module), Term.A(ErlangSizeErrorNames.Formatter))
    ]);

    internal static Term BadArgumentWithInfo { get; } = Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(PortErrorSourceLocationNames.FileName)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(ErlangErrorHelperNames.BadArgumentWithInfoLine))
    );
}
