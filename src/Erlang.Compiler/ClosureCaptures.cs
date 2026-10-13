namespace Erlang.Compiler;

internal static class ClosureCaptures
{
    public static Dictionary<string, Term> Capture(Expr.Fun function, IReadOnlyDictionary<string, Term> bindings)
    {
        var collector = new Collector(bindings.Keys);
        foreach (var clause in function.Clauses)
            collector.Clause(clause, [], true);

        return collector.Names.Order(StringComparer.Ordinal).ToDictionary(name => name, name => bindings[name], StringComparer.Ordinal);
    }

    private sealed class Collector(IEnumerable<string> outer)
    {
        private readonly HashSet<string> outerNames = new(outer, StringComparer.Ordinal);
        public HashSet<string> Names { get; } = new(StringComparer.Ordinal);

        private void Read(string name, HashSet<string> local)
        {
            if (!local.Contains(name) && outerNames.Contains(name))
                Names.Add(name);
        }

        private void Bind(Pattern pattern, HashSet<string> local, bool shadow)
        {
            foreach (string name in Semantics.Variables(pattern))
            {
                if (!shadow && !local.Contains(name) && outerNames.Contains(name))
                    Names.Add(name);
                else
                    local.Add(name);
            }
        }

        private void Keys(Pattern pattern, HashSet<string> local, bool shadow)
        {
            switch (pattern)
            {
                case AliasPattern alias:
                    Keys(alias.Left, local, shadow);
                    Keys(alias.Right, local, shadow);
                    break;
                case BitPattern bits:
                    var sizes = new HashSet<string>(local, StringComparer.Ordinal);
                    foreach (var segment in bits.Segments)
                    {
                        if (segment.Specification.Size is { } size)
                            Walk(size, new HashSet<string>(sizes, StringComparer.Ordinal));
                        Keys(segment.Value, sizes, shadow);
                        Bind(segment.Value, sizes, shadow);
                    }
                    break;
                case MapPattern map:
                    foreach (var field in map.Fields)
                    {
                        Walk(field.Key, new HashSet<string>(local, StringComparer.Ordinal));
                        Keys(field.Value, local, shadow);
                    }
                    break;
                case NativeRecordPattern record:
                    foreach (var field in record.Fields)
                        Keys(field.Value, local, shadow);
                    break;
                case Pattern.Tuple tuple:
                    foreach (var item in tuple.Items)
                        Keys(item, local, shadow);
                    break;
                case Pattern.List list:
                    foreach (var item in list.Items)
                        Keys(item, local, shadow);
                    if (list.Tail is { } tail)
                        Keys(tail, local, shadow);
                    break;
            }
        }

        public HashSet<string> Clause(Clause clause, HashSet<string> parent, bool shadow)
        {
            var local = new HashSet<string>(parent, StringComparer.Ordinal);
            foreach (var pattern in clause.Patterns)
                Keys(pattern, parent, shadow);
            foreach (var pattern in clause.Patterns)
                Bind(pattern, local, shadow);
            if (clause.Guard is { } guard)
                Walk(guard, local);
            Walk(clause.Body, local);

            return local;
        }

        private void Parallel(IEnumerable<Expr> items, HashSet<string> local)
        {
            var joined = new HashSet<string>(local, StringComparer.Ordinal);
            foreach (var item in items)
            {
                var child = new HashSet<string>(local, StringComparer.Ordinal);
                Walk(item, child);
                joined.UnionWith(child);
            }
            local.UnionWith(joined);
        }

        private void Branches(IReadOnlyList<Clause> clauses, HashSet<string> local, Expr? after = null)
        {
            HashSet<string>? common = null;
            foreach (var clause in clauses)
            {
                var child = Clause(clause, local, false);
                if (common is null)
                    common = child;
                else
                    common.IntersectWith(child);
            }
            if (after is not null)
            {
                var child = new HashSet<string>(local, StringComparer.Ordinal);
                Walk(after, child);
                if (common is null)
                    common = child;
                else
                    common.IntersectWith(child);
            }
            if (common is not null)
                local.UnionWith(common);
        }

        private void Comprehension(IEnumerable<Expr> items, IReadOnlyList<ComprehensionQualifier> qualifiers, HashSet<string> parent)
        {
            var local = new HashSet<string>(parent, StringComparer.Ordinal);
            foreach (var qualifier in qualifiers)
            {
                if (qualifier is ComprehensionQualifier.Filter filter)
                {
                    Walk(filter.Expression, local);
                    continue;
                }
                var generators = qualifier is ComprehensionQualifier.Zip zip ? ComprehensionGenerator.Flatten(zip.Generators).ToArray() : ComprehensionGenerator.Flatten([qualifier]).ToArray();
                var sources = new HashSet<string>(local, StringComparer.Ordinal);
                foreach (var generator in generators)
                    Walk(generator.Source, sources);
                foreach (var generator in generators)
                    Keys(generator.Pattern, local, true);
                foreach (var generator in generators)
                    Bind(generator.Pattern, local, true);
            }
            Parallel(items, local);
        }

