namespace Erlang.Tests;

internal sealed record HybridAsyncPattern507(
    Func<ProcessContext, Term, Dictionary<string, Term>, Dictionary<string, Term>, ValueTask<bool>> Invoke
) : Pattern
{
    protected override bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
        => throw new InvalidOperationException(HybridBoundary507Names.Failure);

    protected override ValueTask<bool> MatchCoreAsync(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
        => Invoke(
            context!,
            value,
            bindings,
            keyScope!
        );
}
