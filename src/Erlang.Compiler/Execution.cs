namespace Erlang.Compiler;

public static class Execution
{
    private sealed record Selection(Clause Clause, Dictionary<string, Term> Bindings);
    private sealed class TailCall(string function, IReadOnlyList<Term> arguments) : Exception
    { public string Function { get; } = function; public IReadOnlyList<Term> Arguments { get; } = arguments; }
    public static ValueTask<Term> EvaluateAsync(Expr expression, ProcessContext context) => Evaluate(expression, context, new Dictionary<string, Term>(StringComparer.Ordinal), null);
    public static async ValueTask<Term> InvokeAsync(ModuleDefinition module, string function, ProcessContext context, IReadOnlyList<Term> arguments)
    {
        while (true)
        {
            var definition = module.Functions.FirstOrDefault(f => f.Name == function && f.Arity == arguments.Count) ?? throw new ErlangException("undef");
            var selection = Select(definition.Clauses, arguments, [], context) ?? throw new ErlangException("function_clause");
            try { return await Evaluate(selection.Clause.Body, context, selection.Bindings, module, true); }
            catch (TailCall next) { function = next.Function; arguments = next.Arguments; }
        }
    }
    private static Selection? Select(IReadOnlyList<Clause> clauses, IReadOnlyList<Term> values, Dictionary<string, Term> parent, ProcessContext ctx, Dictionary<string, Term>? keyScope = null)
    {
        foreach (var clause in clauses)
        {
            var b = new Dictionary<string, Term>(parent, StringComparer.Ordinal); bool matched = clause.Patterns.Count == values.Count;
            for (int i = 0; matched && i < values.Count; i++) matched = clause.Patterns[i].Match(values[i], b, ctx, keyScope ?? parent);
            if (matched && Guard(clause.Guard, b, ctx)) return new(clause, b);
        }
        return null;
    }
    private static bool Guard(Expr? e, Dictionary<string, Term> b, ProcessContext ctx)
    { if (e is null) return true; if (e is Expr.GuardAlternatives alternatives) return alternatives.Items.Any(x => Guard(x, b, ctx)); try { return GuardValue(e, b, ctx).Equals(Term.A("true")); } catch (ErlangException) { return false; } }
    internal static Term PatternKey(Expr e, Dictionary<string, Term> b, ProcessContext? ctx) => GuardValue(e, b, ctx);
    private static Term GuardValue(Expr e, Dictionary<string, Term> b, ProcessContext? ctx) => e switch
    {
        Expr.Literal l => l.Value,
        Expr.Variable v => b.TryGetValue(v.Name, out var value) ? value : throw new ErlangException("unbound"),
        Expr.Tuple t => new TupleTerm(t.Items.Select(x => GuardValue(x, b, ctx))),
        Expr.List l => Cons.From(l.Items.Select(x => GuardValue(x, b, ctx)), l.Tail is null ? null : GuardValue(l.Tail, b, ctx)),
        Expr.Map m => EvaluateMap(m.Base is null ? new MapTerm([]) : GuardValue(m.Base, b, ctx), m.Fields.Select(f => (GuardValue(f.Key, b, ctx), GuardValue(f.Value, b, ctx), f.Exact)).ToArray()),
        Expr.Unary u => Unary(u.Operator, GuardValue(u.Operand, b, ctx)),
        Expr.Binary { Operator: "andalso" } x => CoreModules.Bool(GuardValue(x.Left, b, ctx)) ? GuardValue(x.Right, b, ctx) : Term.A("false"),
        Expr.Binary { Operator: "orelse" } x => CoreModules.Bool(GuardValue(x.Left, b, ctx)) ? Term.A("true") : GuardValue(x.Right, b, ctx),
        Expr.Binary x => Binary(x.Operator, GuardValue(x.Left, b, ctx), GuardValue(x.Right, b, ctx), ctx),
        Expr.Call x when ctx is not null && Semantics.GuardBifs.Contains((x.Function, x.Arguments.Count)) => ctx.Runtime.Modules.Call(ctx, "erlang", x.Function, x.Arguments.Select(a => GuardValue(a, b, ctx)).ToArray()).GetAwaiter().GetResult(),
        _ => throw new ErlangException("illegal_guard")
    };
    private static async ValueTask<Term> Evaluate(Expr e, ProcessContext ctx, Dictionary<string, Term> b, ModuleDefinition? module, bool tail = false)
    {
        await ctx.ReduceAsync();
        async ValueTask<Term[]> Arguments(IReadOnlyList<Expr> expressions)
        { var args = new Term[expressions.Count]; for (int i = 0; i < args.Length; i++) args[i] = await Evaluate(expressions[i], ctx, b, module); return args; }
        async ValueTask<Term> Branch(Selection selection)
        { var result = await Evaluate(selection.Clause.Body, ctx, selection.Bindings, module, tail); foreach (var v in selection.Bindings) b[v.Key] = v.Value; return result; }
        switch (e)
        {
            case Expr.Literal l: return l.Value;
            case Expr.Variable v: return b.TryGetValue(v.Name, out var boundValue) ? boundValue : throw new ErlangException("unbound");
            case Expr.Tuple t: return new TupleTerm(await Arguments(t.Items));
            case Expr.List l: { var items = await Arguments(l.Items); return Cons.From(items, l.Tail is null ? null : await Evaluate(l.Tail, ctx, b, module)); }
            case Expr.Map m:
                {
                    Term mapBase = m.Base is null ? new MapTerm([]) : await Evaluate(m.Base, ctx, b, module);
                    var fields = new (Term Key, Term Value, bool Exact)[m.Fields.Count];
                    for (int i = 0; i < fields.Length; i++) fields[i] = (await Evaluate(m.Fields[i].Key, ctx, b, module), await Evaluate(m.Fields[i].Value, ctx, b, module), m.Fields[i].Exact);
                    // Evaluate expressions before map type/key checks, as required by reference error cases.
                    return EvaluateMap(mapBase, fields);
                }
            case Expr.Sequence s: { Term result = Term.A("ok"); for (int i = 0; i < s.Items.Count; i++) result = await Evaluate(s.Items[i], ctx, b, module, tail && i == s.Items.Count - 1); return result; }
            case Expr.Match m: { var value = await Evaluate(m.Value, ctx, b, module); if (!m.Pattern.Match(value, b, ctx)) throw new ErlangException(Term.Tuple(Term.A("badmatch"), value)); return value; }
            case Expr.Unary u: return Unary(u.Operator, await Evaluate(u.Operand, ctx, b, module));
            case Expr.Binary x:
                { var left = await Evaluate(x.Left, ctx, b, module); if (x.Operator == "andalso" && !CoreModules.Bool(left)) return Term.A("false"); if (x.Operator == "orelse" && CoreModules.Bool(left)) return Term.A("true"); var right = await Evaluate(x.Right, ctx, b, module); return x.Operator is "andalso" or "orelse" ? right : Binary(x.Operator, left, right, ctx); }
            case Expr.Call x:
                { var args = await Arguments(x.Arguments); if (module is not null && (x.Module is null || x.Module == module.Name && module.Exports.Contains((x.Function, args.Length))) && module.Functions.Any(f => f.Name == x.Function && f.Arity == args.Length)) { if (tail) throw new TailCall(x.Function, args); return await InvokeAsync(module, x.Function, ctx, args); } return await ctx.Runtime.Modules.Call(ctx, x.Module ?? "erlang", x.Function, args); }
            case Expr.Apply x: { var fun = await Evaluate(x.Function, ctx, b, module); var args = await Arguments(x.Arguments); return fun is FunctionTerm f ? await f.Invoke(ctx, args) : throw new ErlangException(Term.Tuple(Term.A("badfun"), fun)); }
            case Expr.Case x: { var value = await Evaluate(x.Value, ctx, b, module); return await Branch(Select(x.Clauses, [value], b, ctx) ?? throw new ErlangException(Term.Tuple(Term.A("case_clause"), value))); }
            case Expr.Receive x:
                {
                    TimeSpan? timeout = null;
                    if (x.Timeout is not null) { var time = await Evaluate(x.Timeout, ctx, b, module); timeout = time switch { Atom { Name: "infinity" } => null, Integer i when i.Value >= 0 && i.Value <= uint.MaxValue => TimeSpan.FromMilliseconds((double)i.Value), _ => throw new ErlangException("timeout_value") }; }
                    var selection = await ctx.ReceiveAsync(message => Select(x.Clauses, [message], b, ctx), timeout);
                    if (selection is not null) return await Branch(selection);
                    return x.After is not null ? await Evaluate(x.After, ctx, b, module, tail) : throw new ErlangException("timeout");
                }
            case Expr.Fun f:
                {
                    var capture = new Dictionary<string, Term>(b, StringComparer.Ordinal);
                    var headNames = f.Clauses.SelectMany(c => c.Patterns.SelectMany(Semantics.Variables)).Distinct().ToArray();
                    return new FunctionTerm(f.Clauses[0].Patterns.Count, async (context, args) =>
                    { var process = (ProcessContext)context; var scope = new Dictionary<string, Term>(capture); foreach (var n in headNames) scope.Remove(n); var selection = Select(f.Clauses, args, scope, process, capture) ?? throw new ErlangException("function_clause"); return await Evaluate(selection.Clause.Body, process, selection.Bindings, module); });
                }
            default: throw new NotSupportedException();
        }
    }
    private static Term Unary(string op, Term value) => op switch
    { "+" when value is Integer or FloatTerm => value, "-" when value is FloatTerm f => new FloatTerm(-f.Value), "-" => CoreModules.Arithmetic("-", Term.I(0), value), "not" => CoreModules.Boolean(!CoreModules.Bool(value)), _ => throw new ErlangException("badarith") };
    private static MapTerm EvaluateMap(Term mapBase, IReadOnlyList<(Term Key, Term Value, bool Exact)> fields)
    {
        if (mapBase is not MapTerm map) throw new ErlangException(Term.Tuple(Term.A("badmap"), mapBase));
        var entries = map.Entries.ToDictionary(e => e.Key, e => e.Value);
        foreach (var field in fields)
        {
            if (field.Exact && !entries.ContainsKey(field.Key)) throw new ErlangException(Term.Tuple(Term.A("badkey"), field.Key));
            entries[field.Key] = field.Value;
        }
        return new MapTerm(entries);
    }
    private static Term Binary(string op, Term a, Term b, ProcessContext? ctx) => op switch
    {
        "==" => CoreModules.Boolean(a.NumericEquals(b)),
        "/=" => CoreModules.Boolean(!a.NumericEquals(b)),
        "=:=" => CoreModules.Boolean(a.Equals(b)),
        "=/=" => CoreModules.Boolean(!a.Equals(b)),
        "<" => CoreModules.Boolean(a.CompareTo(b) < 0),
        ">" => CoreModules.Boolean(a.CompareTo(b) > 0),
        "=<" => CoreModules.Boolean(a.CompareTo(b) <= 0),
        ">=" => CoreModules.Boolean(a.CompareTo(b) >= 0),
        "++" => Cons.From(Cons.Items(a), b),
        "--" => Subtract(a, b),
        "!" when ctx is not null => ctx.Runtime.Send(a is Pid p ? p : a is Atom name && ctx.Runtime.WhereIs(name.Name) is Pid registered ? registered : throw new ErlangException("badarg"), b),
        _ => CoreModules.Arithmetic(op, a, b)
    };
    private static Term Subtract(Term a, Term b) { var items = Cons.Items(a).ToList(); foreach (var x in Cons.Items(b)) { int i = items.FindIndex(v => v.Equals(x)); if (i >= 0) items.RemoveAt(i); } return Cons.From(items); }
}
