using Erlang;

namespace Erlang.Tests;

internal static class NetKernel400HostAtoms
{
    internal static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
    internal static readonly Atom Killed = Term.A(ProcessExitReasons.Killed);
    internal static readonly Atom BadArgument = Term.A(ErlangErrorReasons.BadArgument);
}
