namespace Erlang;

public abstract partial record Pattern
{
    protected abstract bool MatchCore(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    );

    protected virtual ValueTask<bool> MatchCoreAsync(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    ) => ValueTask.FromResult(MatchCore(
        value,
        bindings,
        context,
        keyScope
    ));

    public async ValueTask<bool> MatchAsync(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        var candidate = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        if (!await MatchBoundaryAsync507(
            value,
            candidate,
            context,
            new Dictionary<string, Term>(keyScope ?? bindings, StringComparer.Ordinal)
        ))
            return false;
        bindings.Clear();
        foreach (var binding in candidate)
            bindings.Add(binding.Key, binding.Value);

        return true;
    }

    public bool Match(
        Term value,
        Dictionary<string, Term> bindings,
        ProcessContext? context = null,
        Dictionary<string, Term>? keyScope = null
    )
    {
        var candidate = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        if (!MatchBoundary507(
            value,
            candidate,
            context,
            new Dictionary<string, Term>(keyScope ?? bindings, StringComparer.Ordinal)
        ))
            return false;
        bindings.Clear();
        foreach (var b in candidate)
            bindings.Add(b.Key, b.Value);

        return true;
    }

    public sealed record Any : Pattern
    {
        protected override bool MatchCore(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        ) => true;
    }
    public sealed record Variable(string Name) : Pattern
    {
        protected override bool MatchCore(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        )
        {
            if (Name == "_")
                return true;
            if (bindings.TryGetValue(Name, out var bound))
                return bound.Equals(value);
            bindings.Add(Name, value);

            return true;
        }
    }
    public sealed record Literal(Term Value) : Pattern
    {
        protected override bool MatchCore(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        ) => Value.Equals(value);
    }
    public sealed record Tuple(IReadOnlyList<Pattern> Items) : Pattern
    {
        protected override bool MatchCore(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        )
        {
            if (value is not TupleTerm t || t.Items.Count != Items.Count)
                return false;
            for (int i = 0; i < Items.Count; i++)
                if (!Items[i].MatchBoundary507(
                    t.Items[i],
                    bindings,
                    context,
                    keyScope
                ))
                    return false;

            return true;
        }

        protected override async ValueTask<bool> MatchCoreAsync(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        )
        {
            if (value is not TupleTerm t || t.Items.Count != Items.Count)
                return false;
            for (int i = 0; i < Items.Count; i++)
                if (!await Items[i].MatchBoundaryAsync507(
                    t.Items[i],
                    bindings,
                    context,
                    keyScope
                ))
                    return false;

            return true;
        }
    }
    public sealed record List(IReadOnlyList<Pattern> Items, Pattern? Tail = null) : Pattern
    {
        protected override bool MatchCore(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        )
        {
            foreach (var p in Items)
            {
                if (value is not Cons c || !p.MatchBoundary507(
                    c.Head,
                    bindings,
                    context,
                    keyScope
                ))
                    return false;
                value = c.Tail;
            }

            return Tail is null ? value is Nil : Tail.MatchBoundary507(
                value,
                bindings,
                context,
                keyScope
            );
        }

        protected override async ValueTask<bool> MatchCoreAsync(
            Term value,
            Dictionary<string, Term> bindings,
            ProcessContext? context = null,
            Dictionary<string, Term>? keyScope = null
        )
        {
            foreach (var p in Items)
            {
                if (value is not Cons c || !await p.MatchBoundaryAsync507(
                    c.Head,
                    bindings,
                    context,
                    keyScope
                ))
                    return false;
                value = c.Tail;
            }

            return Tail is null ? value is Nil : await Tail.MatchBoundaryAsync507(
                value,
                bindings,
                context,
                keyScope
            );
        }
    }
}
