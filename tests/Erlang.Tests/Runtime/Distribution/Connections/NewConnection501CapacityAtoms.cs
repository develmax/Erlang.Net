using Erlang;

namespace Erlang.Tests;

internal static class NewConnection501CapacityAtoms
{
    internal static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom SystemLimit = Term.A(ErlangErrorReasons.SystemLimit);
}
