namespace Erlang.Compiler;

public static class Semantics
{
    internal static readonly HashSet<(string, int)> GuardBifs = [(ErlangModuleNames.IsAtom, 1), (ErlangModuleNames.IsInteger, 1), (ErlangModuleNames.IsFloat, 1), (ErlangModuleNames.IsNumber, 1), (ErlangModuleNames.IsTuple, 1), (ErlangModuleNames.IsBinary, 1), (ErlangModuleNames.IsBitString, 1), (ErlangModuleNames.BitSize, 1), (ErlangModuleNames.ByteSize, 1), (ErlangModuleNames.IsList, 1), (ErlangModuleNames.IsPid, 1), (ErlangModuleNames.IsMap, 1), (ErlangModuleNames.MapSize, 1), (ErlangModuleNames.MapGet, 2), (ErlangModuleNames.IsMapKey, 2), (ErlangModuleNames.Length, 1), (ErlangModuleNames.Head, 1), (ErlangModuleNames.Tail, 1), (ErlangModuleNames.Element, 2), (ErlangModuleNames.TupleSize, 1), (ErlangModuleNames.Self, 0)];

    public static void Validate(ModuleDefinition module)
    {
        if (module.Functions.Select(f => (f.Name, f.Arity)).Distinct().Count() != module.Functions.Count)
            throw new CompileException(CompilerDiagnosticCodes.FunctionDefinition, SemanticDiagnostics.DuplicateFunction, 0);
        foreach (var export in module.Exports)
            if (!module.Functions.Any(f => f.Name == export.Name && f.Arity == export.Arity))
                throw new CompileException(CompilerDiagnosticCodes.FunctionDefinition, SemanticDiagnostics.UndefinedExport(export.Name, export.Arity), 0);
        foreach (var f in module.Functions)
            foreach (var clause in f.Clauses)
                ValidateClause(clause, [], false);
    }

    public static void Validate(Expr expression) => Walk(expression, [], false);

    internal static IEnumerable<string> Variables(Pattern p) => p switch
    {
        BitPattern bits => bits.Segments.SelectMany(s => Variables(s.Value)),
        MapPattern m => m.Fields.SelectMany(f => Variables(f.Value)),
        Pattern.Variable v when v.Name != VariableScopeNames.Wildcard => [v.Name],
        Pattern.Tuple t => t.Items.SelectMany(Variables),
        Pattern.List l => l.Items.SelectMany(Variables).Concat(l.Tail is null ? [] : Variables(l.Tail)),
        _ => []
    };

    private static void PatternKeys(Pattern pattern, HashSet<string> bound)
    {
        switch (pattern)
        {
            case BitPattern bits:
                var sizes = new HashSet<string>(bound);
                foreach (var segment in bits.Segments)
                {
                    if (segment.Specification.Size is not null)
                        Walk(segment.Specification.Size, new HashSet<string>(sizes), true);
                    sizes.UnionWith(Variables(segment.Value));
                }
                break;
            case MapPattern m:
                foreach (var field in m.Fields)
                {
                    Walk(field.Key, new HashSet<string>(bound), true);
                    PatternKeys(field.Value, bound);
                }
                break;
            case Pattern.Tuple t:
                foreach (var item in t.Items)
                    PatternKeys(item, bound);
                break;
            case Pattern.List l:
                foreach (var item in l.Items)
                    PatternKeys(item, bound);
                if (l.Tail is not null)
                    PatternKeys(l.Tail, bound);
                break;
        }
    }

    private static HashSet<string> ValidateClause(Clause clause, HashSet<string> bound, bool shadow)
    {
        var scope = new HashSet<string>(bound);
        foreach (var p in clause.Patterns)
            PatternKeys(p, bound);
        foreach (var p in clause.Patterns)
        {
            foreach (string name in Variables(p))
            {
                if (shadow)
                    scope.Remove(VariableScopeNames.UnsafePrefix + name);
                else if (scope.Contains(VariableScopeNames.UnsafePrefix + name))
                    throw new CompileException(CompilerDiagnosticCodes.VariableBinding, SemanticDiagnostics.UnsafePatternVariable(name), 0);
                scope.Add(name);
            }
        }
        if (clause.Guard is not null)
            Walk(clause.Guard, scope, true);
        Walk(clause.Body, scope, false);

        return scope;
    }

    private static void Branches(IReadOnlyList<Clause> clauses, HashSet<string> bound, Expr? after = null)
    {
        var outcomes = clauses.Select(c => ValidateClause(c, bound, false)).ToList();
        if (after is not null)
        {
            var b = new HashSet<string>(bound);
            Walk(after, b, false);
            outcomes.Add(b);
        }
        if (outcomes.Count > 0)
        {
            var all = outcomes.SelectMany(b => b).ToHashSet();
            var common = new HashSet<string>(outcomes[0]);
            foreach (var b in outcomes.Skip(1))
                common.IntersectWith(b);
            bound.UnionWith(common);
            foreach (var name in all.Except(common))
                bound.Add(name.StartsWith(VariableScopeNames.UnsafePrefix, StringComparison.Ordinal) ? name : VariableScopeNames.UnsafePrefix + name);
        }
    }

