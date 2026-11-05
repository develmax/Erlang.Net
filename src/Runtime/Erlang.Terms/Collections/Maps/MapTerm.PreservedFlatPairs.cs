namespace Erlang;

public sealed partial class MapTerm
{
    private readonly IReadOnlyList<KeyValuePair<Term, Term>>? preservedFlatPairs426;

    // Owned maps read the registered slots on demand. Never keep a second
    // boxed child array alive beyond the heap owner's lifetime.
    internal IReadOnlyList<KeyValuePair<Term, Term>>? PreservedFlatPairs426 =>
        FlatKeys426 is null ? null : OwnedMapPayload517?.Physical ?? preservedFlatPairs426;
}
