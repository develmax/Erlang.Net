namespace Erlang.Tests;

internal sealed record HybridKeyScopePattern507(
    Action<Term, Dictionary<string, Term>, Dictionary<string, Term>, ProcessContext?> Operation,
    bool Async
) : Pattern
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        Operation(
            value,
            bindings,
            keyScope!,
            context
        );

        return true;
    }

    protected override async ValueTask<bool> MatchCoreAsync(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        if (Async)
            await Task.Yield();

        return MatchCore(
            value,
            bindings,
            context,
            keyScope
        );
    }
}
