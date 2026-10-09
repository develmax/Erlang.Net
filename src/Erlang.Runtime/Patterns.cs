namespace Erlang;

public abstract record Pattern
{
    protected abstract bool MatchCore(Term value, Dictionary<string, Term> bindings);
    public bool Match(Term value, Dictionary<string, Term> bindings)
    {
        var candidate = new Dictionary<string, Term>(bindings, StringComparer.Ordinal);
        if (!MatchCore(value, candidate)) return false;
        bindings.Clear(); foreach (var b in candidate) bindings.Add(b.Key, b.Value); return true;
    }
    public sealed record Any : Pattern { protected override bool MatchCore(Term value, Dictionary<string, Term> bindings) => true; }
    public sealed record Variable(string Name) : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings)
        { if (Name == "_") return true; if (bindings.TryGetValue(Name, out var bound)) return bound.Equals(value); bindings.Add(Name, value); return true; }
    }
    public sealed record Literal(Term Value) : Pattern { protected override bool MatchCore(Term value, Dictionary<string, Term> bindings) => Value.Equals(value); }
    public sealed record Tuple(IReadOnlyList<Pattern> Items) : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings)
        { if (value is not TupleTerm t || t.Items.Count != Items.Count) return false; for (int i = 0; i < Items.Count; i++) if (!Items[i].MatchCore(t.Items[i], bindings)) return false; return true; }
    }
    public sealed record List(IReadOnlyList<Pattern> Items, Pattern? Tail = null) : Pattern
    {
        protected override bool MatchCore(Term value, Dictionary<string, Term> bindings)
        { foreach (var p in Items) { if (value is not Cons c || !p.MatchCore(c.Head, bindings)) return false; value = c.Tail; } return Tail is null ? value is Nil : Tail.MatchCore(value, bindings); }
    }
}
