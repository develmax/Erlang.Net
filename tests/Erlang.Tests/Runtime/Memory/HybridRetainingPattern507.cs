namespace Erlang.Tests;

// Represents ordinary host code supplied through the public Pattern extension.
internal sealed record HybridRetainingPattern507(Action<Term> Capture) : Pattern
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        Capture(value);

        return true;
    }
}
