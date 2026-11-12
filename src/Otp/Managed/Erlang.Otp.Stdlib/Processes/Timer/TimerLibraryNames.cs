namespace Erlang;

internal static class TimerLibraryNames
{
    internal const string Module = TimerLibraryModuleNames.Module;
    internal const string Server = TimerProtocolAtoms.Server;
    internal static readonly Term Ok = Term.A(CommonAtomNames.Ok);
    internal static readonly Term Error = Term.A(CommonAtomNames.Error);
    internal static readonly Term BadArgument = Term.Tuple(Error, Term.A(ErlangErrorReasons.BadArgument));
    internal static readonly Term Cancelled = Term.Tuple(Ok, Term.A(TimerResultNames.Cancelled));
    internal static readonly Term Absolute = Term.List(Term.Tuple(Term.A(TimerProtocolAtoms.Absolute), Term.A(ErlangBooleanAtoms.True)));
    internal static readonly Term CancelOptions = Term.List(
        Term.Tuple(Term.A(TimerProtocolAtoms.Async), Term.A(ErlangBooleanAtoms.True)),
        Term.Tuple(Term.A(TimerProtocolAtoms.Info), Term.A(ErlangBooleanAtoms.False))
    );
}
