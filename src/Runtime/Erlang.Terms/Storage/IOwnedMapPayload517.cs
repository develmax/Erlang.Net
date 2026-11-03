namespace Erlang;

// Views over real registered payload slots. Index arrays may describe order,
// but no view is allowed to retain a second array of key/value Term references.
internal interface IOwnedMapPayload517
{
    IOwnedTermSequence506 RootPayload { get; }
    IReadOnlyList<KeyValuePair<Term, Term>> Physical { get; }
    IReadOnlyList<KeyValuePair<Term, Term>> Canonical { get; }
    IReadOnlyList<KeyValuePair<Term, Term>> Arithmetic { get; }
}
