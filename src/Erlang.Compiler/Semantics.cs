namespace Erlang.Compiler;

public static class Semantics
{
    internal static readonly HashSet<(string, int)> GuardBifs = [("is_atom", 1), ("is_integer", 1), ("is_float", 1), ("is_number", 1), ("is_tuple", 1), ("is_binary", 1), ("is_bitstring", 1), ("bit_size", 1), ("byte_size", 1), ("is_list", 1), ("is_pid", 1), ("is_map", 1), ("map_size", 1), ("map_get", 2), ("is_map_key", 2), ("length", 1), ("hd", 1), ("tl", 1), ("element", 2), ("tuple_size", 1), ("self", 0)];
    public static void Validate(ModuleDefinition module)
    {
        if (module.Functions.Select(f => (f.Name, f.Arity)).Distinct().Count() != module.Functions.Count) throw new CompileException("ERL005", "Duplicate function definition", 0);
        foreach (var export in module.Exports) if (!module.Functions.Any(f => f.Name == export.Name && f.Arity == export.Arity)) throw new CompileException("ERL005", $"Undefined export {export.Name}/{export.Arity}", 0);
        foreach (var f in module.Functions) foreach (var clause in f.Clauses) ValidateClause(clause, [], false);
    }
    public static void Validate(Expr expression) => Walk(expression, [], false);
    internal static IEnumerable<string> Variables(Pattern p) => p switch { MapPattern m => m.Fields.SelectMany(f => Variables(f.Value)), Pattern.Variable v when v.Name != "_" => [v.Name], Pattern.Tuple t => t.Items.SelectMany(Variables), Pattern.List l => l.Items.SelectMany(Variables).Concat(l.Tail is null ? [] : Variables(l.Tail)), _ => [] };
    private static void PatternKeys(Pattern pattern, HashSet<string> bound)
    {
        switch (pattern)
        {
            case MapPattern m:
                foreach (var field in m.Fields) { Walk(field.Key, new HashSet<string>(bound), true); PatternKeys(field.Value, bound); }
                break;
            case Pattern.Tuple t: foreach (var item in t.Items) PatternKeys(item, bound); break;
            case Pattern.List l: foreach (var item in l.Items) PatternKeys(item, bound); if (l.Tail is not null) PatternKeys(l.Tail, bound); break;
        }
    }
    private static HashSet<string> ValidateClause(Clause clause, HashSet<string> bound, bool shadow)
    {
        var scope = new HashSet<string>(bound);
        foreach (var p in clause.Patterns) PatternKeys(p, bound);
        foreach (var p in clause.Patterns) { foreach (string name in Variables(p)) { if (shadow) scope.Remove("!unsafe:" + name); else if (scope.Contains("!unsafe:" + name)) throw new CompileException("ERL006", $"Unsafe pattern variable '{name}'", 0); scope.Add(name); } }
        if (clause.Guard is not null) Walk(clause.Guard, scope, true);
        Walk(clause.Body, scope, false); return scope;
    }
    private static void Branches(IReadOnlyList<Clause> clauses, HashSet<string> bound, Expr? after = null)
    {
        var outcomes = clauses.Select(c => ValidateClause(c, bound, false)).ToList();
        if (after is not null) { var b = new HashSet<string>(bound); Walk(after, b, false); outcomes.Add(b); }
        if (outcomes.Count > 0) { var all = outcomes.SelectMany(b => b).ToHashSet(); var common = new HashSet<string>(outcomes[0]); foreach (var b in outcomes.Skip(1)) common.IntersectWith(b); bound.UnionWith(common); foreach (var name in all.Except(common)) bound.Add(name.StartsWith("!unsafe:", StringComparison.Ordinal) ? name : "!unsafe:" + name); }
    }
    private static void Walk(Expr e, HashSet<string> bound, bool guard)
    {
        switch (e)
        {
            case Expr.Literal: break;
            case Expr.Variable v: if (v.Name == "_" || !bound.Contains(v.Name)) throw new CompileException("ERL006", $"Unbound or unsafe variable '{v.Name}'", 0); break;
            case Expr.Tuple t: foreach (var x in t.Items) Walk(x, bound, guard); break;
            case Expr.List l: foreach (var x in l.Items) Walk(x, bound, guard); if (l.Tail is not null) Walk(l.Tail, bound, guard); break;
            case Expr.Map m:
                if (m.Base is not null) Walk(m.Base, bound, guard);
                else if (m.Fields.Any(f => f.Exact)) throw new CompileException("ERL004", "Map construction requires '=>' fields; ':=' is for updates or patterns", 0);
                foreach (var field in m.Fields) { Walk(field.Key, bound, guard); Walk(field.Value, bound, guard); }
                break;
            case Expr.Bits bits: foreach (var segment in bits.Segments) { Walk(segment.Value, bound, guard); if (segment.Size is not null) Walk(segment.Size, bound, guard); } break;
            case Expr.Sequence s: foreach (var x in s.Items) Walk(x, bound, guard); break;
            case Expr.GuardAlternatives s: foreach (var x in s.Items) Walk(x, bound, true); break;
            case Expr.Unary u: Walk(u.Operand, bound, guard); break;
            case Expr.Binary b:
                if (guard && b.Operator is "!" or "++" or "--") throw new CompileException("ERL007", "Operator is not legal in a guard", 0);
                Walk(b.Left, bound, guard); if (b.Operator is "andalso" or "orelse") { var maybe = new HashSet<string>(bound); Walk(b.Right, maybe, guard); } else Walk(b.Right, bound, guard); break;
            case Expr.Call call:
                if (guard && (call.Module is not null and not "erlang" || !GuardBifs.Contains((call.Function, call.Arguments.Count)))) throw new CompileException("ERL007", $"Illegal guard call '{call.Function}/{call.Arguments.Count}'", 0);
                foreach (var x in call.Arguments) Walk(x, bound, guard); break;
            case Expr.Match m:
                if (guard) throw new CompileException("ERL007", "Match is not legal in a guard", 0); Walk(m.Value, bound, false); PatternKeys(m.Pattern, bound); foreach (string name in Variables(m.Pattern)) { if (bound.Contains("!unsafe:" + name)) throw new CompileException("ERL006", $"Unsafe match variable '{name}'", 0); bound.Add(name); }
                break;
            case Expr.Case c: if (guard) throw new CompileException("ERL007", "case is not legal in a guard", 0); Walk(c.Value, bound, false); Branches(c.Clauses, bound); break;
            case Expr.Receive r: if (guard) throw new CompileException("ERL007", "receive is not legal in a guard", 0); if (r.Timeout is not null) Walk(r.Timeout, bound, false); Branches(r.Clauses, bound, r.After); break;
            case Expr.Fun f: if (guard) throw new CompileException("ERL007", "fun is not legal in a guard", 0); foreach (var c in f.Clauses) ValidateClause(c, bound, true); if (f.Clauses.Select(c => c.Patterns.Count).Distinct().Count() != 1) throw new CompileException("ERL005", "fun clauses must have equal arity", 0); break;
            case Expr.Apply a: if (guard) throw new CompileException("ERL007", "Dynamic calls are not legal in guards", 0); Walk(a.Function, bound, false); foreach (var x in a.Arguments) Walk(x, bound, false); break;
        }
    }
}
