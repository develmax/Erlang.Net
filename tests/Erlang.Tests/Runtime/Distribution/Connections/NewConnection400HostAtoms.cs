using Erlang;

namespace Erlang.Tests;

internal static class NewConnection400HostAtoms
{
    internal static readonly Atom Remote = Term.A(NewConnection400HostNames.Remote);
    internal static readonly Atom Local = Term.A(NewConnection400HostNames.Local);
    internal static readonly Atom BadArgument = Term.A(ErlangErrorReasons.BadArgument);
    internal static readonly Atom SystemLimit = Term.A(ErlangErrorReasons.SystemLimit);
    internal static readonly Atom Killed = Term.A(ProcessExitReasons.Killed);
}
