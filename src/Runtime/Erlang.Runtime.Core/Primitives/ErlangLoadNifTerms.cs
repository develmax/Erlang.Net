namespace Erlang;

internal static class ErlangLoadNifTerms
{
    internal static readonly Term IndirectCallError = Term.Tuple(
        Term.A(CommonAtomNames.Error),
        Term.Tuple(Term.A(ErlangLoadNifNames.BadLibrary), Term.String(ErlangLoadNifNames.IndirectCallMessage))
    );
}
