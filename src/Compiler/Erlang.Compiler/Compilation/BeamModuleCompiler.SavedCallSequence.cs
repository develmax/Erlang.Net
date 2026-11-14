using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SavedCallStep(
            EntryCall Call,
            int? Spill,
            bool ReuseSlot,
            int[] Released,
            PreparedCallClosure? Closure = null,
            (int Slot, int Field, int Register)[]? TupleArguments = null,
            (int Source, int Destination)? SlotRelocation = null,
            bool ReloadRelocatedArgument = false
        );

        private bool TryEmitSavedCallSequence(Expr body, string[] parameters)
        {
            var plan = ReadSavedCallSequence(body, parameters);
            if (plan is null && body is Expr.Sequence { Items: [_, .., Expr.Call] } sequence
                && ReadCallsWithClosures(FlattenCallArguments(sequence.Items, prepareClosures: true), parameters, out var closures)
                    is { Length: > 1 } calls
                && closures.Count != 0)
            {
                plan = ReadSavedCallSequence(
                    calls,
                    parameters,
                    extraction: null,
                    closures: closures
                );
            }

            if (plan is null)
            {
                return false;
            }

            EmitSavedCallSequence(plan);

            return true;
        }

        private sealed record SavedCallSequence(
            EntryCall First,
            int Live,
            List<int> Inputs,
            List<int> LastSlotReads,
            List<SavedCallStep> Steps,
            CallListExtraction? ListExtraction,
            RegisterCase? Decision,
            CallSequenceTuple? Tuple,
            Term? Constant = null,
            IReadOnlyDictionary<int, CallResultCheck>? Checks = null
        )
        {
            internal TupleFieldCase? ProjectedCase { get; init; }
            internal TupleFieldArm? ProjectedReturn { get; init; }
        }

        private SavedCallSequence? ReadSavedCallSequence(
            Expr body,
            string[] parameters,
            CallListExtraction? extraction = null)
        {
            if (body is not Expr.Sequence { Items: [_, .., Expr.Call] } sequence)
            {
                return null;
            }

            var calls = ReadStraightCalls(FlattenCallArguments(sequence.Items), parameters);

            return calls is { Length: >= 3 } ? ReadSavedCallSequence(calls, parameters, extraction) : null;
        }

        private SavedCallSequence? ReadComputedConsTail(Expr.Call tail, string[] parameters)
        {
            if (tail.Arguments.OfType<Expr.List>().ToArray() is not
                [Expr.List { Items: [var head], Tail: Expr.Variable } cons]
                || tail.Arguments.Any(argument => !ReferenceEquals(argument, cons) && !IsPreparedOperand(argument)))
            {
                return null;
            }

            Expr.Call? producer = head switch
            {
                Expr.Call call => call,
                Expr.Binary { Operator: ErlangOperators.Append } append => new Expr.Call(
                    ErlangModuleNames.Module,
                    append.Operator,
                    [append.Left, append.Right]
                )
                {
                    SourceLine = append.SourceLine,
                    PreprocessorFile = tail.PreprocessorFile
                },
                _ => null
            };
            if (producer is null)
            {
                return null;
            }

            const string result = "\0cons-argument-result";
            var sequence = FlattenCallArguments([new Expr.Match(new Pattern.Variable(result), producer)]);
            var prepared = tail with
            {
                Arguments = tail.Arguments.Select(argument => ReferenceEquals(argument, cons)
                    ? cons with { Items = [new Expr.Variable(result)] } : argument).ToArray()
            };
            if (ReadStraightCalls([.. sequence, prepared], parameters) is not { Length: >= 2 } calls)
            {
                return null;
            }

            return ReadSavedCallSequence(
                calls,
                parameters,
                extraction: null,
                allowLinear: true,
                preserveInputOrder: true
            );
        }

        private bool TryEmitSavedCallSequenceCase(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [_, .., Expr.Case decision] } sequence)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            IEnumerable<Expr> statements = sequence.Items.SkipLast(1);
            if (decision.Value is Expr.Call selector)
            {
                statements = statements.Append(selector);
            }

            var flattened = FlattenCallArguments(statements);
            Dictionary<int, CallResultCheck>? checks = null;
            bool checkedSelector = decision.Value is Expr.Call
                || decision.Value is Expr.Variable projected && flattened.LastOrDefault() is { } last
                    && ReadTupleCallCheck(last)?.Check.Bindings?.Any(field => field.Name == projected.Name) == true;
            var calls = checkedSelector && flattened.Any(item => ReadTupleCallCheck(item) is not null)
                ? ReadCallsWithChecks(flattened, parameters, out checks)
                : ReadStraightCalls(flattened, parameters);
            if (calls is not { Length: > 0 }
                || !checkedSelector && (decision.Value is not Expr.Variable result || result.Name != calls[^1].Result)
                || ReadSavedCallSequence(
                    calls,
                    parameters,
                    extraction: null,
                    decision: decision,
                    checks: checks
                ) is not { } plan)
            {
                return false;
            }

            EmitSavedCallSequence(plan);

            return true;
        }

        private static List<Expr> FlattenCallArguments(
            IEnumerable<Expr> expressions,
            Expr.Tuple? result = null,
            bool prepareClosures = false
        )
        {
            var statements = new List<Expr>();
            foreach (Expr expression in expressions)
            {
                statements.Add(expression switch
                {
                    Expr.Match { Value: Expr.Call call } match => match with { Value = FlattenArguments(call) },
                    Expr.Call call => FlattenArguments(call),
                    _ => expression
                });
            }

            if (result is not null)
            {
                statements.Add(FlattenResult(result));
            }

            return statements;

            Expr FlattenResult(Expr value) => value switch
            {
                Expr.Call call => BindCallResult(call),
                Expr.Tuple tuple => tuple with { Items = tuple.Items.Select(FlattenResult).ToArray() },
                Expr.RecordConstruction record => FlattenResult(RecordExpressions.Create(record)),
                Expr.List list => list with
                {
                    Items = list.Items.Select(FlattenResult).ToArray(),
                    Tail = list.Tail is null ? null : FlattenResult(list.Tail)
                },
                _ => value
            };

            Expr.Variable BindCallResult(Expr.Call call)
            {
                Expr.Call prepared = FlattenArguments(call);
                string name = FormattableString.Invariant($"\0call-argument-result-{statements.Count}");
                statements.Add(new Expr.Match(new Pattern.Variable(name), prepared));

                return new Expr.Variable(name);
            }

            Expr.Call FlattenArguments(Expr.Call call)
            {
                if (call.DynamicRemote)
                {
                    return call;
                }

                Expr[] arguments = call.Arguments.Select(argument => argument is Expr.Binary
                    { Operator: ErlangOperators.Append, Left: Expr.Literal, Right: Expr.Variable tail } append
                    && AppendPrefix(append.Left) is { } prefix
                    ? new Expr.List(prefix, tail)
                    : argument).ToArray();
                var nestedCalls = arguments.Select(argument => ReadCallOperand(argument, call.PreprocessorFile)).ToArray();

                bool HasClosure(int index) => prepareClosures && call.Arguments[index] is Expr.Fun { SelfName: null };
                if (nestedCalls.All(nested => nested is null) && !Enumerable.Range(0, arguments.Length).Any(HasClosure))
                {
                    return arguments.Where((argument, index) => !ReferenceEquals(argument, call.Arguments[index])).Any()
                        ? call with { Arguments = arguments }
                        : call;
                }

                if (Enumerable.Range(0, arguments.Length).Any(index =>
                    nestedCalls[index] is null && !HasClosure(index)
                    && ReferenceEquals(arguments[index], call.Arguments[index]) && !IsPreparedOperand(arguments[index])))
                {
                    return call;
                }

                for (int index = 0; index < arguments.Length; index++)
                {
                    if (nestedCalls[index] is { } nested)
                    {
                        arguments[index] = BindCallResult(nested);
                    }
                    else if (HasClosure(index))
                    {
                        string name = FormattableString.Invariant($"\0call-closure-{statements.Count}");
                        statements.Add(new Expr.Match(new Pattern.Variable(name), arguments[index]));
                        arguments[index] = new Expr.Variable(name);
                    }
                }

                return call with { Arguments = arguments };
            }

            static Expr.Call? ReadCallOperand(Expr expression, string? file) => expression switch
            {
                Expr.Call call => call,
                Expr.Binary { Operator: ErlangOperators.Append } append when IsAppendOperand(append.Left)
                    && !AttributeLiteral.TryNormalizeConstant(append, out _)
                    => new Expr.Call(ErlangModuleNames.Module, append.Operator, [append.Left, append.Right])
                    {
                        SourceLine = append.SourceLine,
                        PreprocessorFile = file
                    },
                _ => null
            };
        }

        private SavedCallSequence? ReadSavedCallSequence(
            (Expr.Call Call, string? Result)[] calls,
            string[] parameters,
            CallListExtraction? extraction,
            Expr.Case? decision = null,
            CallSequenceTupleSource? tuple = null,
            IReadOnlyDictionary<int, CallClosure>? closures = null,
            Term? constant = null,
            Dictionary<int, CallResultCheck>? checks = null,
            bool allowLinear = false,
            bool preserveInputOrder = false
        )
        {
            EntryCall? first = extraction is { } list
                ? ReadListSequenceFirstCall(
                    calls,
                    parameters,
                    list,
                    allowSavedHead: false
                )
                : ReadEntryCall(calls[0].Call, parameters, allowConstructedArgument: true);
            if (first is null)
            {
                return null;
            }

            var lastReads = new Dictionary<string, int>(StringComparer.Ordinal);
            var firstReads = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < calls.Length; index++)
            {
                foreach (Expr.Variable read in calls[index].Call.Arguments.SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>())
                {
                    firstReads.TryAdd(read.Name, index);
                    lastReads[read.Name] = index;
                }
            }

            if (closures is not null)
            {
                foreach (var (index, closure) in closures)
                {
                    foreach (string capture in closure.Captures)
                    {
                        lastReads[capture] = Math.Max(index, lastReads.GetValueOrDefault(capture));
                    }
                }
            }

            var finalFields = checks?.GetValueOrDefault(calls.Length - 1)?.Bindings;
            bool projectSelector = decision?.Value is Expr.Variable projected
                && finalFields?.Any(field => field.Name == projected.Name) == true;
            bool projectReturn = tuple is { Closure: null }
                && finalFields?.Any(field => tuple.Reads.Contains(field.Name)) == true;
            if (decision?.Value is Expr.Variable selectorBinding && selectorBinding.Name == calls[^1].Result)
            {
                decision = decision with
                {
                    Clauses = decision.Clauses.Select(branch =>
                        branch is { Guard: null, Patterns: [Pattern.Literal { Value: Atom or Integer } key] }
                            && (IsPreparedOperand(branch.Body)
                                || branch.Body is Expr.Tuple tupleBody && tupleBody.Items.All(IsPreparedOperand)
                                || branch.Body is Expr.Call { DynamicRemote: false } branchCall
                                    && branchCall.Arguments.All(IsPreparedOperand))
                            ? branch with
                            {
                                Body = SubstituteConstants(
                                    branch.Body,
                                    new Dictionary<string, Term>(StringComparer.Ordinal) { [selectorBinding.Name] = key.Value }
                                )
                            }
                            : branch).ToArray()
                };
            }

            if (decision is not null)
            {
                var bindings = parameters.Concat(calls.Select(call => call.Result).OfType<string>())
                    .Concat(ReadAdditionalCallResultBindings(checks))
                    .Where(name => name != VariableScopeNames.Wildcard)
                    .Select(name => (Pattern)new Pattern.Variable(name)).ToArray();
                var branches = decision with { Value = new Expr.Literal(Nil.Value) };
                if (ReadUsedEntryParameters(new Clause(bindings, null, branches)) is not { } survivors
                    || calls[^1].Result is { } result && survivors.Contains(result))
                {
                    return null;
                }

                foreach (string survivor in survivors)
                {
                    lastReads[survivor] = calls.Length;
                }
            }
            else if (tuple is not null)
            {
                foreach (string name in tuple.Reads)
                {
                    if (name != calls[^1].Result)
                    {
                        lastReads[name] = calls.Length;
                    }
                }
            }
            else if (!allowLinear && checks is not { Count: > 0 } && closures is not { Count: > 0 } && !calls.Where((item, index) => item.Result is { } name
                && lastReads.GetValueOrDefault(name) > index + 1).Any())
            {
                return null;
            }

            if (!projectSelector && !projectReturn && finalFields is not null
                && finalFields.Any(field => lastReads.GetValueOrDefault(field.Name, -1) >= calls.Length))
            {
                return null;
            }

            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            var inputs = new List<int>();
            for (int index = 0; index < parameters.Length; index++)
            {
                int register = preserveInputOrder ? parameters.Length - 1 - index : index;
                string name = parameters[register];
                if (lastReads.GetValueOrDefault(name) > 0)
                {
                    saved.Add(name, inputs.Count);
                    inputs.Add(register);
                }
            }

            var lastSlotReads = inputs.Select(register => lastReads[parameters[register]]).ToList();
            var steps = new List<SavedCallStep>();
            var deferredTuples = new List<DeferredCallTuple>();
            for (int index = 1; index < calls.Length; index++)
            {
                Expr.Call target = calls[index].Call;
                string? previous = calls[index - 1].Result;
                PreparedCallClosure? preparedClosure = null;
                int closureOperand = -1;
                if (closures?.TryGetValue(index, out var closure) == true)
                {
                    preparedClosure = PrepareCallClosure(
                        closure,
                        index,
                        target,
                        previous,
                        saved,
                        lastSlotReads,
                        lastReads,
                        out closureOperand
                    );
                    if (preparedClosure is null)
                    {
                        return null;
                    }
                }

                var multiple = checks?.GetValueOrDefault(index - 1);
                bool saveProjectedResult = false;
                if (multiple is { Field: int projection, Bindings: null }
                    && previous is not null && lastReads.GetValueOrDefault(previous) > index
                    && target.Arguments.All(IsPreparedOperand) && preparedClosure is null)
                {
                    // The tuple check can write the live payload straight into its frame slot.
                    multiple = multiple with { Field = null, Bindings = [(projection, previous)] };
                    saveProjectedResult = true;
                }

                DeferredCallTuple? deferredTuple = multiple?.Bindings is not null ? ReadDeferredCallTuple(
                    multiple,
                    index,
                    calls,
                    firstReads,
                    lastReads
                ) : null;
                int? tupleSlot = null;
                if (deferredTuple is not null)
                {
                    tupleSlot = lastSlotReads.Count;
                    saved.Add(deferredTuple.Binding, tupleSlot.Value);
                    lastReads.Add(deferredTuple.Binding, deferredTuple.Consumer);
                    lastSlotReads.Add(deferredTuple.Consumer);
                    deferredTuples.Add(deferredTuple);
                }

                var prior = multiple?.Bindings is not null ? ReadCallResultRegisters(
                    deferredTuple is null ? multiple : multiple with
                    {
                        Bindings = multiple.Bindings.Except(deferredTuple.Fields).ToArray(),
                        SavedTupleSlot = tupleSlot
                    },
                    index,
                    target,
                    lastReads,
                    saved,
                    lastSlotReads,
                    checks!
                ) : previous is null ? [] : new[] { previous };
                var tupleArguments = new List<(int Slot, int Field, int Register)>();
                foreach (var retained in deferredTuples.Where(tuple => tuple.Consumer == index))
                {
                    int slot = saved[retained.Binding];
                    if (prior.Length == 0)
                    {
                        prior = [VariableScopeNames.Wildcard];
                    }

                    foreach (var (field, name) in retained.Fields)
                    {
                        tupleArguments.Add((slot, field, prior.Length));
                        prior = [.. prior, name];
                    }
                }

                if (prior.Length >= FirstReservedXRegister)
                {
                    return null;
                }

                string[] slots = Enumerable.Repeat(VariableScopeNames.Wildcard, lastSlotReads.Count).ToArray();
                foreach (var (name, slot) in saved)
                {
                    slots[slot] = name;
                }

                if ((ReadEntryCall(
                    target,
                    prior,
                    slots,
                    preparedOperand: closureOperand
                )
                    ?? (closureOperand < 0 && tupleArguments.Count == 0
                        ? ReadStraightConstructedCall(target, multiple?.Bindings is null ? previous : null, saved,
                            projectedRegisters: multiple?.Bindings is null ? null : prior) : null)) is not { } step)
                {
                    return null;
                }

                if (preparedClosure is { MoveResult: true })
                {
                    step.Moves.Remove((1, 0));
                }

                if (closureOperand >= 0 && preparedClosure!.Destination != closureOperand)
                {
                    step.Moves.Add((closureOperand, preparedClosure.Destination));
                }

                int[] released = saved.Where(pair => lastReads[pair.Key] == index).Select(pair => pair.Value).ToArray();
                var occupied = saved.Values.ToHashSet();
                bool freeSlotBeforeSelector = decision is not null && index == calls.Length - 1
                    && occupied.Count < lastSlotReads.Count;
                int? reuseSlot = !saveProjectedResult && previous is not null
                    && lastReads.GetValueOrDefault(previous) > index && !freeSlotBeforeSelector
                    ? FindReusableCallSlot(
                        ref step,
                        previous,
                        saved,
                        released,
                        allowSwapWithPreviousArgument: decision is null || index == calls.Length - 1,
                        allowRegisterReload: checks is null && closures is null
                    )
                    : null;
                foreach (int slot in released)
                {
                    saved.Remove(slots[slot]);
                }

                (int Source, int Destination)? slotRelocation = null;
                bool reloadRelocatedArgument = false;
                if (deferredTuple is not null && tupleSlot == lastSlotReads.Count - 1 && released.Length != 0)
                {
                    int destination = released.Min();
                    slotRelocation = (tupleSlot.Value, destination);
                    saved[deferredTuple.Binding] = destination;
                    lastSlotReads[destination] = deferredTuple.Consumer;
                    lastSlotReads[tupleSlot.Value] = index;
                }

                int? spill = null;
                bool reuse = false;
                if (!saveProjectedResult && previous is not null && lastReads.GetValueOrDefault(previous) > index)
                {
                    int slot = reuseSlot ?? 0;
                    while (reuseSlot is null && occupied.Contains(slot))
                    {
                        slot++;
                    }

                    reuse = occupied.Contains(slot);
                    spill = slot;
                    if (slot == lastSlotReads.Count)
                    {
                        lastSlotReads.Add(lastReads[previous]);
                    }
                    else
                    {
                        lastSlotReads[slot] = lastReads[previous];
                    }

                    saved.Add(previous, slot);
                }

                if (slotRelocation is null && spill is null && preparedClosure is null
                    && constant is not null && index == calls.Length - 2
                    && checks?.GetValueOrDefault(index) is { Arity: null }
                    && checks.GetValueOrDefault(index + 1) is { Arity: null }
                    && step.RegisterConstructions is null && step.SavedInputs is { } loads
                    && saved.Count == 1 && released.Length != 0)
                {
                    var retained = saved.Single();
                    int destination = released.Min();
                    if (retained.Value > destination && loads.Contains((0, retained.Value)))
                    {
                        slotRelocation = (retained.Value, destination);
                        reloadRelocatedArgument = true;
                        saved[retained.Key] = destination;
                        lastSlotReads[destination] = lastReads[retained.Key];
                        lastSlotReads[retained.Value] = index;
                    }
                }

                if (slotRelocation is null && spill is null && preparedClosure is null
                    && tuple is not null && index == calls.Length - 1
                    && checks?.Values.Any(check => check is { Tag: not null, Field: not null } or { Tag: not null, Bindings: not null }) == true
                    && step.RegisterConstructions is null && step.SavedInputs is { } finalLoads
                    && saved.Count == 1 && released.Length == 1)
                {
                    var retained = saved.Single();
                    int destination = released[0];
                    if (retained.Value == lastSlotReads.Count - 1 && destination == retained.Value - 1
                        && finalLoads.Contains((0, retained.Value)))
                    {
                        slotRelocation = (retained.Value, destination);
                        reloadRelocatedArgument = true;
                        saved[retained.Key] = destination;
                        lastSlotReads[destination] = lastReads[retained.Key];
                        lastSlotReads[retained.Value] = index;
                    }
                }

                if (slotRelocation is null && spill is null && preparedClosure is null
                    && tuple is not null && index == calls.Length - 1
                    && multiple?.Bindings is not null && step.RegisterConstructions is not null
                    && saved.Count == 1 && released.Length != 0)
                {
                    var retained = saved.Single();
                    int destination = released.Min();
                    if (retained.Value > destination)
                    {
                        slotRelocation = (retained.Value, destination);
                        saved[retained.Key] = destination;
                        lastSlotReads[destination] = lastReads[retained.Key];
                        lastSlotReads[retained.Value] = index;
                    }
                }

                if (slotRelocation is null && spill is null && preparedClosure is null
                    && checks is null && tuple is not null && index < calls.Length - 1
                    && step.RegisterConstructions is null && released is [var freed]
                    && step.SavedInputs is { } intermediateLoads
                    && lastSlotReads.Count > 1
                    && saved.FirstOrDefault(binding => binding.Value == lastSlotReads.Count - 1) is
                        { Key: not null } leading
                    && intermediateLoads.Any(load => load.Destination == 0
                        && load.Source != freed && load.Source != leading.Value))
                {
                    slotRelocation = (leading.Value, freed);
                    saved[leading.Key] = freed;
                    lastSlotReads[freed] = lastReads[leading.Key];
                    lastSlotReads[leading.Value] = index;
                }

                steps.Add(new SavedCallStep(
                    step,
                    spill,
                    reuse,
                    released,
                    preparedClosure,
                    tupleArguments.Count == 0 ? null : tupleArguments.ToArray(),
                    slotRelocation,
                    reloadRelocatedArgument
                ));
            }

            int live = extraction is { } deferred
                ? ListSequenceLive(
                    first,
                    parameters,
                    inputs,
                    deferred
                )
                : Math.Max(EntryCallLiveCount(first, parameters), inputs.DefaultIfEmpty(-1).Max() + 1);
            if (first.ConstructedArgument is { } constructor)
            {
                live = Math.Max(live, constructor.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1);
            }

            RegisterCase? registerCase = null;
            TupleFieldCase? projectedCase = null;
            if (decision is not null)
            {
                int trimmed = Enumerable.Reverse(lastSlotReads).TakeWhile(read => read < calls.Length - 1).Count();
                var branchSlots = saved.ToDictionary(
                    binding => binding.Key,
                    binding => lastSlotReads.Count - 1 - binding.Value - trimmed,
                    StringComparer.Ordinal
                );
                EntryCall selector = steps.Count == 0 ? first : steps[^1].Call;
                bool boolean = (selector.Module, selector.Call.Function, selector.Call.Arguments.Count)
                    is ("lists", "keymember", 3) or ("lists", "member", 2) or ("maps", "is_key", 2);
                if (projectSelector)
                {
                    projectedCase = ReadTupleFieldCase(decision, finalFields!, branchSlots);
                }
                else
                {
                    registerCase = ReadRegisterCase(
                        decision,
                        branchSlots,
                        boolean,
                        allowUnmatchedLiterals: true,
                        selectorCall: selector
                    );
                }

                if (registerCase is null && projectedCase is null)
                {
                    return null;
                }
            }

            CallSequenceTuple? tupleResult = null;
            TupleFieldArm? projectedReturn = null;
            if (tuple is not null)
            {
                int lastCall = calls.Length - 1;
                int trimmed = TupleCallTrim(lastSlotReads, lastCall, steps.LastOrDefault()?.Closure);
                var resultSlots = saved.ToDictionary(
                    binding => binding.Key,
                    binding => lastSlotReads.Count - 1 - binding.Value - trimmed,
                    StringComparer.Ordinal
                );
                if (projectReturn)
                {
                    projectedReturn = ReadTupleFieldArm(tuple.Tuple, finalFields!, resultSlots);
                }
                else
                {
                    tupleResult = ReadCallSequenceTuple(tuple, calls[^1].Result, resultSlots);
                }

                if (tupleResult is null && projectedReturn is null)
                {
                    return null;
                }
            }

            if (checks is not null)
            {
                foreach (int index in checks.Keys.ToArray())
                {
                    var check = checks[index];
                    var producer = index == 0 ? first : steps[index - 1].Call;
                    if (check is { Tag: null, Arity: int arity, Bindings: not null } && producer.Module is null
                        && ReturnsTuple(
                            producer.Call,
                            arity,
                            [],
                            exact: true
                        ))
                    {
                        checks[index] = check with { ShapeKnown = true };
                    }
                }

                PrepareCallResultChecks(
                    calls,
                    lastReads,
                    steps,
                    tuple,
                    checks
                );
            }

            return new SavedCallSequence(
                first,
                live,
                inputs,
                lastSlotReads,
                steps,
                extraction,
                registerCase,
                tupleResult,
                constant,
                checks
            )
            { ProjectedCase = projectedCase, ProjectedReturn = projectedReturn };
        }

        private static int TupleCallTrim(List<int> lastSlotReads, int lastCall, PreparedCallClosure? closure)
        {
            // A closure prepared in X registers can release its captures before the call.
            bool trimFinal = closure is { OnStack: false } || lastSlotReads.Any(read => read > lastCall);
            int trimAt = lastCall > 0 && trimFinal ? lastCall : lastCall - 1;

            return Enumerable.Reverse(lastSlotReads).TakeWhile(read => read <= trimAt).Count();
        }

        private void EmitSavedCallSequence(
            SavedCallSequence plan,
            List<Action>? deferredFailures = null,
            (Expr.Match Match, Term? Literal, TaggedCallMatch? Tagged)? terminalMatch = null
        )
        {
            var (first, live, inputs, lastSlotReads, steps, extraction, decision, tuple, constant, checks) = plan;
            bool hasCase = decision is not null || plan.ProjectedCase is not null;
            bool hasTuple = tuple is not null || plan.ProjectedReturn is not null;
            int size = lastSlotReads.Count;
            EmitFrameAllocation(live, size, first.ConstructedArgument?.HeapWords ?? 0);
            EmitFrameInitialization(0, size - inputs.Count);
            for (int slot = inputs.Count - 1; slot >= 0; slot--)
            {
                if (inputs[slot] == extraction?.Tail)
                {
                    continue;
                }

                owner.MoveRegister(
                    XRegisterTag,
                    inputs[slot],
                    YRegisterTag,
                    size - 1 - slot
                );
            }

            if (extraction is { } list)
            {
                EmitCallListExtraction(list, -1, size - 1 - inputs.IndexOf(list.Tail));
            }

            EmitEntryCall(first);
            BeamSourceLocation previousCall = CallLocation(first.Call, location);
            const int pendingFailure = int.MinValue;
            var pendingOperands = new Dictionary<int, (int Start, int End)>();
            int? tupleFieldProjection = tuple?.Construction is { Constructions: [{ IsTuple: true, MapLive: null }] }
                && checks?.GetValueOrDefault(steps.Count) is { Tag: null, Field: int field }
                ? field : null;
            var failures = checks?.Where(check => !check.Value.ShapeKnown).OrderByDescending(check => check.Key)
                .ToDictionary(check => check.Key, _ => (Label: hasCase ? pendingFailure : Label(), Location: location));
            int trimmed = 0;
            (int Field, int Register, bool Saved)? deferredProjection = null;
            EmitCheck(0);
            for (int index = 0; index < steps.Count; index++)
            {
                SavedCallStep step = steps[index];
                int[] remap = Enumerable.Range(0, size).Select(slot => size - 1 - slot - trimmed).ToArray();
                if (step.Closure is { } closure)
                {
                    EmitPreparedCallClosure(closure, remap, step.Call.RegisterConstructions);
                }

                bool last = index == steps.Count - 1;
                bool finalCheck = last && (terminalMatch is not null || constant is not null && checks?.ContainsKey(index + 1) == true);
                bool deferConstant = last && (hasCase || finalCheck) && !step.ReuseSlot
                    && step.Call.RegisterConstructions is null && step.Call.Constants is [{ Destination: 0 }]
                    && step.Released.Any(slot => slot != step.Spill);
                bool tupleField = hasTuple && last;
                int trimTo = last && hasTuple ? TupleCallTrim(lastSlotReads, index + 1, step.Closure) : trimmed;
                while ((!last || finalCheck) && trimTo < size && lastSlotReads[size - 1 - trimTo] <= index + 1)
                {
                    trimTo++;
                }

                var finalTrim = last && checks is null
                    && tuple is { Closure: null, Live: 0 }
                    && step is { Spill: null, SlotRelocation: null, Closure: null, TupleArguments: null }
                    && step.Call.RegisterConstructions is null
                    ? PlanFinalTupleCallTrim(tuple, size - trimmed, trimTo - trimmed, step.Released.Select(slot => remap[slot]))
                    : null;
                int? deferredSlot = null;
                if (finalTrim is null && hasTuple && checks is null && step is
                    { Spill: null, ReloadRelocatedArgument: false, Closure: null, TupleArguments: null }
                    && step.Call.RegisterConstructions is null && trimTo > trimmed
                    && step.Call.SavedInputs is { } savedInputs)
                {
                    int firstLoad = savedInputs.FindIndex(move => move.Destination == 0);
                    if (firstLoad >= 0)
                    {
                        int slot = remap[savedInputs[firstLoad].Source];
                        if (slot >= trimTo - trimmed
                            && savedInputs[firstLoad].Source != step.SlotRelocation?.Destination)
                        {
                            deferredSlot = slot - (trimTo - trimmed);
                        }
                    }
                }

                if (step.TupleArguments is { } tupleArguments)
                {
                    foreach (var (slot, element, register) in tupleArguments)
                    {
                        owner.Op(GetTupleElement, YRegisterTag, remap[slot]);
                        owner.Operand(BeamWire.UnsignedTag, element);
                        owner.Operand(XRegisterTag, register);
                    }
                }

                EmitSavedCallArguments(
                    step,
                    remap,
                    deferConstant,
                    swapBeforeConstants: tupleField,
                    heapChecked: step.Closure is not null || deferredProjection.HasValue,
                    afterConstruction: deferredProjection is { } projection ? () => owner.EmitTuplePart(0, projection.Field, 0) : null,
                    deferStackLoad: step.ReloadRelocatedArgument || deferredSlot.HasValue
                );
                if (step.SlotRelocation is { } relocation)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        remap[relocation.Source],
                        YRegisterTag,
                        remap[relocation.Destination]
                    );
                }
                if (last && !hasCase && !hasTuple && constant is null && terminalMatch is null)
                {
                    EmitPreparedFrameTail(step.Call, previousCall, size - trimmed);
                    if (step.Call.Module is not null)
                    {
                        previousCall = CallLocation(step.Call.Call, previousCall);
                    }

                    EmitFailures();

                    return;
                }

                int[] expired = step.Released.Where(slot => slot != step.Spill && slot != step.SlotRelocation?.Destination
                    && remap[slot] >= trimTo - trimmed)
                    .Select(slot => remap[slot]).Order().ToArray();
                if (finalTrim is { } compacted)
                {
                    expired = compacted.Kills;
                    trimTo = trimmed + compacted.Count;
                    var construction = tuple!.Construction!;
                    tuple = tuple with { Construction = construction with
                    {
                        Constructions = [construction.Constructions[0] with { Operands = compacted.Operands }]
                    } };
                }

                if (expired.Length != 0)
                {
                    owner.EmitSlotKills(expired);
                }

                if (finalTrim is { } relocationPlan)
                {
                    foreach (var (source, destination) in relocationPlan.Moves)
                    {
                        owner.MoveRegister(YRegisterTag, source, YRegisterTag, destination);
                    }
                }

                if (trimTo > trimmed)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, trimTo - trimmed);
                    owner.Operand(BeamWire.UnsignedTag, size - trimTo);
                    trimmed = trimTo;
                }

                if (deferConstant)
                {
                    owner.EmitConstant(step.Call.Constants[0].Value, 0);
                }

                if (step.ReloadRelocatedArgument)
                {
                    Load(size - 1 - step.SlotRelocation!.Value.Destination - trimmed);
                }
                else if (deferredSlot is int finalSlot)
                {
                    Load(finalSlot);
                }

                owner.EmitCallLine(step.Call.Call, previousCall);
                Call(step.Call.Module, step.Call.Call.Function, step.Call.Call.Arguments.Count);
                previousCall = CallLocation(step.Call.Call, previousCall);
                EmitCheck(index + 1);
            }

            if (terminalMatch is { } match)
            {
                EmitTerminalCallMatch(
                    match.Match,
                    match.Literal,
                    match.Tagged,
                    size - trimmed,
                    previousCall
                );
            }
            else if (constant is not null)
            {
                owner.EmitConstant(constant, 0);
                owner.EmitFrameReturn(size - trimmed);
            }
            else if (tuple is not null)
            {
                EmitCallSequenceTuple(tuple, size - trimmed, tupleFieldProjection);
            }
            else if (plan.ProjectedReturn is { } returned)
            {
                EmitTupleFieldArm(returned, size - trimmed);
            }
            else if (decision is not null)
            {
                EntryCall selector = steps.Count == 0 ? first : steps[^1].Call;
                EmitRegisterCase(
                    decision,
                    size - trimmed,
                    previousCall,
                    RegisterCaseSelectorType(selector, decision.Opcode == IsTaggedTuple ? IsTuple : decision.Opcode)
                );
            }
            else if (plan.ProjectedCase is { } projected)
            {
                EmitTupleFieldCase(projected, size - trimmed);
            }

            EmitFailures();

            void EmitCheck(int index)
            {
                if (checks is null || !checks.TryGetValue(index, out var check))
                {
                    return;
                }

                int label = 0;
                if (!check.ShapeKnown)
                {
                    label = failures![index].Label;
                    failures[index] = (label, previousCall);
                }
                int start = owner.deferredLabelOperands.Count;
                deferredProjection = null;
                Action? beforeProjections = null;
                if (index < steps.Count && check is { Arity: not null, SavedTupleSlot: null, Field: null,
                        Projections: [.., { Register: 0, Saved: false } lastProjection] }
                    && check.Projections.All(field => !field.Saved)
                    && steps[index] is { Spill: null, Closure: null, TupleArguments: null,
                        Call.RegisterConstructions: { } construction }
                    && construction.Constructions.All(item => item.MapLive is null && item.Register != 0
                        && item.Operands.All(operand => operand.Tag != XRegisterTag || operand.Register != 0)))
                {
                    deferredProjection = lastProjection;
                    check = check with { Projections = check.Projections[..^1] };
                    beforeProjections = () =>
                    {
                        owner.Op(TestHeap, BeamWire.UnsignedTag, construction.Constructions.Sum(item => item.HeapWords));
                        owner.Operand(BeamWire.UnsignedTag, 1);
                    };
                }
                EmitCallResultCheck(
                    index == steps.Count && tupleFieldProjection is not null ? check with { Field = null } : check,
                    label,
                    size - trimmed,
                    beforeProjections
                );
                if (label == pendingFailure)
                {
                    pendingOperands.Add(index, (start, owner.deferredLabelOperands.Count));
                }
            }

            void EmitFailures()
            {
                if (failures is null)
                {
                    return;
                }

                foreach (var (index, failure) in failures)
                {
                    int label = failure.Label;
                    if (pendingOperands.TryGetValue(index, out var operands))
                    {
                        label = Label();
                        for (int operand = operands.Start; operand < operands.End; operand++)
                        {
                            owner.deferredLabelOperands[operand] = (owner.deferredLabelOperands[operand].Position, label);
                        }
                    }

                    var match = checks![index].Match;
                    if (deferredFailures is not null)
                    {
                        int failureLabel = label;
                        deferredFailures.Add(() => EmitTaggedCallMatchFailure(match, failureLabel, failure.Location, owner.lastEmittedLine));
                        continue;
                    }

                    EmitTaggedCallMatchFailure(match, label, failure.Location, previousCall);
                    previousCall = match.SourceLine == 0
                        ? failure.Location : new BeamSourceLocation(match.PreprocessorFile, match.SourceLine);
                }
            }
        }

        private static (int Count, int[] Kills, List<(int Source, int Destination)> Moves, ConstructionOperand[] Operands)?
            PlanFinalTupleCallTrim(CallSequenceTuple tuple, int frame, int prefix, IEnumerable<int> releases)
        {
            var killed = releases.ToHashSet();
            if (killed.Count < 2 || tuple.Construction is not
                { Constructions: [{ IsTuple: true, MapLive: null } result], Loads.Count: 0, Constants.Count: 0, Moves.Count: 0 })
            {
                return null;
            }

            var retained = result.Operands.Where(operand => operand.Literal is null && operand.Tag == YRegisterTag)
                .Select(operand => operand.Register + prefix).ToHashSet();
            var layout = retained.ToHashSet();
            var destinations = new HashSet<int>();
            var moves = new List<(int Source, int Destination)>();
            int count = 0;
            while (count < frame && !destinations.Contains(count))
            {
                if (layout.Contains(count))
                {
                    int destination = frame - 1;
                    while (destination > count && layout.Contains(destination))
                    {
                        destination--;
                    }

                    if (destination == count)
                    {
                        break;
                    }

                    moves.Add((count, destination));
                    layout.Add(destination);
                    destinations.Add(destination);
                }

                count++;
            }

            // A relocation earns its cost when there are enough released slots to remove.
            var kills = killed.Where(slot => slot >= count && !layout.Contains(slot)).Order().ToArray();
            if (moves.Count < 2 || count <= prefix || kills.Length + moves.Count >= killed.Count)
            {
                return null;
            }

            var targets = moves.ToDictionary(move => move.Source, move => move.Destination);
            var operands = result.Operands.Select(operand => operand.Literal is null && operand.Tag == YRegisterTag
                ? operand with { Register = targets.GetValueOrDefault(operand.Register + prefix, operand.Register + prefix) - count }
                : operand).ToArray();

            return (count, kills, moves, operands);
        }

        private int? FindReusableCallSlot(
            ref EntryCall step,
            string previous,
            IReadOnlyDictionary<string, int> saved,
            IEnumerable<int> released,
            bool allowSwapWithPreviousArgument = false,
            bool allowRegisterReload = false
        )
        {
            var call = step.Call;
            if (step.RegisterConstructions is not { } construction)
            {
                if (call.Arguments.Count >= FirstReservedXRegister)
                {
                    return null;
                }

                if (!call.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == previous))
                {
                    return released.Min(slot => (int?)slot);
                }

                // Other X0 operands are copied before swapping the dying slot into X0.
                if (step.SavedInputs is not { } loads)
                {
                    return null;
                }

                int? swap = allowSwapWithPreviousArgument
                    ? loads.Where(load => load.Destination == 0 && released.Contains(load.Source)).Min(load => (int?)load.Source)
                    : null;
                if (swap is not null || !allowRegisterReload)
                {
                    return swap;
                }

                // Only X0 carries a call result here. Read the dying slot into
                // its argument registers before saving X0 in the same slot.
                // If X0 must be loaded, the emitter stages that load in X[arity].
                return released.Where(slot => loads.All(load => load.Destination != 0 || load.Source != slot))
                    .Min(slot => (int?)slot);
            }

            // Stack loads run after the spill. Only constructor operands can read the old slot value.
            int? slot = released.Where(slot => construction.Loads.All(load => load.Source != slot)).Min(slot => (int?)slot);
            if (slot is not null && ReadStraightConstructedCall(
                call,
                previous,
                saved,
                preserveX0: true
            ) is { } preserved)
            {
                step = preserved;

                return slot;
            }

            return null;
        }

        private void EmitSavedCallArguments(
            SavedCallStep step,
            int[] remap,
            bool deferConstant = false,
            bool swapBeforeConstants = false,
            bool heapChecked = false,
            bool deferStackLoad = false,
            Action? afterConstruction = null
        )
        {
            var call = step.Call;
            var loads = call.SavedInputs?.Select(move => (move.Destination, Source: remap[move.Source])).ToList() ?? [];
            int spill = step.Spill is int slot ? remap[slot] : -1;
            if (spill >= 0 && !step.ReuseSlot && !deferStackLoad && !deferConstant
                && call.RegisterConstructions is null && call.Moves.Count == 0
                && call.Constants is [{ Destination: 2, Value: Nil }]
                && loads.OrderBy(move => move.Destination).ToArray() is
                    [{ Destination: 0 } first, { Destination: 1 } second]
                && first.Source != spill && second.Source != spill)
            {
                Load(first.Source, 2);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, spill);
                Load(second.Source, 1);
                owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
                owner.EmitConstant(Nil.Value, 2);

                return;
            }

            if (spill >= 0 && !step.ReuseSlot && !heapChecked && afterConstruction is null
                && call.RegisterConstructions is { Live: 0, Moves.Count: 0 } prefix
                && prefix.Constructions is [var firstCons, _, .., var lastCons]
                && firstCons.Register == 0 && lastCons.Register == 2
                && prefix.Loads is [{ Destination: 0 } parent]
                && prefix.Constants is [{ Destination: 1 } id, { Destination: 3 } options]
                && prefix.Constructions.Take(prefix.Constructions.Count - 1).All(cell => cell.Register == 0)
                && prefix.Constructions.All(cell => !cell.IsTuple && cell.MapLive is null
                    && cell.Operands is [{ Literal: Integer }, _])
                && firstCons.Operands[1].Tag == YRegisterTag
                && prefix.Constructions.Skip(1).All(cell => cell.Operands[1] is
                    { Tag: XRegisterTag, Register: 0, Literal: null }))
            {
                var cells = prefix.Constructions.Select((cell, index) => cell with
                {
                    Register = index == prefix.Constructions.Count - 1 ? lastCons.Register : 1,
                    Operands = cell.Operands.Select(operand => operand.Tag == XRegisterTag
                        ? operand with { Register = 1 } : operand).ToArray()
                }).ToList();
                EmitCallArgumentConstructions(
                    prefix with { Constructions = cells, Loads = [], Constants = [], Live = 1 },
                    -1,
                    remap,
                    spillSlot: spill
                );
                Load(remap[parent.Source], 1);
                owner.EmitConstant(options.Value, 3);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                owner.EmitConstant(id.Value, 1);

                return;
            }

            if (step.ReuseSlot && call.RegisterConstructions is { } beforeSpill)
            {
                EmitCallArgumentConstructions(
                    beforeSpill,
                    -1,
                    remap,
                    heapChecked: heapChecked,
                    spillSlot: spill
                );

                return;
            }

            if (step.ReuseSlot)
            {
                int scratch = call.Call.Arguments.Count;
                int firstLoad = loads.FindIndex(move => move.Destination == 0);
                if (firstLoad >= 0 && loads[firstLoad].Source == spill)
                {
                    loads.RemoveAt(firstLoad);
                    bool constantsAfterSwap = swapBeforeConstants
                        || call.Module == ErlangModuleNames.Module && call.Call.Function == ErlangOperators.Append
                            && call.Moves.Count == 0 && loads.Count == 0 && call.Constants is [{ Destination: 1 }];
                    owner.EmitOrderedTailMoves(call.Moves, constantsAfterSwap ? [] : call.Constants, loads);
                    owner.Op(Swap, YRegisterTag, spill);
                    owner.Operand(XRegisterTag, 0);
                    if (constantsAfterSwap)
                    {
                        owner.EmitIndependentCallMoves(call.Constants);
                    }

                    return;
                }

                if (firstLoad >= 0)
                {
                    if (call.Moves.Count == 0 && loads.Count == 2
                        && loads.Contains((1, spill))
                        && call.Constants is [{ Destination: 2, Value: Cons listArgument }])
                    {
                        Load(spill, 1);
                        Load(loads[firstLoad].Source, 2);
                        owner.MoveRegister(XRegisterTag, 0, YRegisterTag, spill);
                        owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
                        owner.EmitConstant(listArgument, 2);

                        return;
                    }

                    if (call.Moves is [(var destination, 0)] && call.Constants.Count == 0)
                    {
                        loads[firstLoad] = (destination, loads[firstLoad].Source);
                        var beforeSave = loads.Where(load => load.Source == spill || load.Destination == destination).ToList();
                        owner.EmitOrderedTailMoves([], [], beforeSave);
                        owner.MoveRegister(XRegisterTag, 0, YRegisterTag, spill);
                        loads.RemoveAll(load => load.Source == spill || load.Destination == destination);
                        owner.EmitOrderedTailMoves([], [], loads);
                        owner.Op(Swap, XRegisterTag, 0);
                        owner.Operand(XRegisterTag, destination);

                        return;
                    }

                    loads[firstLoad] = (scratch, loads[firstLoad].Source);
                    owner.EmitOrderedTailMoves(call.Moves, call.Constants, loads);
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        spill
                    );
                    owner.MoveRegister(
                        XRegisterTag,
                        scratch,
                        XRegisterTag,
                        0
                    );

                    return;
                }

                foreach (var (destination, source) in loads.Where(move => move.Source == spill).ToArray())
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        source,
                        XRegisterTag,
                        destination
                    );
                    loads.Remove((destination, source));
                }
            }

            if (spill >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    spill
                );
            }

            if (call.RegisterConstructions is { } construction)
            {
                EmitCallArgumentConstructions(
                    construction,
                    -1,
                    remap,
                    heapChecked,
                    afterConstruction: afterConstruction
                );
            }
            else if (spill >= 0 && !step.ReuseSlot && !deferStackLoad && !deferConstant
                && call.Moves.Count == 0 && loads is [{ Destination: 0 } single]
                && call.Constants is [{ Destination: 1, Value: Nil }])
            {
                Load(single.Source);
                owner.EmitConstant(Nil.Value, 1);
            }
            else if (spill >= 0 && !step.ReuseSlot && !deferStackLoad && !deferConstant
                && call.Moves.Count == 0 && loads is [{ Destination: 0 } receiver]
                && call.Constants is [{ Destination: 1, Value: Integer integer }, { Destination: 2, Value: Cons list }])
            {
                Load(receiver.Source, 1);
                owner.EmitConstant(list, 2);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                owner.EmitConstant(integer, 1);
            }
            else
            {
                owner.EmitOrderedTailMoves(call.Moves, deferConstant ? [] : call.Constants, loads,
                    deferX0StackLoad: deferStackLoad);
            }
        }
    }
}
