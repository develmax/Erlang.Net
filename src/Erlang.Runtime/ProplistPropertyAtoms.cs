namespace Erlang;

internal static class ProplistPropertyAtoms
{
    internal const string MissingValue = "undefined";

    internal static readonly Atom DefaultValue = Term.A(MissingValue);
    internal static readonly Atom ShorthandValue = Term.A(ErlangBooleanAtoms.True);
}
