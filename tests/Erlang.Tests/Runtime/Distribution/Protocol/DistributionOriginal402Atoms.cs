using Erlang;

namespace Erlang.Tests;

internal static class DistributionOriginal402Atoms
{
    internal static readonly Atom Returned = Term.A(DistributionOriginal402Names.Returned);
    internal static readonly Atom Caught = Term.A(DistributionOriginal402Names.Caught);
    internal static readonly Atom Protocol = Term.A(DistributionOriginal402Names.Protocol);
    internal static readonly Atom ProbeReturned = Term.A(DistributionOriginal402Names.ProbeReturned);
    internal static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom Error = Term.A(ErlangExceptionClasses.Error);
    internal static readonly Atom BadArgument = Term.A(ErlangErrorReasons.BadArgument);
    internal static readonly Atom NativeModule = Term.A(DistributionOriginal402Names.NativeModule);
    internal static readonly Atom NativeAbort = Term.A(DistributionOriginal402Names.NativeAbort);
    internal static readonly Atom WrongNode = Term.A(DistributionOriginal402Names.WrongNode);
    internal static readonly Atom Noname = Term.A(ProcessNodeDefaults.Local);
    internal static readonly Term NativeInfo = DistributionState402Atoms.NativeInfo;
}
