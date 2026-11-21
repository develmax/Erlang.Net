namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition>? properListSourceFunctions;
    private ProperListEntryFacts? properListEntryFacts;

    private bool HasProperListEntryFact((string Name, int Arity) signature, int argument) =>
        HasLocalListEntryFact(signature, argument, nonempty: false);

    private bool HasNonemptyListEntryFact((string Name, int Arity) signature, int argument) =>
        HasLocalListEntryFact(signature, argument, nonempty: true);

    private bool HasLocalListEntryFact((string Name, int Arity) signature, int argument, bool nonempty) =>
        ReadPrivateEntryFacts()?.Proves(signature, argument, nonempty) == true;

    private (long Lower, long Upper)? ReadIntegerEntryRange((string Name, int Arity) signature, int argument) =>
        ReadPrivateEntryFacts()?.ReadIntegerRange(signature, argument);

    private bool HasIntegerEntryFact((string Name, int Arity) signature, int argument) =>
        ReadPrivateEntryFacts()?.ProvesInteger(signature, argument) == true;

    private ProperListEntryFacts? ReadPrivateEntryFacts()
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
        {
            return null;
        }

        return properListEntryFacts ??= new ProperListEntryFacts(this, properListSourceFunctions, exportedFunctions);
    }

    private sealed class ProperListEntryFacts
    {
        [Flags]
        private enum Shape
        {
            Unreachable = 0,
            EmptyList = 1,
            ProperCons = 2,
            ImproperCons = 4,
            NonList = 8,
            ProperList = EmptyList | ProperCons,
            Cons = ProperCons | ImproperCons,
            Unknown = ProperList | ImproperCons | NonList
        }

        private readonly record struct Fact(
            Shape Possible,
            (long Lower, long Upper)? Integer = null,
            (long Lower, long Upper)? Elements = null,
            bool ProvenInteger = false
        )
        {
            internal static readonly Fact Unreachable = new(Shape.Unreachable);
            internal static readonly Fact EmptyList = new(Shape.EmptyList);
            internal static readonly Fact Unknown = new(Shape.Unknown);
            internal static readonly Fact UnboundedInteger = new(Shape.NonList, ProvenInteger: true);

            internal bool IsInteger => Integer is not null || ProvenInteger;

            internal bool MayContainCons => (Possible & Shape.Cons) != 0;

            internal Fact Intersect(Shape admitted)
            {
                Shape possible = Possible & admitted;

                return new Fact(
                    possible,
                    (possible & Shape.NonList) != 0 ? Integer : null,
                    (possible & Shape.Cons) != 0 ? Elements : null,
                    (possible & Shape.NonList) != 0 && ProvenInteger
                );
            }
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
                Fact joined = Join(previous, result);
                if (joined == previous)
                {
                    continue;
                }

                results[current] = joined;
                if (dependents.TryGetValue(current, out var callers))
                {
                    foreach (var caller in callers)
                    {
                        Enqueue(caller);
                    }
                }
            }
        }

        internal bool Proves((string Name, int Arity) signature, int argument, bool nonempty) =>
            ReadEntryArgument(signature, argument) is { } input && input != Fact.Unreachable
            && (input.Possible & ~(nonempty ? Shape.Cons : Shape.ProperList)) == 0;

        internal (long Lower, long Upper)? ReadIntegerRange((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument)?.Integer;

        internal bool ProvesInteger((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument) is { IsInteger: true };

        private Fact? ReadEntryArgument((string Name, int Arity) signature, int argument) =>
            !exports.Contains(signature) && incoming.TryGetValue(signature, out var arguments)
            && argument >= 0 && argument < arguments.Length ? arguments[argument] : null;

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
                    return ReadLiteral(literal.Value);
                case Expr.Variable variable:
                    return scope.GetValueOrDefault(variable.Name, Fact.Unknown);
                case Expr.Match match:
                    Fact matched = Read(match.Value, scope);
                    foreach (var patternExpression in LocalFunctionEntries.Children(match).SkipLast(1))
                    {
                        ObserveUnknownCalls(patternExpression);
                    }

                    if (match.Pattern is Pattern.List { Items.Count: > 0 })
                    {
                        matched = matched.Intersect(Shape.Cons);
                    }

                    if (matched != Fact.Unreachable)
                    {
                        Bind(match.Pattern, matched, scope);
                    }

                    return matched;
                case Expr.Sequence sequence:
                    return ReadSequence(sequence, scope);
                case Expr.Block block:
                    return Read(block.Body, scope);
                case Expr.List list:
                    Fact[] elements = ReadOperands(list.Items, scope);
                    if (elements.Contains(Fact.Unreachable))
                    {
                        return Fact.Unreachable;
                    }

                    Fact suffix = list.Tail is null ? Fact.EmptyList : Read(list.Tail, scope);
                    for (int index = elements.Length - 1; index >= 0; index--)
                    {
                        suffix = BuildCons(elements[index], suffix);
                    }

                    return suffix;
                case Expr.Tuple tuple:
                    return ReadOperands(tuple.Items, scope).Contains(Fact.Unreachable) ? Fact.Unreachable : Fact.Unknown;
                case Expr.Call call:
                    return ReadCall(call, scope);
                case Expr.Unary unary:
                    return Read(unary.Operand, scope) == Fact.Unreachable ? Fact.Unreachable : Fact.Unknown;
                case Expr.Binary binary:
                    Fact leftOperand = Read(binary.Left, scope);
                    if (leftOperand == Fact.Unreachable)
                    {
                        return Fact.Unreachable;
                    }

                    if (binary.Operator is ErlangOperators.AndAlso or ErlangOperators.OrElse)
                    {
                        Read(binary.Right, new(scope, StringComparer.Ordinal));

                        return Fact.Unknown;
                    }

                    Fact rightOperand = Read(binary.Right, scope);
                    if (rightOperand == Fact.Unreachable)
                    {
                        return Fact.Unreachable;
                    }

                    // Keep the integer kind without expanding ranges at every recursive step.
                    return binary.Operator is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                        && leftOperand.IsInteger && rightOperand.IsInteger ? Fact.UnboundedInteger : Fact.Unknown;
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

        private Fact ReadSequence(Expr.Sequence sequence, Dictionary<string, Fact> scope)
        {
            // Successful pattern matches narrow subsequent expressions in this sequence.
            // Existing bindings in the outer scope remain independent of sibling evaluation.
            var continuation = new Dictionary<string, Fact>(scope, StringComparer.Ordinal);
            Fact result = Fact.Unknown;
            foreach (Expr item in sequence.Items)
            {
                result = Read(item, continuation);
                if (result == Fact.Unreachable)
                {
                    return result;
                }

                if (item is Expr.Match
                    {
                        Pattern: Pattern.List { Items.Count: > 0 },
                        Value: Expr.Variable source
                    })
                {
                    continuation[source.Name] = result;
                }
            }

            foreach (var (name, fact) in continuation)
            {
                scope.TryAdd(name, fact);
            }

            return result;
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
                // The invocation can only happen after every argument has evaluated successfully.
                // Keep these facts on the call edge so sibling evaluation order cannot affect them.
                for (int projection = 0; projection < arguments.Length; projection++)
                {
                    if (call.Arguments[projection] is not Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.Hd or AutoImportNames.Tl,
                            Arguments: [Expr.Variable projected]
                        } prior || !owner.ResolvesErlangBif(prior))
                    {
                        continue;
                    }

                    for (int argument = 0; argument < arguments.Length; argument++)
                    {
                        if (call.Arguments[argument] is Expr.Variable forwarded && forwarded.Name == projected.Name)
                        {
                            arguments[argument] = arguments[argument].Intersect(Shape.Cons);
                        }
                    }
                }

                AddCall(signature, arguments);
                if (!dependents.TryGetValue(signature, out var callers))
                {
                    callers = [];
                    dependents.Add(signature, callers);
                }

                callers.Add(current);

                return results.GetValueOrDefault(signature);
            }

            if (call.DynamicRemote || call.Module is not (null or ErlangModuleNames.Module)
                || call.Function is not (AutoImportNames.Hd or AutoImportNames.Tl)
                || arguments is not [var input] || !owner.ResolvesErlangBif(call))
            {
                return Fact.Unknown;
            }

            Fact nonempty = input.Intersect(Shape.Cons);

            if (nonempty == Fact.Unreachable)
            {
                return Fact.Unreachable;
            }

            return call.Function == AutoImportNames.Tl ? ReadTail(nonempty) : ReadHead(nonempty);
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
                        Bind(item, ReadHead(fact), scope);
                    }

                    if (list.Tail is { } tail)
                    {
                        Bind(tail, list.Items.Count == 0 ? fact : ReadTail(fact), scope);
                    }

                    break;
                case AliasPattern { Left: Pattern.Literal { Value: Integer leftInteger } } left:
                    Bind(left.Right, ReadLiteral(leftInteger), scope);
                    break;
                case AliasPattern { Right: Pattern.Literal { Value: Integer rightInteger } } right:
                    Bind(right.Left, ReadLiteral(rightInteger), scope);
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

        private static Fact ReadLiteral(Term value)
        {
            switch (value)
            {
                case Nil:
                    return Fact.EmptyList;
                case Integer integer:
                    return new Fact(Shape.NonList, ReadSmallInteger(integer), ProvenInteger: true);
                case Cons first:
                    Term tail = first.Tail;
                    var elements = ReadSmallInteger(first.Head);
                    for (; tail is Cons cell; tail = cell.Tail)
                    {
                        elements = JoinRange(elements, ReadSmallInteger(cell.Head));
                    }

                    return new Fact(tail is Nil ? Shape.ProperCons : Shape.ImproperCons, Elements: elements);
                default:
                    return new Fact(Shape.NonList);
            }
        }

        private static (long Lower, long Upper)? ReadSmallInteger(Term value) =>
            value is Integer integer && integer.Value >= long.MinValue && integer.Value <= long.MaxValue
                ? ((long)integer.Value, (long)integer.Value) : null;

        private static Fact BuildCons(Fact head, Fact tail)
        {
            Shape proper = (tail.Possible & Shape.ProperList) != 0 ? Shape.ProperCons : Shape.Unreachable;
            Shape improper = (tail.Possible & (Shape.ImproperCons | Shape.NonList)) != 0
                ? Shape.ImproperCons : Shape.Unreachable;
            Shape possible = proper | improper;
            var elements = tail.MayContainCons ? JoinRange(head.Integer, tail.Elements) : head.Integer;

            return possible == Shape.Unreachable ? Fact.Unreachable : new Fact(possible, Elements: elements);
        }

        private static Fact ReadTail(Fact input)
        {
            Shape proper = (input.Possible & Shape.ProperCons) != 0 ? Shape.ProperList : Shape.Unreachable;
            Shape improper = (input.Possible & Shape.ImproperCons) != 0
                ? Shape.ImproperCons | Shape.NonList : Shape.Unreachable;
            Shape possible = proper | improper;

            return possible == Shape.Unreachable ? Fact.Unreachable : new Fact(possible, Elements: input.Elements);
        }

        private static Fact ReadHead(Fact list) => list.MayContainCons
            ? list.Elements is { } elements ? new Fact(Shape.NonList, elements) : Fact.Unknown
            : Fact.Unreachable;

        private static Fact Join(Fact left, Fact right)
        {
            if (left == Fact.Unreachable)
            {
                return right;
            }

            if (right == Fact.Unreachable)
            {
                return left;
            }

            var elements = (left.MayContainCons, right.MayContainCons) switch
            {
                (true, true) => JoinRange(left.Elements, right.Elements),
                (true, false) => left.Elements,
                (false, true) => right.Elements,
                _ => null
            };

            return new Fact(
                left.Possible | right.Possible,
                JoinRange(left.Integer, right.Integer),
                elements,
                left.IsInteger && right.IsInteger
            );
        }

        private static (long Lower, long Upper)? JoinRange(
            (long Lower, long Upper)? left,
            (long Lower, long Upper)? right
        ) => left is { } a && right is { } b ? (Math.Min(a.Lower, b.Lower), Math.Max(a.Upper, b.Upper)) : null;
    }
}