    private static void Walk(Expr e, HashSet<string> bound, bool guard)
    {
        switch (e)
        {
            case Expr.Literal:
                break;
            case Expr.Variable v:
                if (v.Name == VariableScopeNames.Wildcard || !bound.Contains(v.Name))
                    throw new CompileException(CompilerDiagnosticCodes.VariableBinding, SemanticDiagnostics.UnboundOrUnsafeVariable(v.Name), 0);
                break;
            case Expr.Tuple t:
                foreach (var x in t.Items)
                    Walk(x, bound, guard);
                break;
            case Expr.List l:
                foreach (var x in l.Items)
                    Walk(x, bound, guard);
                if (l.Tail is not null)
                    Walk(l.Tail, bound, guard);
                break;
            case Expr.Map m:
                if (m.Base is not null)
                    Walk(m.Base, bound, guard);
                else if (m.Fields.Any(f => f.Exact))
                    throw new CompileException(CompilerDiagnosticCodes.InvalidPattern, SemanticDiagnostics.MapConstructionOperator, 0);
                foreach (var field in m.Fields)
                {
                    Walk(field.Key, bound, guard);
                    Walk(field.Value, bound, guard);
                }
                break;
            case Expr.Bits bits:
                var exported = new HashSet<string>(bound);
                foreach (var segment in bits.Segments)
                {
                    var valueScope = new HashSet<string>(bound);
                    Walk(segment.Value, valueScope, guard);
                    exported.UnionWith(valueScope);
                    if (segment.Size is not null)
                    {
                        var sizeScope = new HashSet<string>(bound);
                        Walk(segment.Size, sizeScope, guard);
                        exported.UnionWith(sizeScope);
                    }
                }
                bound.UnionWith(exported);
                break;
            case Expr.Sequence s:
                foreach (var x in s.Items)
                    Walk(x, bound, guard);
                break;
            case Expr.GuardAlternatives s:
                foreach (var x in s.Items)
                    Walk(x, bound, true);
                break;
            case Expr.Unary u:
                Walk(u.Operand, bound, guard);
                break;
            case Expr.Block block:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardBlock, 0);
                Walk(block.Body, bound, false);
                break;
            case Expr.Catch caught:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardCatch, 0);
                var catchScope = new HashSet<string>(bound);
                Walk(caught.Operand, catchScope, false);
                foreach (string name in catchScope.Except(bound))
                    bound.Add(name.StartsWith(VariableScopeNames.UnsafePrefix, StringComparison.Ordinal) ? name : VariableScopeNames.UnsafePrefix + name);
                break;
            case Expr.Binary b:
                if (guard && b.Operator is ErlangOperators.Send or ErlangOperators.Append or ErlangOperators.SubtractList)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardOperator, 0);
                if (b.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse)
                {
                    Walk(b.Left, bound, guard);
                    var maybe = new HashSet<string>(bound);
                    Walk(b.Right, maybe, guard);
                    foreach (string name in maybe.Except(bound))
                        bound.Add(name.StartsWith(VariableScopeNames.UnsafePrefix, StringComparison.Ordinal) ? name : VariableScopeNames.UnsafePrefix + name);
                }
                else
                {
                    var leftScope = new HashSet<string>(bound);
                    var rightScope = new HashSet<string>(bound);
                    Walk(b.Left, leftScope, guard);
                    Walk(b.Right, rightScope, guard);
                    bound.UnionWith(leftScope);
                    bound.UnionWith(rightScope);
                }
                break;
            case Expr.Call call:
                if (guard && (call.Module is not null and not ErlangModuleNames.Module || !GuardBifs.Contains((call.Function, call.Arguments.Count))))
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.IllegalGuardCall(call.Function, call.Arguments.Count), 0);
                foreach (var x in call.Arguments)
                    Walk(x, bound, guard);
                break;
            case Expr.Match m:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardMatch, 0);
                Walk(m.Value, bound, false);
                PatternKeys(m.Pattern, bound);
                foreach (string name in Variables(m.Pattern))
                {
                    if (bound.Contains(VariableScopeNames.UnsafePrefix + name))
                        throw new CompileException(CompilerDiagnosticCodes.VariableBinding, SemanticDiagnostics.UnsafeMatchVariable(name), 0);
                    bound.Add(name);
                }
                break;
            case Expr.Case c:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardCase, 0);
                Walk(c.Value, bound, false);
                Branches(c.Clauses, bound);
                break;
            case Expr.If i:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardIf, 0);
                Branches(i.Clauses, bound);
                break;
            case Expr.Receive r:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardReceive, 0);
                if (r.Timeout is not null)
                    Walk(r.Timeout, bound, false);
                Branches(r.Clauses, bound, r.After);
                break;
            case Expr.Fun f:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardFun, 0);
                foreach (var c in f.Clauses)
                    ValidateClause(c, bound, true);
                if (f.Clauses.Select(c => c.Patterns.Count).Distinct().Count() != 1)
                    throw new CompileException(CompilerDiagnosticCodes.FunctionDefinition, SemanticDiagnostics.FunClauseArityMismatch, 0);
                break;
            case Expr.Apply a:
                if (guard)
                    throw new CompileException(CompilerDiagnosticCodes.IllegalGuard, SemanticDiagnostics.GuardDynamicCall, 0);
                Walk(a.Function, bound, false);
                foreach (var x in a.Arguments)
                    Walk(x, bound, false);
                break;
        }
    }
}
