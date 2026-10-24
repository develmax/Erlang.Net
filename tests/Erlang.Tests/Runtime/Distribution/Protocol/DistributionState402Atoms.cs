using Erlang;

namespace Erlang.Tests;

internal static class DistributionState402Atoms
{
    internal static readonly Atom Module = Term.A(DistributionState402Names.ModuleName);
    internal static readonly Atom Abort = Term.A(DistributionState402Names.AbortFunction);
    internal static readonly Atom Remote = Term.A(DistributionState402Names.Remote);
    internal static readonly Atom Other = Term.A(DistributionState402Names.Other);
    internal static readonly Atom BadHandle = Term.A(DistributionState402Names.MalformedHandle);
    internal static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
    internal static readonly Atom BadArgument = Term.A(ErlangErrorReasons.BadArgument);
    internal static readonly Atom Undefined = Term.A(DistributionState402Names.Undefined);
    internal static readonly Atom Killed = Term.A(ProcessExitReasons.Killed);
    internal static readonly Atom Ok = Term.A(DistributionState402Names.Success);
    internal static readonly Term NativeInfo = Term.List(Term.Tuple(
        Term.A(DistributionState402Names.ErrorInfo),
        new MapTerm([new(Term.A(DistributionState402Names.InfoModule), Term.A(DistributionState402Names.NativeFormatter))])
    ));
}
