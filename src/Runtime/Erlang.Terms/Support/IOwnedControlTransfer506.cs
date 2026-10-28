namespace Erlang;

// A control exception publishes its complete term payload in one root update.
// Non-term scope/code lease identities remain the original objects.
internal interface IOwnedControlTransfer506
{
    Exception TransferTerms506(Func<Term, Term> publish);
}
