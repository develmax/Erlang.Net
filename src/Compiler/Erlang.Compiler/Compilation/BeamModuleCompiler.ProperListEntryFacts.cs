namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition>? properListSourceFunctions;
    private ProperListEntryFacts? properListEntryFacts;

    private bool HasProperListEntryFact((string Name, int Arity) signature, int argument)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
        {
            return false;
        }

        properListEntryFacts ??= new ProperListEntryFacts(this, properListSourceFunctions, exportedFunctions);

        return properListEntryFacts.Proves(signature, argument);
    }

    private sealed class ProperListEntryFacts
    {
        private enum Fact
        {
            Unreachable,
            ProperList,
            Unknown
        }

        private readonly BeamModuleCompiler owner;
        private readonly IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions;
        private readonly HashSet<(string Name, int Arity)> exports;
        private readonly Dictionary<(string Name, int Arity), Fact[]> incoming = [];
        private readonly Dictionary<(string Name, int Arity), Fact> results = [];
        private readonly Dictionary<(string Name, int Arity), HashSet<(string Name, int Arity)>> dependents = [];
        private readonly HashSet<(string Name, int Arity)> queued = [];
        private readonly Queue<(string Name, int Arity)> work = [];
        private (string Name, int Arity) current;

        internal ProperListEntryFacts(
            BeamModuleCompiler owner,
            IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> functions,
            IReadOnlySet<(string Name, int Arity)> exports
        )
        {
            this.owner = owner;
            this.functions = functions;
            this.exports = exports.ToHashSet();
            var escaped = functions.Values.SelectMany(function => LocalFunctionEntries.ClauseExpressions(function.Clauses))
                .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.FunctionReference>()
                .Select(reference => (reference.Name, reference.Arity));
            foreach (var entry in exports.Concat(escaped))
            {
                AddCall(entry, UnknownArguments(entry.Arity));
            }

            while (work.TryDequeue(out current))
            {
                queued.Remove(current);
                // Recursive edges may update the argument vector while this function is being analyzed.
                var arguments = incoming[current].ToArray();
                Fact result = ReadClauses(functions[current].Clauses, arguments, new(StringComparer.Ordinal));
                Fact previous = results.GetValueOrDefault(current);
                if ((int)result <= (int)previous)
                {
                    continue;
                }

                results[current] = result;
                if (dependents.TryGetValue(current, out var callers))
                {
                    foreach (var caller in callers)
                    {
                        Enqueue(caller);
                    }
                }
            }
        }

        internal bool Proves((string Name, int Arity) signature, int argument) =>
            !exports.Contains(signature) && incoming.TryGetValue(signature, out var arguments)
            && argument >= 0 && argument < arguments.Length && arguments[argument] == Fact.ProperList;

        private void AddCall((string Name, int Arity) target, Fact[] arguments)
        {
            if (!functions.ContainsKey(target) || arguments.Contains(Fact.Unreachable))
            {
                return;
            }

            if (!incoming.TryGetValue(target, out var previous))
            {
                incoming.Add(target, arguments);
                Enqueue(target);

                return;
            }

            bool changed = false;
            for (int index = 0; index < arguments.Length; index++)
            {
                Fact joined = Join(previous[index], arguments[index]);
                changed |= joined != previous[index];
                previous[index] = joined;
            }

            if (changed)
            {
                Enqueue(target);
            }
        }

        private void Enqueue((string Name, int Arity) signature)
        {
            if (queued.Add(signature))
            {
                work.Enqueue(signature);
            }
        }

        private Fact ReadClauses(
            IReadOnlyList<Clause> clauses,
            IReadOnlyList<Fact> arguments,
            Dictionary<string, Fact> outer
        )
        {
            if (arguments.Contains(Fact.Unreachable))
            {
                return Fact.Unreachable;
            }

            Fact result = Fact.Unreachable;
            foreach (var clause in clauses)
            {
                var scope = new Dictionary<string, Fact>(outer, StringComparer.Ordinal);
                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    Fact argument = index < arguments.Count ? arguments[index] : Fact.Unknown;
                    Bind(clause.Patterns[index], argument, scope);
                }

                int trailingExpressions = clause.Guard is null ? 1 : 2;
                foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]).SkipLast(trailingExpressions))
                {
                    ObserveUnknownCalls(expression);
                }

                if (clause.Guard is { } guard)
                {
                    Read(guard, new(scope, StringComparer.Ordinal));
                }

                result = Join(result, Read(clause.Body, scope));
            }

            return result;
        }

        private Fact Read(Expr expression, Dictionary<string, Fact> scope)
        {
            switch (expression)
            {
                case Expr.Literal literal:
                    Term tail = literal.Value;
                    while (tail is Cons cons)
                    {
                        tail = cons.Tail;
                    }

                    return tail is Nil ? Fact.ProperList : Fact.Unknown;
                case Expr.Variable variable:
                    return scope.GetValueOrDefault(variable.Name, Fact.Unknown);
                case Expr.Match match:
                    Fact matched = Read(match.Value, scope);
                    foreach (var patternExpression in LocalFunctionEntries.Children(match).SkipLast(1))
                    {
                        ObserveUnknownCalls(patternExpression);
                    }

                    if (matched != Fact.Unreachable)
                    {
                        Bind(match.Pattern, matched, scope);
                    }

                    return matched;
                case Expr.Sequence sequence:
                    Fact result = Fact.Unknown;
                    foreach (var item in sequence.Items)
                    {
                        result = Read(item, scope);
                        if (result == Fact.Unreachable)
                        {
                            break;
                        }
                    }

                    return result;
                case Expr.Block block:
                    return Read(block.Body, scope);
                case Expr.List list:
                    if (ReadOperands(list.Items, scope).Contains(Fact.Unreachable))
                    {
                        return Fact.Unreachable;
                    }

                    return list.Tail is null ? Fact.ProperList : Read(list.Tail, scope);
                case Expr.Tuple tuple:
                    return ReadOperands(tuple.Items, scope).Contains(Fact.Unreachable) ? Fact.Unreachable : Fact.Unknown;
                case Expr.Call call:
                    return ReadCall(call, scope);
                case Expr.Unary unary:
                    return Read(unary.Operand, scope) == Fact.Unreachable ? Fact.Unreachable : Fact.Unknown;
                case Expr.Binary binary:
                    if (Read(binary.Left, scope) == Fact.Unreachable)
                    {
                        return Fact.Unreachable;
                    }

                    if (binary.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse)
                    {
                        Read(binary.Right, new(scope, StringComparer.Ordinal));

                        return Fact.Unknown;
                    }

                    return Read(binary.Right, scope) == Fact.Unreachable ? Fact.Unreachable : Fact.Unknown;
                case Expr.Case decision:
                    return ReadClauses(decision.Clauses, [Read(decision.Value, scope)], scope);
                case Expr.If decision:
                    return ReadClauses(decision.Clauses, [], scope);
                case Expr.Try attempted:
                    var bodyScope = new Dictionary<string, Fact>(scope, StringComparer.Ordinal);
                    Fact body = Read(attempted.Body, bodyScope);
                    Fact normal = attempted.Clauses.Count == 0 ? body : ReadClauses(attempted.Clauses, [body], bodyScope);
                    Fact exceptions = ReadClauses(attempted.Catches, [], scope);
                    if (attempted.After is { } cleanup)
                    {
                        Read(cleanup, new(scope, StringComparer.Ordinal));
                    }

                    return Join(normal, exceptions);
                case Expr.Catch caught:
                    Read(caught.Operand, new(scope, StringComparer.Ordinal));

                    return Fact.Unknown;
                case Expr.Fun closure:
                    ReadClauses(closure.Clauses, [], new(StringComparer.Ordinal));

                    return Fact.Unknown;
                case Expr.Receive receive:
                    Fact received = ReadClauses(receive.Clauses, [], scope);
                    if (receive.Timeout is { } timeout && receive.After is { } after)
                    {
                        Read(timeout, new(scope, StringComparer.Ordinal));
                        received = Join(received, Read(after, new(scope, StringComparer.Ordinal)));
                    }

                    return received;
                default:
                    ObserveUnknownCalls(expression);

                    return Fact.Unknown;
            }
        }

        private Fact ReadCall(Expr.Call call, Dictionary<string, Fact> scope)
        {
            Fact[] arguments = ReadOperands(call.Arguments, scope);
            if (arguments.Contains(Fact.Unreachable))
            {
                return Fact.Unreachable;
            }

            var signature = (call.Function, arguments.Length);
            if (!call.DynamicRemote && functions.ContainsKey(signature)
                && call.Module is null)
            {
                AddCall(signature, arguments);
                if (!dependents.TryGetValue(signature, out var callers))
                {
                    callers = [];
                    dependents.Add(signature, callers);
                }

                callers.Add(current);

                return results.GetValueOrDefault(signature);
            }

            return !call.DynamicRemote && call.Module is (null or ErlangModuleNames.Module)
                && call.Function == ErlangModuleNames.Tail && arguments is [Fact.ProperList]
                && owner.ResolvesErlangBif(call) ? Fact.ProperList : Fact.Unknown;
        }

        private Fact[] ReadOperands(IReadOnlyList<Expr> operands, Dictionary<string, Fact> scope)
        {
            var values = new Fact[operands.Count];
            for (int index = 0; index < operands.Count; index++)
            {
                values[index] = Read(operands[index], scope);
                if (values[index] == Fact.Unreachable)
                {
                    break;
                }
            }

            return values;
        }

        private void ObserveUnknownCalls(Expr expression)
        {
            foreach (Expr.Call call in LocalFunctionEntries.Walk(expression).OfType<Expr.Call>())
            {
                if (!call.DynamicRemote && call.Module is null)
                {
                    AddCall((call.Function, call.Arguments.Count), UnknownArguments(call.Arguments.Count));
                }
            }
        }

        private static void Bind(Pattern pattern, Fact fact, Dictionary<string, Fact> scope)
        {
            switch (pattern)
            {
                case Pattern.Variable variable when variable.Name != VariableScopeNames.Wildcard:
                    scope.TryAdd(variable.Name, fact);
                    break;
                case Pattern.List list:
                    foreach (var item in list.Items)
                    {
                        Bind(item, Fact.Unknown, scope);
                    }

                    if (list.Tail is { } tail)
                    {
                        Bind(tail, fact, scope);
                    }

                    break;
                case AliasPattern alias:
                    Bind(alias.Left, fact, scope);
                    Bind(alias.Right, fact, scope);
                    break;
                default:
                    foreach (string name in Semantics.Variables(pattern))
                    {
                        scope.TryAdd(name, Fact.Unknown);
                    }

                    break;
            }
        }

        private static Fact[] UnknownArguments(int count) => Enumerable.Repeat(Fact.Unknown, count).ToArray();

        private static Fact Join(Fact left, Fact right) => (int)left >= (int)right ? left : right;
    }
}
