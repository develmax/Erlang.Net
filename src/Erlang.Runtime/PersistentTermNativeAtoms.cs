namespace Erlang;

internal static class PersistentTermNativeAtoms
{
    internal static readonly Atom Ok = Term.A(CommonAtomNames.Ok);
    internal static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
}
