namespace Erlang;

public abstract record Pattern
{
    protected abstract bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null);
    public bool Match(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
    {
        var candidate = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        if (!MatchCore(value, candidate, context, new Dictionary<string, Term>(keyScope ?? bindings, StringComparer.Ordinal)))
            return false;
        bindings.Clear();
        foreach (var b in candidate)
            bindings.Add(b.Key, b.Value);
        return true;
    }
    public sealed record Any : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null) => true;
    }
    public sealed record Variable(string Name) : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
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
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null) => Value.Equals(value);
    }
    public sealed record Tuple(IReadOnlyList<Pattern> Items) : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
        {
            if (value is not TupleTerm t || t.Items.Count != Items.Count)
                return false;
            for (int i = 0; i < Items.Count; i++)
                if (!Items[i].MatchCore(t.Items[i], bindings, context, keyScope))
                    return false;
            return true;
        }
    }
    public sealed record List(IReadOnlyList<Pattern> Items, Pattern? Tail = null) : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
        {
            foreach (var p in Items)
            {
                if (value is not Cons c || !p.MatchCore(c.Head, bindings, context, keyScope))
                    return false;
                value = c.Tail;
            }
            return Tail is null ? value is Nil : Tail.MatchCore(value, bindings, context, keyScope);
        }
    }
}
