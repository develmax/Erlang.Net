using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSiblingEntryCalls(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause) || clause.Body is not Expr.Call tail)
            {
                return false;
            }

            var children = new List<Expr.Call>();
            var resultNames = new List<string>();
            var tailArguments = tail.Arguments.Select(ReplaceCalls).ToArray();
            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (children.Count < 2 || ReadEntryCall(children[0], parameters) is not { } first)
            {
                return false;
            }

            string lastResult = resultNames[^1];
            string[] savedNames = [.. parameters, .. resultNames.Take(resultNames.Count - 1)];
            var finish = ReadEntryCall(tail with { Arguments = tailArguments }, [lastResult], savedNames);
            CallArgumentConstructions? constructions = null;
            int resultArgument = -1;
            if (finish is null)
            {
                var target = owner.ResolveImportedCall(tail, entries);
                var resolved = target with { Arguments = target.Arguments.Select(_ => (Expr)new Expr.Literal(Nil.Value)).ToArray() };
                if (!TryResolveOrdinaryCall(resolved, out string? module))
                {
                    return false;
                }

                var savedSlots = savedNames.Select((name, slot) => (name, slot))
                    .Where(binding => binding.name != VariableScopeNames.Wildcard)
                    .ToDictionary(binding => binding.name, binding => binding.slot, StringComparer.Ordinal);
                resultArgument = Array.FindIndex(tailArguments, argument => argument is Expr.Variable variable && variable.Name == lastResult);
                constructions = ReadCallArgumentConstructions(tailArguments, resultArgument, savedSlots,
                    registerValue: resultArgument < 0 ? lastResult : null);
                if (constructions is null)
                {
                    return false;
                }

                finish = new EntryCall(target, module, [], []);
            }

            if (children.Count == 2 && constructions is null
                && children[1].Arguments.Any(argument => argument is Expr.Tuple or Expr.List
                    && !AttributeLiteral.TryNormalizeConstant(argument, out _))
                && ReadSavedCallSequence(
                    [(children[0], resultNames[0]), (children[1], lastResult), (finish.Call, null)],
                    parameters,
                    extraction: null,
                    allowLinear: true
                ) is { Steps: [{ ReuseSlot: true, Call.RegisterConstructions: not null }, _] } constructedSequence)
            {
                EmitSavedCallSequence(constructedSequence);

                return true;
            }

            var finishInputs = finish.SavedInputs ?? [];
            var finishSources = constructions is null ? finishInputs.Select(move => move.Source)
                : constructions.Loads.Select(move => move.Source).Concat(constructions.Constructions
                    .SelectMany(construction => construction.Operands)
                    .Where(operand => operand.Tag == YRegisterTag).Select(operand => operand.Register));
            var remainingInputs = finishSources
                .Where(source => source < parameters.Length).ToHashSet();
            var steps = new EntryCall[children.Count - 1];
            var reuse = new (int Destination, int Source)?[steps.Length];
            int dedicatedSlots = 0;
            for (int index = steps.Length - 1; index >= 0; index--)
            {
                if (ReadEntryCall(children[index + 1], [], parameters) is not { } step)
                {
                    return false;
                }

                steps[index] = step;
                var inputs = step.SavedInputs ?? [];
                foreach (var input in inputs)
                {
                    if (!remainingInputs.Contains(input.Source) && inputs.Count(other => other.Source == input.Source) == 1)
                    {
                        reuse[index] = input;
                        break;
                    }
                }

                if (!reuse[index].HasValue)
                {
                    dedicatedSlots++;
                }

                remainingInputs.UnionWith(inputs.Select(move => move.Source));
            }

            int[] saved = remainingInputs.OrderDescending().ToArray();
            var slots = new int[parameters.Length + steps.Length];
            int frame = saved.Length + dedicatedSlots;
            int live = Math.Max(EntryCallLiveCount(first, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            EmitFrameAllocation(live, frame);
            EmitFrameInitialization(0, dedicatedSlots);
            for (int index = 0; index < saved.Length; index++)
            {
                int slot = index + dedicatedSlots;
                slots[saved[index]] = slot;
                owner.MoveRegister(XRegisterTag, saved[index], YRegisterTag, slot);
            }

            EmitEntryCall(first);
            var previous = CallLocation(first.Call, location);
            int nextDedicated = dedicatedSlots;
            for (int index = 0; index < steps.Length; index++)
            {
                EntryCall step = steps[index];
                int reusedSource = -1;
                int slot;
                if (reuse[index] is { } input)
                {
                    reusedSource = input.Source;
                    slot = slots[reusedSource];
                    if (input.Destination == 0)
                    {
                        owner.Op(Swap, YRegisterTag, slot);
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        owner.MoveRegister(YRegisterTag, slot, XRegisterTag, input.Destination);
                        owner.MoveRegister(XRegisterTag, 0, YRegisterTag, slot);
                    }
                }
                else
                {
                    slot = --nextDedicated;
                    owner.MoveRegister(XRegisterTag, 0, YRegisterTag, slot);
                }

                slots[parameters.Length + index] = slot;
                var stackMoves = step.SavedInputs?.Where(move => move.Source != reusedSource)
                    .Select(move => (move.Destination, slots[move.Source])).ToList();
                owner.EmitOrderedTailMoves(step.Moves, step.Constants, stackMoves);
                owner.EmitCallLine(step.Call, previous);
                Call(step.Module, step.Call.Function, step.Call.Arguments.Count);
                previous = CallLocation(step.Call, previous);
            }

            if (constructions is not null)
            {
                EmitCallArgumentConstructions(constructions, resultArgument, slots);
                previous = location;
            }
            else
            {
                var finalMoves = finishInputs.Select(move => (move.Destination, slots[move.Source])).ToList();
                owner.EmitOrderedTailMoves(finish.Moves, finish.Constants, finalMoves);
            }

            EmitPreparedFrameTail(finish, previous, frame);

            return true;

            Expr ReplaceCalls(Expr expression)
            {
                switch (expression)
                {
                    case Expr.Call call:
                        string name = FormattableString.Invariant($"\0sibling-result-{children.Count}");
                        children.Add(call);
                        resultNames.Add(name);
                        return new Expr.Variable(name);
                    case Expr.Tuple tuple:
                        return tuple with { Items = tuple.Items.Select(ReplaceCalls).ToArray() };
                    case Expr.List list:
                        return list with
                        {
                            Items = list.Items.Select(ReplaceCalls).ToArray(),
                            Tail = list.Tail is null ? null : ReplaceCalls(list.Tail)
                        };
                    default:
                        return expression;
                }
            }
        }

        private bool IsTermToBinaryCall(Expr.Call call) =>
            call is { DynamicRemote: false, Function: AutoImportNames.TermToBinary, Arguments.Count: 1 }
            && owner.ResolvesErlangBif(call);

        private bool TryEmitNestedEntryCalls(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if ((ReadEntryCallChain(clause.Body, parameters) ?? ReadStraightCallSequence(clause.Body, parameters))
                is not { } chain)
            {
                if (clause.Body is Expr.Call { DynamicRemote: false } tail
                    && tail.Arguments.Any(argument => argument is Expr.Binary { Operator: ErlangOperators.Append })
                    && ReadStraightCalls(FlattenCallArguments([tail]), parameters) is { Length: >= 3 } calls
                    && ReadSavedCallSequence(calls, parameters, extraction: null) is { } plan)
                {
                    EmitSavedCallSequence(plan);

                    return true;
                }

                return TryEmitSavedCallSequence(clause.Body, parameters);
            }

            EmitEntryCallChain(chain.First, parameters, chain.Continuation, chain.RelocateBeforeTrim);
            return true;
        }

        private bool TryEmitEntryCallWithComputedOperands(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Call { DynamicRemote: false } tail)
            {
                return false;
            }

            int firstIndex = tail.Arguments.TakeWhile(IsPreparedOperand).Count();
            if (firstIndex >= tail.Arguments.Count || tail.Arguments[firstIndex] is not Expr.Call first
                || tail.Arguments.Skip(firstIndex + 1).All(IsPreparedOperand))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(first, parameters) is not { } entry)
            {
                return false;
            }

            var target = owner.ResolveImportedCall(tail, entries);
            var resolved = target with { Arguments = target.Arguments.Select(_ => (Expr)new Expr.Literal(Nil.Value)).ToArray() };
            if (!TryResolveOrdinaryCall(resolved, out string? module))
            {
                return false;
            }

            var remainder = tail.Arguments.ToArray();
            remainder[firstIndex] = new Expr.Literal(Nil.Value);
            var continuation = clause with { Body = tail with { Arguments = remainder } };
            if (ReadUsedEntryParameters(continuation) is not { } survivors)
            {
                return false;
            }

            int[] saved = Enumerable.Range(0, parameters.Length).Reverse().Where(index => survivors.Contains(parameters[index])).ToArray();
            var savedSlots = saved.Select((input, slot) => (Name: parameters[input], Slot: slot))
                .ToDictionary(binding => binding.Name, binding => binding.Slot, StringComparer.Ordinal);
            var constructions = ReadCallArgumentConstructions(tail.Arguments, firstIndex, savedSlots);
            if (constructions is null
                && !first.Arguments.OfType<Expr.Variable>().Any(argument => !survivors.Contains(argument.Name)))
            {
                return false;
            }

            int live = Math.Max(EntryCallLiveCount(entry, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            if (constructions is not null)
            {
                EmitFrameAllocation(live, saved.Length);
                foreach (int input in saved)
                {
                    owner.MoveRegister(XRegisterTag, input, YRegisterTag, savedSlots[parameters[input]]);
                }

                EmitEntryCall(entry);
                EmitCallArgumentConstructions(constructions, firstIndex);
                EmitPreparedFrameTail(new EntryCall(target, module, [], []), location, saved.Length);
                return true;
            }

            EmitFrameAllocation(live, frameSize);
            EmitFrameInitialization(saved.Length, frameSize);
            foreach (int input in saved)
            {
                int slot = Slot();
                variables.Add(parameters[input], slot);
                owner.MoveRegister(XRegisterTag, input, YRegisterTag, slot);
            }

            var previous = LoadCallArguments(tail.Arguments, null, entryOperand: (firstIndex, entry));
            EmitPreparedFrameTail(new EntryCall(target, module, [], []), previous, frameSize);
            return true;
        }

        private sealed record EntryCallChain(
            EntryCall First,
            List<(EntryCall Call, int? ListDestination)> Continuation,
            bool RelocateBeforeTrim = false,
            CallListExtraction? ListExtraction = null
        );

        private EntryCallChain? ReadEntryCallChain(Expr body, string[] parameters, bool allowDiscardedPrefix = false)
        {
            if (body is Expr.Call call)
            {
                return ReadEntryCallChain(call, parameters);
            }

            if (allowDiscardedPrefix && body is Expr.Sequence { Items: [Expr.Call discarded, ..] } prefix
                && ReadEntryCall(discarded, parameters) is { } first)
            {
                int count = prefix.Items.TakeWhile(item => item is Expr.Call).Count();
                if (count < prefix.Items.Count
                    && ReadEntryCallChain(new Expr.Sequence(prefix.Items.Skip(count).ToArray()), parameters) is { } suffix
                    && ReadEntryCall(suffix.First.Call, [], parameters) is { } forwarded)
                {
                    var continuation = new List<(EntryCall Call, int? ListDestination)>(suffix.Continuation)
                    {
                        (forwarded, null)
                    };
                    for (int index = count - 1; index > 0; index--)
                    {
                        if (ReadEntryCall((Expr.Call)prefix.Items[index], [], parameters) is not { } step)
                        {
                            return null;
                        }

                        continuation.Add((step, null));
                    }

                    return new EntryCallChain(first, continuation, RelocateBeforeTrim: true);
                }
            }

            if (body is not Expr.Sequence { Items: [Expr.Match initial, .., Expr.Call tail] } sequence
                || !ReadBinding(initial, out var binding, out var nested))
            {
                return null;
            }

            var names = new HashSet<string>(parameters, StringComparer.Ordinal) { binding.Name };
            foreach (Expr expression in sequence.Items.Skip(1).SkipLast(1))
            {
                if (!ReadBinding(expression, out var nextBinding, out var nextCall) || !names.Add(nextBinding.Name)
                    || ForwardResult(nextCall, binding.Name, nested) is not { } next)
                {
                    return null;
                }

                binding = nextBinding;
                nested = next;
            }

            return ForwardResult(tail, binding.Name, nested) is { } final
                && ReadEntryCallChain(final, parameters) is { } chain
                ? chain with { RelocateBeforeTrim = sequence.Items.Count > 2 }
                : null;

            bool ReadBinding(Expr expression, out Pattern.Variable variable, out Expr.Call value)
            {
                if (expression is Expr.Match
                    { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } name, Value: Expr.Call producer }
                    && !parameters.Contains(name.Name, StringComparer.Ordinal))
                {
                    variable = name;
                    value = producer;
                    return true;
                }

                variable = null!;
                value = null!;
                return false;
            }

            static Expr.Call? ForwardResult(Expr.Call consumer, string name, Expr.Call result)
            {
                var arguments = consumer.Arguments.ToArray();
                int destination = -1;
                for (int index = 0; index < arguments.Length; index++)
                {
                    Expr argument = arguments[index];
                    if (argument is Expr.Variable variable && variable.Name == name
                        || argument is Expr.List { Items: [Expr.Variable member], Tail: null } && member.Name == name)
                    {
                        if (destination >= 0)
                        {
                            return null;
                        }

                        destination = index;
                        arguments[index] = argument is Expr.List list ? list with { Items = [result] } : result;
                    }
                    else if (argument is not Expr.Variable && !AttributeLiteral.TryNormalizeConstant(argument, out _))
                    {
                        return null;
                    }
                }

                return destination >= 0 ? consumer with { Arguments = arguments } : null;
            }
        }

        private EntryCallChain? ReadEntryCallChain(Expr.Call call, string[] parameters)
        {
            var continuation = new List<(EntryCall Call, int? ListDestination)>();
            const string result = "\0nested-call-result";
            while (true)
            {
                var arguments = call.Arguments.ToArray();
                int destination = Array.FindIndex(
                    arguments,
                    argument =>
                    argument is Expr.Call or Expr.List { Items: [Expr.Call], Tail: null }
                    || argument is Expr.Binary { Operator: ErlangOperators.Append } append
                        && !AttributeLiteral.TryNormalizeConstant(append, out _)
                        && (IsAppendOperand(append.Left) && IsAppendOperand(append.Right)
                            || append is { Left: Expr.Variable, Right: Expr.List { Items: [Expr.Variable], Tail: null } })
                );
                if (destination < 0)
                {
                    break;
                }

                int? listDestination = null;
                Expr.Call inner;
                if (arguments[destination] is Expr.List { Items: [Expr.Call item] })
                {
                    listDestination = destination;
                    inner = item;
                }
                else if (arguments[destination] is Expr.Binary append)
                {
                    inner = new Expr.Call(ErlangModuleNames.Module, ErlangOperators.Append, [append.Left, append.Right])
                    {
                        SourceLine = append.SourceLine,
                        PreprocessorFile = call.PreprocessorFile
                    };
                }
                else
                {
                    inner = (Expr.Call)arguments[destination];
                }

                arguments[destination] = new Expr.Variable(result);
                if (ReadEntryCall(call with { Arguments = arguments }, [result], parameters) is not { } step)
                {
                    return null;
                }

                if (listDestination.HasValue)
                {
                    step.Moves.Remove((destination, 0));
                }

                continuation.Add((step with { Call = call }, listDestination));
                call = inner;
            }

            if (continuation.Count == 0
                || (ReadEntryAppendCall(call, parameters) ?? ReadEntryCall(call, parameters, allowTermToBinary: true)) is not { } first)
            {
                return null;
            }

            return new(first, continuation);
        }

        private static bool IsAppendOperand(Expr operand) => operand is Expr.Call || IsPreparedOperand(operand);

        private static EntryCall? ReadEntryAppendCall(Expr.Call call, string[] parameters)
        {
            if (call is not { Module: ErlangModuleNames.Module, Function: ErlangOperators.Append, DynamicRemote: false,
                Arguments: [Expr.Variable left, Expr.List { Items: [Expr.Variable member], Tail: null } list] }
                || left.Name == VariableScopeNames.Wildcard || member.Name == VariableScopeNames.Wildcard
                || Array.IndexOf(parameters, left.Name) is not (>= 0 and var input)
                || ReadEntryConstructedArgument(list, 1, parameters) is not { } construction)
            {
                return null;
            }

            bool before = input != 0 && construction.Operands[0].Source != 0;
            // A move into X0 must happen after construction when X0 contains the list member.
            if (!before && input == 1)
            {
                return null;
            }

            return new(call, ErlangModuleNames.Module, input == 0 ? [] : [(0, input)], [],
                ConstructedArgument: construction, ConstructAfterMoves: before);
        }

        private bool TryEmitDiscardedCallSequence(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [Expr.Call or Expr.Match, _, ..] } sequence)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (TryEmitTaggedCallSequence(sequence, parameters))
            {
                return true;
            }

            if (sequence.Items[^1] is Expr.Tuple)
            {
                return TryEmitCallSequenceTuple(sequence, parameters);
            }

            bool returnsConstant = AttributeLiteral.TryNormalizeConstant(sequence.Items[^1], out Term constant);
            if (sequence.Items[0] is not Expr.Call firstCall || ReadEntryCall(firstCall, parameters) is not { } first)
            {
                if (returnsConstant
                    && ReadStraightCalls(FlattenCallArguments(sequence.Items.Take(sequence.Items.Count - 1)), parameters)
                        is { Length: > 0 } calls
                    && ReadSavedCallSequence(calls, parameters, extraction: null, constant: constant, allowLinear: true)
                        is { } plan)
                {
                    EmitSavedCallSequence(plan);

                    return true;
                }

                return false;
            }

            var continuation = new List<(EntryCall Call, int? ListDestination)>();
            for (int index = sequence.Items.Count - (returnsConstant ? 2 : 1); index > 0; index--)
            {
                if (sequence.Items[index] is not Expr.Call call
                    || ReadEntryCall(call, [], parameters) is not { } step)
                {
                    return false;
                }

                continuation.Add((step, null));
            }

            Term? constantResult = returnsConstant ? constant : null;
            if (constantResult is not null && continuation.Count != 0
                && ReturnsLocalCallConstant(continuation[0].Call, constantResult))
            {
                constantResult = null;
            }

            EmitEntryCallChain(first, parameters, continuation, relocateBeforeTrim: true,
                constantResult: constantResult);
            return true;
        }

        private bool TryEmitCallSequenceTuple(Expr.Sequence sequence, string[] parameters)
        {
            if (sequence.Items[^1] is not Expr.Tuple tuple)
            {
                return false;
            }

            var inputRegisters = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int register = 0; register < parameters.Length; register++)
            {
                if (parameters[register] != VariableScopeNames.Wildcard)
                {
                    inputRegisters.Add(parameters[register], register);
                }
            }

            if (!TryReadBoundTupleOperands(tuple, null, inputRegisters, out var operands, requireResult: false))
            {
                return false;
            }

            var names = new HashSet<string>(inputRegisters.Keys, StringComparer.Ordinal);
            string? previous = null;
            var steps = new List<(EntryCall Call, int? ListDestination)>();
            for (int index = 0; index < sequence.Items.Count - 1; index++)
            {
                Expr item = sequence.Items[index];
                string? binding = null;
                if (item is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.Call value })
                {
                    if (variable.Name != VariableScopeNames.Wildcard)
                    {
                        if (!names.Add(variable.Name))
                        {
                            return false;
                        }

                        binding = variable.Name;
                    }

                    item = value;
                }

                if (item is not Expr.Call call)
                {
                    return false;
                }

                EntryCall? step = index == 0
                    ? ReadEntryCall(call, parameters)
                    : ReadEntryCall(call, previous is null ? [] : [previous], parameters, allowFunctionArgument: true);
                if (step is null)
                {
                    return false;
                }

                steps.Add((step, null));
                previous = binding;
            }

            EntryCall first = steps[0].Call;
            steps.RemoveAt(0);
            steps.Reverse();
            EmitEntryCallChain(first, parameters, steps, relocateBeforeTrim: true, tupleResult: operands);

            return true;
        }

        private void EmitEntryCallChain(
            EntryCall first,
            string[] parameters,
            List<(EntryCall Call, int? ListDestination)> continuation,
            bool relocateBeforeTrim = false,
            (int? Slot, Term? Constant)[]? tupleResult = null,
            (TaggedCallMatch Check, int? Field, bool KeepPayload)? taggedMatch = null,
            CallListExtraction? extraction = null,
            int? entryTupleArity = null,
            Term? constantResult = null
        )
        {
            var lastUses = new Dictionary<int, int>();
            if (tupleResult is not null)
            {
                foreach (var operand in tupleResult)
                {
                    if (operand.Slot is int source)
                    {
                        lastUses.TryAdd(source, -1);
                    }
                }
            }

            for (int index = 0; index < continuation.Count; index++)
            {
                EntryCall step = continuation[index].Call;
                foreach (var (_, source) in step.SavedInputs ?? [])
                {
                    lastUses.TryAdd(source, index);
                }

                if (step.RegisterConstructions is { } constructions)
                {
                    foreach (var (_, source) in constructions.Loads)
                    {
                        lastUses.TryAdd(source, index);
                    }

                    foreach (var operand in constructions.Constructions.SelectMany(item => item.Operands)
                        .Where(operand => operand.Tag == YRegisterTag))
                    {
                        lastUses.TryAdd(operand.Register, index);
                    }
                }
            }

            int payloadSource = taggedMatch is { KeepPayload: true } ? parameters.Length : -1;
            IOrderedEnumerable<int> byLastUse = lastUses.Keys.OrderByDescending(source => source == payloadSource)
                .ThenByDescending(source => relocateBeforeTrim ? 0 : lastUses[source]);
            // OTP numbers plain fun parameters by binding name before reversing the Y registers.
            int[] saved = (relocateBeforeTrim && recursiveClosure is { CaptureCount: 0 }
                ? byLastUse.ThenByDescending(source => source == payloadSource ? "" : parameters[source], StringComparer.Ordinal)
                : byLastUse.ThenByDescending(source => source)).ToArray();
            int live = extraction is { } list
                ? ListSequenceLive(first, parameters, saved, list)
                : Math.Max(EntryCallLiveCount(first, parameters), saved.Where(source => source != payloadSource).DefaultIfEmpty(-1).Max() + 1);
            if (first.ConstructedArgument is { } construction)
            {
                live = Math.Max(live, construction.Operands.Max(operand => operand.Source.GetValueOrDefault(-1)) + 1);
            }

            EmitFrameAllocation(entryTupleArity is null ? live : 1, saved.Length, first.ConstructedArgument?.HeapWords ?? 0);
            var slots = new int[parameters.Length + (payloadSource >= 0 ? 1 : 0)];
            if (entryTupleArity is int tupleArity)
            {
                for (int slot = 0; slot < saved.Length; slot++)
                {
                    slots[saved[slot]] = slot;
                }

                var firstUses = first.Call.Arguments.OfType<Expr.Variable>().Select(argument => argument.Name)
                    .ToHashSet(StringComparer.Ordinal);
                for (int field = 0; field < tupleArity; field++)
                {
                    int register = field + 1;
                    if (firstUses.Contains(parameters[register]))
                    {
                        owner.EmitTuplePart(0, field, register);
                        if (lastUses.ContainsKey(register))
                        {
                            owner.MoveRegister(XRegisterTag, register, YRegisterTag, slots[register]);
                        }
                    }
                    else if (lastUses.ContainsKey(register))
                    {
                        owner.Op(GetTupleElement, XRegisterTag, 0);
                        owner.Operand(BeamWire.UnsignedTag, field);
                        owner.Operand(YRegisterTag, slots[register]);
                    }
                }
            }
            for (int slot = 0; slot < saved.Length; slot++)
            {
                slots[saved[slot]] = slot;
                if (saved[slot] == payloadSource)
                {
                    EmitFrameInitialization(slot, slot + 1);
                    continue;
                }

                if (entryTupleArity is null && saved[slot] != extraction?.Head && saved[slot] != extraction?.Tail)
                {
                    owner.MoveRegister(XRegisterTag, saved[slot], YRegisterTag, slot);
                }
            }

            BeamSourceLocation previous = location;
            if (extraction is { } deferred)
            {
                EmitCallListExtraction(deferred, Array.IndexOf(saved, deferred.Head), slots[deferred.Tail]);
                var loads = first.SavedInputs?.Select(move => (move.Destination, slots[move.Source])).ToList();
                owner.EmitOrderedTailMoves(first.Moves, first.Constants, loads);
                owner.EmitCallLine(first.Call, previous);
                Call(first.Module, first.Call.Function, first.Call.Arguments.Count);
                previous = CallLocation(first.Call, location);
            }
            else if (first.Module == ErlangModuleNames.Module && IsTermToBinaryCall(first.Call))
            {
                owner.EmitOrderedTailMoves(first.Moves, first.Constants);
                Call(first.Module, first.Call.Function, 1);
            }
            else
            {
                EmitEntryCall(first);
                previous = CallLocation(first.Call, location);
            }

            int failure = 0;
            var checkedAt = previous;
            bool deferredPayload = false;
            if (taggedMatch is { } tagged)
            {
                failure = Label();
                deferredPayload = payloadSource >= 0 && continuation[^1].Call.SavedInputs?.Any(move => move.Source == payloadSource) != true;
                if (deferredPayload)
                {
                    owner.MoveRegister(XRegisterTag, 0, YRegisterTag, slots[payloadSource]);
                }

                EmitDetailRecordTest(0, tagged.Check.Arity, tagged.Check.Tag, failure);
                if (!deferredPayload && tagged.Field is int field)
                {
                    if (payloadSource >= 0)
                    {
                        EmitPayloadField(XRegisterTag, 0, YRegisterTag, slots[payloadSource], field);
                    }
                    else
                    {
                        owner.EmitTuplePart(0, field, 0);
                    }
                }
            }

            int trimmed = 0;
            for (int index = continuation.Count - 1; index >= 0; index--)
            {
                var (step, listDestination) = continuation[index];
                if (listDestination is int destination)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.Op(PutList, XRegisterTag, 0);
                    owner.LiteralOperand(Nil.Value);
                    owner.Operand(XRegisterTag, destination);
                }

                var stackMoves = step.SavedInputs?.Select(move => (move.Destination, slots[move.Source] - trimmed)).ToList();
                var moves = step.Moves;
                if (deferredPayload && step.SavedInputs is { } loads)
                {
                    var payloadLoads = loads.Where(move => move.Source == payloadSource).ToArray();
                    if (payloadLoads.Length != 0)
                    {
                        int slot = slots[payloadSource] - trimmed;
                        int field = taggedMatch!.Value.Field!.Value;
                        if (lastUses[payloadSource] == index && payloadLoads is [var use]
                            && moves.All(move => move.Source != use.Destination))
                        {
                            EmitPayloadField(YRegisterTag, slot, XRegisterTag, use.Destination, field);
                            stackMoves!.Remove((use.Destination, slot));
                        }
                        else
                        {
                            EmitPayloadField(YRegisterTag, slot, YRegisterTag, slot, field);
                        }

                        deferredPayload = false;
                    }
                }
                if (step.FunctionArgument is { } function)
                {
                    int register = function.KeepsResult && function.Destination == 0 ? 1 : function.Destination;
                    owner.EmitClosureAllocation(0, function.KeepsResult ? 1 : 0, 1);
                    if (register != function.Destination && moves is [(1, 0)]
                        && step.Constants.Count == 0 && stackMoves is null
                        && ReturnsUnconstrainedParameter(function.Reference))
                    {
                        // OTP can fold the swap into the closure destination without a fun result annotation.
                        owner.MoveRegister(XRegisterTag, 0, XRegisterTag, register);
                        owner.EmitDeclaredFunctionReference(function.Reference, function.Entry, function.Destination);
                        moves = [];
                    }
                    else
                    {
                        owner.EmitDeclaredFunctionReference(function.Reference, function.Entry, register);
                        if (register != function.Destination)
                        {
                            moves = [.. moves, (function.Destination, register)];
                        }
                    }
                }

                bool deferPayloadArgument = index != 0 && payloadSource >= 0 && lastUses[payloadSource] < index
                    && step.SavedInputs?.Contains((0, payloadSource)) == true
                    && saved.Skip(trimmed).Any(source => lastUses[source] == index);
                if (step.RegisterConstructions is { } constructions)
                {
                    int[] currentSlots = slots.Select(slot => slot - trimmed).ToArray();
                    EmitCallArgumentConstructions(constructions, -1, currentSlots);
                }
                else
                {
                    owner.EmitOrderedTailMoves(
                        moves,
                        step.Constants,
                        stackMoves,
                        deferX0StackLoad: deferPayloadArgument
                    );
                }
                if (index == 0 && tupleResult is null && constantResult is null)
                {
                    EmitPreparedFrameTail(step, previous, saved.Length - trimmed);
                    if (step.Module is not null)
                    {
                        previous = CallLocation(step.Call, previous);
                    }

                    break;
                }

                int next = trimmed;
                while (next < saved.Length && lastUses[saved[next]] == index)
                {
                    next++;
                }

                if (relocateBeforeTrim)
                {
                    int hole = saved.Length - 1;
                    while (next < saved.Length)
                    {
                        if (lastUses[saved[next]] != index)
                        {
                            while (hole > next && lastUses[saved[hole]] != index)
                            {
                                hole--;
                            }

                            if (hole <= next)
                            {
                                break;
                            }

                            owner.MoveRegister(YRegisterTag, next - trimmed, YRegisterTag, hole - trimmed);
                            saved[hole] = saved[next];
                            slots[saved[next]] = hole;
                            hole--;
                        }

                        next++;
                    }
                }

                if (next > trimmed)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, next - trimmed);
                    owner.Operand(BeamWire.UnsignedTag, saved.Length - next);
                    trimmed = next;
                }

                if (deferPayloadArgument)
                {
                    owner.MoveRegister(YRegisterTag, slots[payloadSource] - trimmed, XRegisterTag, 0);
                }

                owner.EmitCallLine(step.Call, previous);
                Call(step.Module, step.Call.Function, step.Call.Arguments.Count);
                previous = CallLocation(step.Call, previous);
            }

            if (tupleResult is not null)
            {
                if (tupleResult.All(operand => operand.Constant is not null))
                {
                    owner.EmitConstant(new TupleTerm(tupleResult.Select(operand => operand.Constant!).ToArray()), 0);
                }
                else
                {
                    var operands = tupleResult.Select(operand => operand.Slot is int source
                        ? (Slot: (int?)(slots[source] - trimmed), operand.Constant)
                        : operand).ToArray();
                    EmitTupleWithX0(operands, usesX0: false);
                }


                owner.EmitFrameReturn(saved.Length - trimmed);
            }

            if (constantResult is not null)
            {
                owner.EmitConstant(constantResult, 0);
                owner.EmitFrameReturn(saved.Length - trimmed);
            }

            if (taggedMatch is { } check)
            {
                EmitTaggedCallMatchFailure(check.Check.Match, failure, checkedAt, previous);
            }

            void EmitPayloadField(int sourceTag, int source, int destinationTag, int destination, int field)
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(destinationTag, destination);
            }
        }

        private bool ReturnsUnconstrainedParameter(Expr.FunctionReference reference)
        {
            if (!localFunctions.TryGetValue((reference.Name, reference.Arity), out var function)
                || function.Clauses is not [{ Guard: null } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr result = clause.Body is Expr.Sequence sequence ? sequence.Items[^1] : clause.Body;
            return result is Expr.Variable variable
                && clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == variable.Name);
        }

        private bool TryEmitNestedArgumentTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            Expr.Call? call = clause.Body as Expr.Call;
            Expr.Tuple? root = call?.Arguments.LastOrDefault() as Expr.Tuple;
            if (clause.Body is Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Tuple constructed }, Expr.Call tail]
                }
                && binding.Name != VariableScopeNames.Wildcard && !parameters.Contains(binding.Name)
                && tail.Arguments.LastOrDefault() is Expr.Variable forwarded && binding.Name == forwarded.Name)
            {
                call = tail;
                root = constructed;
            }

            if (root is null || call is null || call.DynamicRemote
                || parameters.Contains(VariableScopeNames.Wildcard))
            {
                return false;
            }

            Term? receiver = null;
            int arity = call.Arguments.Count;
            string? module;
            if (arity == 1)
            {
                if (call is { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Exit }
                    && owner.ResolvesErlangBif(call))
                {
                    module = ErlangModuleNames.Module;
                }
                else if (!TryResolveOrdinaryCall(call, out module, preparedArgument: 0))
                {
                    return false;
                }
            }
            else if (call is { Module: not (null or ErlangModuleNames.Module), Arguments: [var first, _] }
                && AttributeLiteral.TryNormalizeConstant(first, out Term constantReceiver))
            {
                module = call.Module;
                receiver = constantReceiver;
            }
            else
            {
                return false;
            }

            var uses = new List<int>(new int[Math.Max(function.Arity, arity)]);
            if (!CountUses(root))
            {
                return false;
            }

            var steps = new List<(bool IsTuple, int Target, (Term? Constant, int Register)[] Operands)>();
            ReadOperand(root, destination: arity - 1);
            if (steps.Count == 0)
            {
                return false;
            }

            int heap = steps.Sum(step => step.IsTuple ? step.Operands.Length + 1 : ConsHeapWords);
            owner.Op(TestHeap, BeamWire.UnsignedTag, heap);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            foreach (var (isTuple, target, operands) in steps)
            {
                if (isTuple)
                {
                    owner.Op(PutTuple, XRegisterTag, target);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, operands.Length);
                }
                else
                {
                    owner.instructions.WriteByte(PutList);
                }

                foreach (var (constant, register) in operands)
                {
                    if (constant is null)
                    {
                        owner.Operand(XRegisterTag, register);
                    }
                    else
                    {
                        owner.LiteralOperand(constant);
                    }
                }

                if (!isTuple)
                {
                    owner.Operand(XRegisterTag, target);
                }
            }

            if (receiver is not null)
            {
                owner.EmitConstant(receiver, 0);
            }

            if (module is null)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entries[(call.Function, arity)]);
            }
            else
            {
                owner.EmitCallLine(call, location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, call.Function, arity));
            }

            return true;

            bool CountUses(Expr expression)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out _))
                {
                    return true;
                }

                if (expression is Expr.Variable variable && Array.IndexOf(parameters, variable.Name) is int register
                    && register >= 0)
                {
                    uses[register]++;
                    return true;
                }

                return expression switch
                {
                    Expr.Tuple tuple => tuple.Items.All(CountUses),
                    Expr.List { Items: [var head], Tail: null } => CountUses(head),
                    _ => false
                };
            }

            (Term? Constant, int Register) ReadOperand(Expr expression, int? destination = null)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    return (constant, -1);
                }

                if (expression is Expr.Variable variable)
                {
                    return (null, Array.IndexOf(parameters, variable.Name));
                }

                bool isTuple = expression is Expr.Tuple;
                var children = isTuple ? ((Expr.Tuple)expression).Items : ((Expr.List)expression).Items;
                var operands = children.Select(child => ReadOperand(child)).ToArray();
                foreach (var (value, register) in operands)
                {
                    if (value is null)
                    {
                        uses[register]--;
                    }
                }

                int target = destination ?? uses.IndexOf(0);
                if (target < 0)
                {
                    target = uses.Count;
                    uses.Add(0);
                }

                uses[target] = 1;
                steps.Add((isTuple, target, isTuple ? operands : [operands[0], (Nil.Value, -1)]));
                return (null, target);
            }
        }
    }
}
