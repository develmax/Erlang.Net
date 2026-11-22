namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition>? properListSourceFunctions;
    private ProperListEntryFacts? properListEntryFacts;
    private ProperListEntryFacts? listResultEntryFacts;
    private ProperListEntryFacts? structuralAtomEntryFacts;
    private BeamModuleCompiler? listResultFactSource;

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

    private bool HasTuplePairEntryFact((string Name, int Arity) signature, int argument) =>
        ReadPrivateEntryFacts()?.ProvesTuplePair(signature, argument) == true;

    private bool HasNumericEntryFact((string Name, int Arity) signature, int argument) =>
        ReadPrivateEntryFacts()?.ProvesNumber(signature, argument) == true;

    private bool HasMapEntryFact((string Name, int Arity) signature, int argument) =>
        (layoutProbeSource ?? this).ReadPrivateEntryFacts()?.ProvesMap(signature, argument) == true;

    private bool HasListEntryFactWithCallResults((string Name, int Arity) signature, int argument) =>
        (listResultFactSource ?? this).ReadPrivateEntryFacts(includeListResults: true)
            ?.Proves(signature, argument, nonempty: false) == true;

    private IReadOnlySet<Atom>? ReadStructuralEntryAtoms((string Name, int Arity) signature, int argument) =>
        (layoutProbeSource ?? this).ReadPrivateEntryFacts(includeAtomDomains: true)?.ReadAtoms(signature, argument);

    private ProperListEntryFacts? ReadPrivateEntryFacts(bool includeListResults = false, bool includeAtomDomains = false)
    {
        if (properListSourceFunctions is null || !SsaTypeInferenceEnabled || ModuleOptimizationDisabled
            || OnLoadFunction is not null || sourceModule?.DeclaredNifs is { Count: > 0 })
        {
            return null;
        }

        if (includeAtomDomains)
        {
            return structuralAtomEntryFacts ??= new ProperListEntryFacts(
                this,
                properListSourceFunctions,
                exportedFunctions,
                includeListResults: false,
                includeAtomDomains: true
            );
        }

        return includeListResults
            ? listResultEntryFacts ??= new ProperListEntryFacts(
                this,
                properListSourceFunctions,
                exportedFunctions,
                includeListResults: true
            )
            : properListEntryFacts ??= new ProperListEntryFacts(
                this,
                properListSourceFunctions,
                exportedFunctions,
                includeListResults: false
            );
    }

    private sealed partial class ProperListEntryFacts
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

        [Flags]
        private enum IntegerProof
        {
            None = 0,
            BitCapture = 1,
            PairMember = 2,
            FirstOfPair = 4,
            SecondOfPair = 8
        }

        private const IntegerProof ScalarProofs = IntegerProof.BitCapture | IntegerProof.PairMember;
        private const IntegerProof PairProofs = IntegerProof.FirstOfPair | IntegerProof.SecondOfPair;

        private readonly record struct Fact(
            Shape Possible,
            (long Lower, long Upper)? Integer = null,
            (long Lower, long Upper)? Elements = null,
            bool ProvenInteger = false,
            IntegerProof Proof = IntegerProof.None,
            bool IsTuplePair = false,
            bool ProvenNumber = false,
            bool IsMap = false,
            AtomDomain? Atoms = null,
            RecordAtomFields? RecordAtoms = null
        )
        {
            internal static readonly Fact Unreachable = new(Shape.Unreachable);
            internal static readonly Fact EmptyList = new(Shape.EmptyList);
            internal static readonly Fact Unknown = new(Shape.Unknown);
            internal static readonly Fact UnboundedInteger = new(Shape.NonList, ProvenInteger: true);

            internal bool IsInteger => Integer is not null || ProvenInteger;
            internal bool HasIntegerKind => IsInteger || (Proof & ScalarProofs) != 0;

            internal bool IsNumber => HasIntegerKind || ProvenNumber;

            internal bool MayContainCons => (Possible & Shape.Cons) != 0;

            internal Fact Intersect(Shape admitted)
            {
                Shape possible = Possible & admitted;

                return new Fact(
                    possible,
                    (possible & Shape.NonList) != 0 ? Integer : null,
                    (possible & Shape.Cons) != 0 ? Elements : null,
                    (possible & Shape.NonList) != 0 && ProvenInteger,
                    (possible & Shape.NonList) != 0 ? Proof : IntegerProof.None,
                    (possible & Shape.NonList) != 0 && IsTuplePair,
                    (possible & Shape.NonList) != 0 && ProvenNumber,
                    (possible & Shape.NonList) != 0 && IsMap,
                    (possible & Shape.NonList) != 0 ? Atoms : null,
                    (possible & Shape.NonList) != 0 ? RecordAtoms : null
                );
            }
        }

        private readonly BeamModuleCompiler owner;
        private readonly bool includeListResults;
        private readonly bool includeAtomDomains;
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
            IReadOnlySet<(string Name, int Arity)> exports,
            bool includeListResults,
            bool includeAtomDomains = false
        )
        {
            this.owner = owner;
            this.includeListResults = includeListResults;
            this.includeAtomDomains = includeAtomDomains;
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

        internal bool ProvesIntegerPairMember((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument) is { } input && (input.Proof & IntegerProof.PairMember) != 0;

        internal bool ProvesTuplePair((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument) is { IsTuplePair: true };

        internal bool ProvesNumber((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument) is { IsNumber: true };

        internal IReadOnlySet<Atom>? ReadAtoms((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument)?.Atoms?.Values.ToHashSet();

        internal bool ProvesMap((string Name, int Arity) signature, int argument) =>
            ReadEntryArgument(signature, argument) is { IsMap: true };

        internal bool ProvesTuplePairResult((string Name, int Arity) signature) =>
            results.TryGetValue(signature, out Fact result) && result.IsTuplePair;

        internal bool ProvesProperListResult((string Name, int Arity) signature) =>
            results.TryGetValue(signature, out Fact result) && result != Fact.Unreachable
            && (result.Possible & ~Shape.ProperList) == 0;

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
            Dictionary<string, Fact> outer,
            Expr.Variable? selector = null
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

                if (selector is not null && clause.Patterns is [var pattern] && MatchesMap(pattern))
                {
                    RefineToMap(selector, scope);
                }

                int trailingExpressions = clause.Guard is null ? 1 : 2;
                foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]).SkipLast(trailingExpressions))
                {
                    ObserveUnknownCalls(expression);
                }

                if (clause.Guard is { } guard)
                {
                    Read(guard, new(scope, StringComparer.Ordinal));
                    RefineSuccessfulMapGuard(guard, scope);
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
                    return ReadLiteral(literal.Value, includeAtomDomains);
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
                        if (MatchesMap(match.Pattern))
                        {
                            matched = matched with { IsMap = true };
                            if (match.Value is Expr.Variable receiver)
                            {
                                RefineToMap(receiver, scope);
                            }
                        }

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
                case Expr.ListComprehension comprehension:
                    ObserveUnknownCalls(comprehension);

                    // Every normal comprehension result is a nil-terminated list.
                    return new Fact(Shape.ProperList);
                case Expr.Tuple tuple:
                    Fact[] fields = ReadOperands(tuple.Items, scope);
                    if (fields.Contains(Fact.Unreachable))
                    {
                        return Fact.Unreachable;
                    }

                    if (fields is [var first, var second])
                    {
                        IntegerProof proof = first.HasIntegerKind ? IntegerProof.FirstOfPair : IntegerProof.None;
                        if (second.HasIntegerKind)
                        {
                            proof |= IntegerProof.SecondOfPair;
                        }

                        return Fact.Unknown with { Proof = proof, IsTuplePair = true };
                    }

                    return Fact.Unknown;
                case Expr.Map map:
                    if (map.Base is { } baseValue && Read(baseValue, scope) == Fact.Unreachable)
                    {
                        return Fact.Unreachable;
                    }

                    foreach (MapField field in map.Fields)
                    {
                        if (Read(field.Key, scope) == Fact.Unreachable || Read(field.Value, scope) == Fact.Unreachable)
                        {
                            return Fact.Unreachable;
                        }
                    }

                    if (map.Base is Expr.Variable updated)
                    {
                        RefineToMap(updated, scope);
                    }

                    return Fact.Unknown with { IsMap = true };
                case Expr.RecordConstruction { Wildcard: null } selected
                    when includeAtomDomains && selected.Definition.Fields.All(field => field.Value is Expr.Literal):
                    return ReadRecordAtoms(selected, scope);
                case Expr.RecordUpdate updatedRecord when includeAtomDomains:
                    return ReadUpdatedRecordAtoms(updatedRecord, scope);
                case Expr.RecordAccess access when includeAtomDomains:
                    return ReadRecordFieldAtoms(access, scope);
                case Expr.RecordConstruction { Wildcard: null } record
                    when record.Definition.Fields.All(field => field.Value is Expr.Literal):
                    // The surrounding bindings apply to each explicit field.
                    // Independent scopes conservatively exclude newly bound siblings.
                    foreach (RecordField field in record.Fields)
                    {
                        Read(field.Value, new(scope, StringComparer.Ordinal));
                    }

                    return Fact.Unknown;
                case Expr.RecordUpdate update:
                    Read(update.Value, new(scope, StringComparer.Ordinal));
                    foreach (RecordUpdateField field in update.Fields)
                    {
                        Read(field.Value, new(scope, StringComparer.Ordinal));
                    }

                    return Fact.Unknown;
                case Expr.Call call:
                    return ReadCall(call, scope);
                case Expr.Unary unary:
                    var unaryOperand = Read(unary.Operand, scope);
                    if (unaryOperand == Fact.Unreachable)
                    {
                        return Fact.Unreachable;
                    }

                    return includeAtomDomains && HasBooleanOperatorResult(unary) ? BooleanSuccess : Fact.Unknown;
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

                    if (includeAtomDomains && HasBooleanOperatorResult(binary))
                    {
                        return BooleanSuccess;
                    }

                    // Keep the integer kind without expanding ranges at every recursive step.
                    if (binary.Operator is not (ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply))
                    {
                        return Fact.Unknown;
                    }

                    if (leftOperand.IsInteger && rightOperand.IsInteger)
                    {
                        return Fact.UnboundedInteger;
                    }

                    // Arithmetic reaches a caller only on successful numeric
                    // operands. Preserve that fact without assuming an integer.
                    return leftOperand.HasIntegerKind && rightOperand.HasIntegerKind
                        ? Fact.Unknown with { Proof = (leftOperand.Proof | rightOperand.Proof) & ScalarProofs, ProvenNumber = true }
                        : Fact.Unknown with { ProvenNumber = true };
                case Expr.Case decision:
                    return ReadClauses(
                        decision.Clauses,
                        [Read(decision.Value, scope)],
                        scope,
                        decision.Value as Expr.Variable
                    );
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

            if (includeAtomDomains && !call.DynamicRemote && owner.ResolvesErlangBif(call)
                && signature is (AutoImportNames.Exit or AutoImportNames.Throw, 1) or (AutoImportNames.Error, 1 or 2 or 3))
            {
                return Fact.Unreachable;
            }

            if (includeAtomDomains && owner.HasBooleanCallResult(call, functions.ContainsKey(signature)))
            {
                return BooleanSuccess;
            }

            if (!call.DynamicRemote && call.Module == ListModuleNames.Module
                && (arguments.Length == 1 && (call.Function == ListModuleNames.Flatten
                        || includeListResults && call.Function == ListModuleNames.Reverse)
                    || arguments.Length == 2 && (call.Function == ListModuleNames.Subtract
                        || includeListResults && call.Function == ListModuleNames.Map)))
            {
                // These list producers return a proper list on success. Errors
                // at their original call cannot enter a subsequent callee.
                return new Fact(Shape.ProperList);
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

        private void RefineSuccessfulMapGuard(Expr guard, Dictionary<string, Fact> scope)
        {
            if (guard is Expr.Call
                {
                    Function: AutoImportNames.IsMap,
                    DynamicRemote: false,
                    Arguments: [Expr.Variable tested]
                } call && owner.ResolvesErlangBif(call))
            {
                RefineToMap(tested, scope);
            }
            else if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso or ErlangOperators.And } conjunction)
            {
                RefineSuccessfulMapGuard(conjunction.Left, scope);
                RefineSuccessfulMapGuard(conjunction.Right, scope);
            }
        }

        private static bool MatchesMap(Pattern pattern) => pattern switch
        {
            MapPattern => true,
            AliasPattern alias => MatchesMap(alias.Left) || MatchesMap(alias.Right),
            _ => false
        };

        private static void RefineToMap(Expr.Variable value, Dictionary<string, Fact> scope)
        {
            if (scope.TryGetValue(value.Name, out Fact prior))
            {
                scope[value.Name] = prior with { IsMap = true };
            }
        }

        private static void Bind(Pattern pattern, Fact fact, Dictionary<string, Fact> scope)
        {
            if (MatchesMap(pattern) && fact != Fact.Unreachable)
            {
                fact = fact with { IsMap = true };
            }

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
                case Pattern.Tuple { Items: [var first, var second] }:
                    Bind(
                        first,
                        (fact.Proof & IntegerProof.FirstOfPair) != 0
                            ? Fact.Unknown with { Proof = IntegerProof.PairMember } : Fact.Unknown,
                        scope
                    );
                    Bind(
                        second,
                        (fact.Proof & IntegerProof.SecondOfPair) != 0
                            ? Fact.Unknown with { Proof = IntegerProof.PairMember } : Fact.Unknown,
                        scope
                    );
                    break;
                case BitPattern bits:
                    foreach (var segment in bits.Segments)
                    {
                        // A successful match binds an integer segment, regardless
                        // of signedness, endianness, size, or unit.
                        BitSegment specification = segment.Specification;
                        Fact captured = specification.Type == BitSegmentTypes.Integer
                            ? Fact.Unknown with { Proof = IntegerProof.BitCapture } : Fact.Unknown;
                        if (specification is { Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit, Signed: false }
                            && (specification.Size is null
                                || specification.Size is Expr.Literal { Value: Integer width }
                                    && width.Value == BitSyntaxDefaults.IntegerWidth))
                        {
                            // A successful unsigned octet extraction bounds the value
                            // regardless of the remainder of the input bitstring.
                            captured = new Fact(Shape.NonList, (0, byte.MaxValue), Proof: IntegerProof.BitCapture);
                        }

                        Bind(segment.Value, captured, scope);
                    }

                    break;
                case AliasPattern { Left: Pattern.Literal { Value: Integer leftInteger } } left:
                    Bind(left.Right, ReadLiteral(leftInteger), scope);
                    break;
                case AliasPattern { Right: Pattern.Literal { Value: Integer rightInteger } } right:
                    Bind(right.Left, ReadLiteral(rightInteger), scope);
                    break;
                case AliasPattern alias:
                    if (alias.Left is Pattern.Tuple { Items.Count: 2 } || alias.Right is Pattern.Tuple { Items.Count: 2 })
                    {
                        // On a successful match the whole alias is the checked
                        // pair, even when the incoming list head was unknown.
                        fact = fact with { IsTuplePair = true };
                    }

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

        private static Fact ReadLiteral(Term value, bool includeAtoms = false)
        {
            switch (value)
            {
                case Atom atom when includeAtoms:
                    return new Fact(Shape.NonList, Atoms: new AtomDomain([atom]));
                case Nil:
                    return Fact.EmptyList;
                case Integer integer:
                    return new Fact(Shape.NonList, ReadSmallInteger(integer), ProvenInteger: true);
                case FloatTerm:
                    return new Fact(Shape.NonList, ProvenNumber: true);
                case TupleTerm { Items.Count: 2 }:
                    return new Fact(Shape.NonList, IsTuplePair: true);
                case MapTerm:
                    return new Fact(Shape.NonList, IsMap: true);
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

            IntegerProof proof = (left.Proof & right.Proof) & PairProofs;
            if (left.HasIntegerKind && right.HasIntegerKind)
            {
                proof |= (left.Proof | right.Proof) & ScalarProofs;
            }

            return new Fact(
                left.Possible | right.Possible,
                JoinRange(left.Integer, right.Integer),
                elements,
                left.IsInteger && right.IsInteger,
                proof,
                left.IsTuplePair && right.IsTuplePair,
                left.IsNumber && right.IsNumber,
                left.IsMap && right.IsMap,
                AtomDomain.Join(left.Atoms, right.Atoms),
                RecordAtomFields.Join(left.RecordAtoms, right.RecordAtoms)
            );
        }

        private static (long Lower, long Upper)? JoinRange(
            (long Lower, long Upper)? left,
            (long Lower, long Upper)? right
        ) => left is { } a && right is { } b ? (Math.Min(a.Lower, b.Lower), Math.Max(a.Upper, b.Upper)) : null;
    }
}
