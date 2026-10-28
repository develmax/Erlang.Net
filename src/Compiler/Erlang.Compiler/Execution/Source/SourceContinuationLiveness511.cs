namespace Erlang.Compiler;

internal static class SourceContinuationLiveness511
{
    internal static Term[] CompleteStatement(
        Expr.Sequence sequence,
        int completed,
        Dictionary<string, Term> bindings
    )
    {
        // Constraints are a separate semantic environment. Keep it intact until
        // its owner has an explicit lexical export/retirement protocol.
        if (bindings is CompiledBindingScope constrained)
            return bindings.Values.Concat(constrained.Constraints.Values).ToArray();

        var continuation = new Expr.Sequence(sequence.Items.Skip(completed + 1).ToArray());
        var function = new Expr.Fun([new Clause([], null, continuation)]);
        HashSet<string> names;
        try
        {
            // This existing source-aware walker handles closures, pattern keys,
            // guards, receive/try/after/maybe and comprehension shadowing.
            names = ClosureCaptures.Capture(function, bindings).Keys.ToHashSet(StringComparer.Ordinal);
        }
        catch (NotSupportedException)
        {
            // A newly added AST form must not turn into premature retirement.
            return bindings.Values.ToArray();
        }
        foreach (string name in bindings.Keys.Where(name => !names.Contains(name)).ToArray())
            bindings.Remove(name);

        return bindings.Values.ToArray();
    }
}
