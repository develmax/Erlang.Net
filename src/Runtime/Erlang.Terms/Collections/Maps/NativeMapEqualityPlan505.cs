namespace Erlang;

internal sealed class NativeMapEqualityPlan505(
    bool sameShape,
    IEnumerable<KeyValuePair<Term, Term>> comparisons
)
{
    internal bool SameShape { get; } = sameShape;
    internal IReadOnlyList<KeyValuePair<Term, Term>> Comparisons { get; } = Array.AsReadOnly(comparisons.ToArray());
}