        private static bool BooleanLiteral(Expr expression, string atom) => expression is Expr.Literal { Value: Atom value } && value.Name == atom;

        private void Walk(Expr expression, HashSet<string> local)
        {
            switch (expression)
            {
                case Expr.Literal:
                    break;
                case Expr.Variable variable:
                    Read(variable.Name, local);
                    break;
                case NativeRecordExpression record:
                    Parallel(record.Children, local);
                    break;
                case Expr.RecordConstruction record:
                    Walk(RecordExpressions.Create(record), local);
                    break;
                case Expr.RecordAccess record:
                    Walk(record.Value, local);
                    break;
                case Expr.RecordTest record:
                    Walk(record.Value, local);
                    break;
                case Expr.RecordUpdate record:
                    Parallel(new[] { record.Value }.Concat(record.Fields.Select(field => field.Value)), local);
                    break;
                case Expr.Tuple tuple:
                    Parallel(tuple.Items, local);
                    break;
                case Expr.List list:
                    Parallel(list.Tail is null ? list.Items : list.Items.Append(list.Tail), local);
                    break;
                case Expr.Map map:
                    foreach (var item in MapExpressionBindings.Expressions(map))
                        Walk(item, local);
                    break;
                case Expr.Bits bits:
                    Parallel(bits.Segments.SelectMany(segment => segment.Size is null ? new[] { segment.Value } : new[] { segment.Value, segment.Size }), local);
                    break;
                case Expr.ListComprehension list:
                    Comprehension(list.Items, list.Qualifiers, local);
                    break;
                case Expr.BinaryComprehension binary:
                    Comprehension([binary.Body], binary.Qualifiers, local);
                    break;
                case Expr.MapComprehension map:
                    Comprehension(map.Fields.SelectMany(field => new[] { field.Key, field.Value }), map.Qualifiers, local);
                    break;
                case Expr.Sequence sequence:
                    foreach (var item in sequence.Items)
                        Walk(item, local);
                    break;
                case Expr.GuardAlternatives guards:
                    Parallel(guards.Items, local);
                    break;
                case Expr.Block block:
                    Walk(block.Body, local);
                    break;
                case Expr.Unary unary:
                    Walk(unary.Operand, local);
                    break;
                case Expr.Binary binary when binary.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse:
                    Walk(binary.Left, local);
                    if (binary.Operator == ErlangOperators.AndAlso && BooleanLiteral(binary.Left, ErlangBooleanAtoms.False) || binary.Operator == ErlangOperators.OrElse && BooleanLiteral(binary.Left, ErlangBooleanAtoms.True))
                        break;
                    Walk(binary.Right, new HashSet<string>(local, StringComparer.Ordinal));
                    break;
                case Expr.Binary binary:
                    Parallel([binary.Left, binary.Right], local);
                    break;
                case Expr.Call call:
                    Parallel(call.Arguments, local);
                    break;
                case Expr.Apply apply:
                    Parallel(apply.Arguments.Prepend(apply.Function), local);
                    break;
                case Expr.Match match:
                    Walk(match.Value, local);
                    Keys(match.Pattern, local, false);
                    Bind(match.Pattern, local, false);
                    break;
                case Expr.MaybeMatch match:
                    Walk(new Expr.Match(match.Pattern, match.Value), local);
                    break;
                case Expr.Case conditional:
                    Walk(conditional.Value, local);
                    Branches(conditional.Clauses, local);
                    break;
                case Expr.If conditional:
                    Branches(conditional.Clauses, local);
                    break;
                case Expr.Receive receive:
                    if (receive.Timeout is { } timeout)
                        Walk(timeout, local);
                    Branches(receive.Clauses, local, receive.After);
                    break;
                case Expr.Catch caught:
                    Walk(caught.Operand, new HashSet<string>(local, StringComparer.Ordinal));
                    break;
                case Expr.Try tried:
                    var success = new HashSet<string>(local, StringComparer.Ordinal);
                    Walk(tried.Body, success);
                    Branches(tried.Clauses, success);
                    Branches(tried.Catches, new HashSet<string>(local, StringComparer.Ordinal));
                    if (tried.After is { } cleanup)
                        Walk(cleanup, new HashSet<string>(local, StringComparer.Ordinal));
                    break;
                case Expr.Maybe maybe:
                    var conditionalLocal = new HashSet<string>(local, StringComparer.Ordinal);
                    foreach (var item in maybe.Items)
                        Walk(item, conditionalLocal);
                    Branches(maybe.Clauses, new HashSet<string>(local, StringComparer.Ordinal));
                    break;
                case Expr.FunctionReference:
                    break;
                case Expr.Fun nested:
                    foreach (var clause in nested.Clauses)
                        Clause(clause, local, true);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
