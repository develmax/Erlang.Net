using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ThrowStackAnalysis? throwStackAnalysis;

    private bool IsOnLoadEntry((string Name, int Arity) function) =>
        function.Arity == 0 && function.Name == sourceModule?.OnLoadFunction;

    private sealed record ThrowHandlerScope(Expr Handler, Clause Root);

    private sealed record ThrowCallSite(
        (string Name, int Arity) Caller,
        Expr.Call Call,
        ThrowHandlerScope? Handlers,
        bool InClosure
    );

    private sealed class ThrowStackAnalysis
    {
        internal Dictionary<(string Name, int Arity), List<ThrowCallSite>> Incoming { get; } = [];
        internal Dictionary<(string Name, int Arity), List<ThrowCallSite>> Throws { get; } = [];
        internal HashSet<(string Name, int Arity)> Escaping { get; } = [];
    }

    private enum ThrowHandling
    {
        Propagates,
        Caught,
        StackObserved
    }

    private enum ThrowPatternMatch
    {
        No,
        Possible,
        Yes
    }

    private sealed partial class GeneralEmitter
    {
        private EntryCall PrepareStackFreeTailThrow(EntryCall entry, string[]? saved = null)
        {
            if (entry.Module != ErlangModuleNames.Module || entry.ConstructedArgument is not null
                || entry.FunctionArgument is not null || saved is null && entry.SavedInputs is not null
                || ReadStackFreeThrow(entry.Call) is not { } call)
            {
                return entry;
            }

            if (entry.RegisterConstructions is not null)
            {
                if (saved is null || ReadCallArgumentConstructions(
                    call.Arguments,
                    -1,
                    saved.Select((name, slot) => (Name: name, Slot: slot))
                        .ToDictionary(binding => binding.Name, binding => binding.Slot, StringComparer.Ordinal)
                ) is not { } constructions)
                {
                    return entry;
                }

                return entry with { Call = call, RegisterConstructions = constructions };
            }

            var moves = entry.Moves.Select(move => (Destination: 1, move.Source)).ToList();
            if (entry.Constants.Count == 0 && moves.Count == 0 && entry.SavedInputs is null)
            {
                moves.Add((1, 0));
            }

            List<(int Destination, Term Value)> constants = [(0, Term.A(ErlangExceptionClasses.Throw)), (2, Nil.Value)];
            constants.AddRange(entry.Constants.Select(item => (1, item.Value)));

            return entry with
            {
                Call = call,
                Moves = moves,
                Constants = constants,
                SavedInputs = entry.SavedInputs?.Select(move => (Destination: 1, move.Source)).ToList()
            };
        }

        private Expr.Call? ReadStackFreeThrow(Expr.Call call, bool requireCallerHandler = false)
        {
            if (call.Function != AutoImportNames.Throw || call.Arguments is not [var reason]
                || !owner.ResolvesErlangBif(call) || !CanDiscardThrowStack(call, reason, requireCallerHandler))
            {
                return null;
            }

            return call with
            {
                Module = ErlangModuleNames.Module,
                Function = ErlangRaiseBifNames.Raise,
                Arguments = [new Expr.Literal(Term.A(ErlangExceptionClasses.Throw)), reason, new Expr.Literal(Nil.Value)]
            };
        }

        private void EmitStackFreeThrow(Expr.Call call, bool tail)
        {
            int argumentBase = nextSlot;
            var previous = TryEmitThrowReason(call.Arguments[1]) is { } reasonLocation
                ? reasonLocation
                : LoadCallArguments(
                    call.Arguments,
                    null,
                    orderSimpleOperands: true,
                    callSite: CallLocation(call, location)
                );
            owner.EmitCallLine(call, previous);
            owner.Op(tail ? TailExternalCallWithFrame : ExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, call.Arguments.Count));
            if (tail)
            {
                owner.Operand(BeamWire.UnsignedTag, frameSize);
            }

            nextSlot = Math.Max(argumentBase, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);
        }

        private BeamSourceLocation? TryEmitThrowReason(Expr reason)
        {
            if (owner.receiveScratchRegisters is not null || reason is not Expr.Tuple tuple)
            {
                return null;
            }

            Expr.Call? producer = null;
            var registers = new Dictionary<string, int>(StringComparer.Ordinal);
            var flattened = FlattenCallArguments([], tuple);
            if (flattened is
                [Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Call computation }, Expr.Tuple result])
            {
                producer = computation;
                tuple = result;
                registers.Add(binding.Name, 0);
            }
            else if (flattened.Count != 1)
            {
                return null;
            }

            var saved = variables.Where(binding => !deferredPairMembers.ContainsKey(binding.Key))
                .ToDictionary(binding => binding.Key, binding => binding.Value, StringComparer.Ordinal);
            if (ReadCallArgumentConstructions(
                [new Expr.Literal(Nil.Value), tuple],
                0,
                saved,
                inputs: registers
            ) is not { } plan)
            {
                return null;
            }

            var previous = location;
            if (producer is not null)
            {
                int lines = owner.lineInstructions;
                Expression(producer);
                if (owner.lineInstructions != lines)
                {
                    previous = CallLocation(producer, location);
                }
            }

            EmitCallArgumentConstructions(plan, -1);
            owner.EmitConstant(Nil.Value, 2);
            owner.EmitConstant(Term.A(ErlangExceptionClasses.Throw), 0);

            return previous;
        }

        private bool CanDiscardThrowStack(Expr.Call call, Expr reason, bool requireCallerHandler)
        {
            if (owner.ModuleOptimizationDisabled || recursiveClosure is not null || parentFunction is not { } function)
            {
                return false;
            }

            var analysis = owner.throwStackAnalysis ??= ReadThrowStackAnalysis();
            var key = (function.Name, function.Arity);
            var site = analysis.Throws.GetValueOrDefault(key)?
                .Find(candidate => ReferenceEquals(candidate.Call.Arguments, call.Arguments));
            if (site is null || site.InClosure || requireCallerHandler && site.Handlers is not null)
            {
                return false;
            }

            var local = ReadThrowHandling(site.Handlers, reason);
            if (local != ThrowHandling.Propagates)
            {
                return local == ThrowHandling.Caught;
            }

            var visited = new HashSet<(string Name, int Arity)> { key };
            var pending = new Queue<(string Name, int Arity)>();
            pending.Enqueue(key);
            bool foundHandler = false;
            while (pending.TryDequeue(out var target))
            {
                if (owner.exportedFunctions.Contains(target) || analysis.Escaping.Contains(target)
                    || owner.IsDeclaredNative(target) || owner.IsOnLoadEntry(target)
                    || !analysis.Incoming.TryGetValue(target, out var callers))
                {
                    return false;
                }

                foreach (var caller in callers)
                {
                    var handling = ReadThrowHandling(caller.Handlers, reason, requireCallerHandler);
                    if (handling == ThrowHandling.StackObserved || handling == ThrowHandling.Propagates && caller.InClosure)
                    {
                        return false;
                    }

                    if (handling == ThrowHandling.Caught)
                    {
                        foundHandler = true;
                    }
                    else if (visited.Add(caller.Caller))
                    {
                        pending.Enqueue(caller.Caller);
                    }
                }
            }

            return foundHandler;
        }

        private ThrowStackAnalysis ReadThrowStackAnalysis()
        {
            var analysis = new ThrowStackAnalysis();
            foreach (var function in localFunctions.Values)
            {
                var signature = (function.Name, function.Arity);
                foreach (var clause in function.Clauses)
                {
                    foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]))
                    {
                        Visit(
                            expression,
                            signature,
                            clause,
                            null,
                            false
                        );
                    }
                }
            }

            return analysis;

            void Visit(
                Expr expression,
                (string Name, int Arity) signature,
                Clause root,
                ThrowHandlerScope? handlers,
                bool inClosure
            )
            {
                switch (expression)
                {
                    case Expr.FunctionReference reference:
                        analysis.Escaping.Add((reference.Name, reference.Arity));
                        break;
                    case Expr.Call call:
                        var target = (call.Function, call.Arguments.Count);
                        var site = new ThrowCallSite(
                            signature,
                            call,
                            handlers,
                            inClosure
                        );
                        if (call is { Module: null, DynamicRemote: false } && localFunctions.ContainsKey(target))
                        {
                            Add(analysis.Incoming, target, site);
                        }
                        else if (target == (AutoImportNames.Throw, 1) && owner.ResolvesErlangBif(call))
                        {
                            Add(analysis.Throws, signature, site);
                        }
                        break;
                    case Expr.Catch caught:
                        Visit(
                            caught.Operand,
                            signature,
                            root,
                            new(caught, root),
                            inClosure
                        );

                        return;
                    case Expr.Try tried:
                        var scope = tried.Catches.Count == 0 && tried.After is null
                            ? handlers : new ThrowHandlerScope(tried, root);
                        Visit(
                            tried.Body,
                            signature,
                            root,
                            scope,
                            inClosure
                        );
                        var clauseScope = tried.After is null ? handlers : scope;
                        foreach (var clauseExpression in LocalFunctionEntries.ClauseExpressions(tried.Clauses.Concat(tried.Catches)))
                        {
                            Visit(
                                clauseExpression,
                                signature,
                                root,
                                clauseScope,
                                inClosure
                            );
                        }

                        if (tried.After is { } after)
                        {
                            Visit(
                                after,
                                signature,
                                root,
                                handlers,
                                inClosure
                            );
                        }

                        return;
                    case Expr.Fun:
                        handlers = null;
                        inClosure = true;
                        break;
                }

                foreach (var child in LocalFunctionEntries.Children(expression))
                {
                    Visit(
                        child,
                        signature,
                        root,
                        handlers,
                        inClosure
                    );
                }
            }

            static void Add(
                Dictionary<(string Name, int Arity), List<ThrowCallSite>> calls,
                (string Name, int Arity) function,
                ThrowCallSite site
            )
            {
                if (!calls.TryGetValue(function, out var sites))
                {
                    calls.Add(function, sites = []);
                }

                sites.Add(site);
            }
        }

        private static ThrowHandling ReadThrowHandling(
            ThrowHandlerScope? scope,
            Expr reason,
            bool requireCallerHandler = false
        )
        {
            if (scope is null)
            {
                return ThrowHandling.Propagates;
            }

            var thrownClass = new Expr.Literal(Term.A(ErlangExceptionClasses.Throw));
            if (scope.Handler is Expr.Catch)
            {
                return ThrowHandling.Caught;
            }

            var tried = (Expr.Try)scope.Handler;
            if (tried.After is not null || requireCallerHandler && tried.Catches.Any(handler =>
                handler.Patterns is [Pattern.Tuple { Items: [_, _, Pattern.Variable stack] }]
                && stack.Name != VariableScopeNames.Wildcard
                && ReadThrowNames([handler with { Patterns = [] }]).Contains(stack.Name)))
            {
                // The SSA handler can use the trace both for rethrow and in another catch arm.
                return ThrowHandling.StackObserved;
            }
            if (tried.Catches.SelectMany(handler => handler.Patterns)
                .OfType<Pattern.Tuple>()
                .Where(tuple => tuple.Items is
                    [Erlang.Pattern.Variable or Pattern.Literal { Value: Atom { Name: ErlangExceptionClasses.Throw } },
                        Pattern.Literal { Value: Atom or Integer }, _])
                .Select(tuple => ((Pattern.Literal)tuple.Items[1]).Value).Distinct().Skip(1).Any())
            {
                // The OTP switch walk includes its default rethrow edge.
                return ThrowHandling.StackObserved;
            }

            var throwReasons = tried.Catches.SelectMany(handler => handler.Patterns)
                .OfType<Pattern.Tuple>()
                .Where(tuple => tuple.Items is
                    [Pattern.Literal { Value: Atom { Name: ErlangExceptionClasses.Throw } }, _, _])
                .Select(tuple => tuple.Items[1])
                .ToArray();
            if (throwReasons.All(pattern => pattern is Pattern.Tuple)
                && throwReasons.OfType<Pattern.Tuple>().Select(tuple => tuple.Items.Count).Distinct().Skip(1).Any())
            {
                // The OTP handler walk follows every tuple-arity switch edge, including resume.
                return ThrowHandling.StackObserved;
            }

            foreach (var handler in tried.Catches)
            {
                if (handler.Patterns is not [Pattern.Tuple { Items: [var kind, var value, Pattern.Variable stack] }])
                {
                    return ThrowHandling.StackObserved;
                }

                var surrounding = ReadThrowNames([scope.Root], handler);
                var bindings = Semantics.Variables(kind).Concat(Semantics.Variables(value)).ToArray();
                bool fresh = bindings.Distinct(StringComparer.Ordinal).Count() == bindings.Length
                    && !bindings.Any(surrounding.Contains);
                var classMatch = MatchThrowPattern(kind, thrownClass, fresh);
                var reasonMatch = MatchThrowPattern(value, reason, fresh);
                if (classMatch == ThrowPatternMatch.No || reasonMatch == ThrowPatternMatch.No)
                {
                    continue;
                }

                if (stack.Name != VariableScopeNames.Wildcard && (surrounding.Contains(stack.Name)
                    || bindings.Contains(stack.Name, StringComparer.Ordinal)
                    || ReadThrowNames([handler with { Patterns = [] }]).Contains(stack.Name)))
                {
                    return ThrowHandling.StackObserved;
                }

                if (handler.Guard is null && classMatch == ThrowPatternMatch.Yes && reasonMatch == ThrowPatternMatch.Yes)
                {
                    return ThrowHandling.Caught;
                }
            }

            // The caller's landing pad owns the rethrow edge even when an outer scope catches it.
            return ThrowHandling.StackObserved;
        }

        private static ThrowPatternMatch MatchThrowPattern(Pattern pattern, Expr value, bool fresh)
        {
            switch (pattern)
            {
                case Pattern.Variable binding:
                    return fresh || binding.Name == VariableScopeNames.Wildcard ? ThrowPatternMatch.Yes : ThrowPatternMatch.Possible;
                case Pattern.Literal literal when AttributeLiteral.TryNormalizeConstant(value, out Term constant):
                    return literal.Value.Equals(constant) ? ThrowPatternMatch.Yes : ThrowPatternMatch.No;
                case Pattern.Tuple tuple:
                    IReadOnlyList<Expr>? values = (value as Expr.Tuple)?.Items;
                    if (values is null && AttributeLiteral.TryNormalizeConstant(value, out Term term))
                    {
                        if (term is not TupleTerm literal)
                        {
                            return ThrowPatternMatch.No;
                        }

                        values = literal.Items.Select(item => (Expr)new Expr.Literal(item)).ToArray();
                    }

                    if (values is null)
                    {
                        return ThrowPatternMatch.Possible;
                    }

                    if (values.Count != tuple.Items.Count)
                    {
                        return ThrowPatternMatch.No;
                    }

                    var fields = tuple.Items.Zip(values).Select(pair => MatchThrowPattern(pair.First, pair.Second, fresh)).ToArray();

                    return fields.Contains(ThrowPatternMatch.No) ? ThrowPatternMatch.No
                        : fields.Contains(ThrowPatternMatch.Possible) ? ThrowPatternMatch.Possible
                        : ThrowPatternMatch.Yes;
                default:
                    return ThrowPatternMatch.Possible;
            }
        }

        private static HashSet<string> ReadThrowNames(
            IEnumerable<Clause> clauses,
            Clause? excluded = null
        )
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            VisitClauses(clauses);

            return names;

            void VisitClauses(IEnumerable<Clause> items)
            {
                foreach (var clause in items.Where(clause => !ReferenceEquals(clause, excluded)))
                {
                    AddPatterns([clause]);
                    foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]))
                    {
                        Visit(expression);
                    }
                }
            }

            void AddPatterns(IEnumerable<Clause> patterns)
            {
                names.UnionWith(patterns.Where(clause => !ReferenceEquals(clause, excluded))
                    .SelectMany(clause => clause.Patterns).SelectMany(Semantics.Variables));
            }

            void Visit(Expr expression)
            {
                switch (expression)
                {
                    case Expr.Variable variable:
                        names.Add(variable.Name);
                        break;
                    case Expr.Match match:
                        names.UnionWith(Semantics.Variables(match.Pattern));
                        break;
                    case Expr.MaybeMatch match:
                        names.UnionWith(Semantics.Variables(match.Pattern));
                        break;
                    case Expr.Try tried:
                        Visit(tried.Body);
                        VisitClauses(tried.Clauses.Concat(tried.Catches));
                        if (tried.After is { } after)
                        {
                            Visit(after);
                        }

                        return;
                    case Expr.Case decision:
                        AddPatterns(decision.Clauses);
                        break;
                    case Expr.If decision:
                        AddPatterns(decision.Clauses);
                        break;
                    case Expr.Receive receive:
                        AddPatterns(receive.Clauses);
                        break;
                    case Expr.Fun function:
                        AddPatterns(function.Clauses);
                        if (function.SelfName is { } name)
                        {
                            names.Add(name);
                        }
                        break;
                    case Expr.Maybe maybe:
                        AddPatterns(maybe.Clauses);
                        break;
                    case Expr.ListComprehension list:
                        AddQualifiers(list.Qualifiers);
                        break;
                    case Expr.BinaryComprehension binary:
                        AddQualifiers(binary.Qualifiers);
                        break;
                    case Expr.MapComprehension map:
                        AddQualifiers(map.Qualifiers);
                        break;
                }

                foreach (var child in LocalFunctionEntries.Children(expression))
                {
                    Visit(child);
                }
            }

            void AddQualifiers(IEnumerable<ComprehensionQualifier> qualifiers)
            {
                foreach (var qualifier in qualifiers)
                {
                    switch (qualifier)
                    {
                        case ComprehensionQualifier.Generator generator:
                            names.UnionWith(Semantics.Variables(generator.Pattern));
                            break;
                        case ComprehensionQualifier.BinaryGenerator generator:
                            names.UnionWith(Semantics.Variables(generator.Pattern));
                            break;
                        case ComprehensionQualifier.MapGenerator generator:
                            names.UnionWith(Semantics.Variables(generator.Pattern));
                            break;
                        case ComprehensionQualifier.Zip zip:
                            AddQualifiers(zip.Generators);
                            break;
                    }
                }
            }
        }
    }
}
