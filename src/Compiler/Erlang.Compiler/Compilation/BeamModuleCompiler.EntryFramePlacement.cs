using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? DelayEntryStackFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        Func<BeamInstruction, bool> isFramelessExternalTail,
        Func<BeamInstruction, bool> isPublicEntry,
        bool closedListMatchesOnly = false
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        BeamProjectionFlow? flow = null;
        Dictionary<int, int>? terminalCallReturns = null;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int allocationIndex = FindEntryFrameAllocation(body, start, end);
            var plan = allocationIndex < 0 || closedListMatchesOnly ? null : PlanCheckedTagFallbackFrame(
                body,
                start,
                allocationIndex,
                end
            ) ?? PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end
            ) ?? PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end,
                extendEntry: true,
                valueDead: IsTemporaryDead
            ) ?? PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end,
                extendEntry: true,
                valueDead: IsTemporaryDead,
                allowFramelessTail: true,
                isFramelessExternalTail: isFramelessExternalTail
            ) ?? PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end,
                extendEntry: true,
                valueDead: IsTemporaryDead,
                allowLeadingProjection: true,
                isPendingFramelessTail: IsPendingFramelessTail
            );
            if (closedListMatchesOnly && allocationIndex >= 0)
            {
                plan = PlanRepeatedEntryListTails(
                    body,
                    start,
                    allocationIndex,
                    end
                );
            }

            // Exported arguments may arrive without the local record facts
            // that can eliminate a private callee's entry refinements.
            if (!closedListMatchesOnly && plan is null && start > 0 && isPublicEntry(body[start - 1])
                && PlanCheckedEntryProjectionAndTests(body, start, end) is { } projected)
            {
                allocationIndex = projected.Allocation;
                plan = projected.Placement;
            }

            if (!closedListMatchesOnly && plan is null
                && PlanNestedCheckedEntryProjections(body, start, end) is { } nested)
            {
                allocationIndex = nested.Allocation;
                plan = nested.Placement;
            }

            if (!closedListMatchesOnly && plan is null
                && PlanCheckedRecordSpillEntry(body, start, end) is { } recordSpill)
            {
                allocationIndex = recordSpill.Allocation;
                plan = recordSpill.Placement;
            }

            if (plan is { } placement)
            {
                int allocation = body[allocationIndex].Offset;
                int first = body[placement.First].Offset;
                ReadOnlySpan<byte> setup = code.Span[allocation..first];
                output.Write(code.Span[copied..allocation]);
                copied = first;
                for (int index = placement.First; index < end; index++)
                {
                    BeamInstruction instruction = body[index];
                    if (placement.Frames.TryGetValue(index, out int frameLive))
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        int original = (int)body[allocationIndex].Operands[^1].Value;
                        if (frameLive == original)
                        {
                            output.Write(setup);
                        }
                        else
                        {
                            int operandEnd = body[allocationIndex + 1].Offset;
                            int width = CompactRegisterCountWidth(original);
                            output.Write(code.Span[allocation..(operandEnd - width)]);
                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frameLive);
                            output.Write(code.Span[operandEnd..first]);
                        }

                        if (placement.ProjectedStores.TryGetValue(index, out var stores))
                        {
                            foreach (var (slot, register) in stores)
                            {
                                output.WriteByte(Move);
                                BeamCompactWriter.Write(output, XRegisterTag, register);
                                BeamCompactWriter.Write(output, YRegisterTag, slot);
                            }
                        }

                        copied = instruction.Offset;
                    }

                    int next = body[index + 1].Offset;
                    if (placement.Omitted?.Contains(index) == true)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = next;
                        continue;
                    }

                    if (!placement.Delayed.Contains(index))
                    {
                        continue;
                    }
                    if (placement.HeapLives.TryGetValue(index, out int heapLive))
                    {
                        int original = (int)instruction.Operands[^1].Value;
                        output.Write(code.Span[copied..(next - CompactRegisterCountWidth(original))]);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heapLive);
                        copied = next;
                        continue;
                    }

                    if (instruction.Definition.Opcode == TailLocalCallWithFrame)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        output.WriteByte(TailLocalCall);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, instruction.Operands[0].Value);
                        BeamCompactWriter.Write(output, LabelTag, instruction.Operands[1].Value);
                        copied = next;
                        continue;
                    }

                    if (instruction.Definition.Opcode == TailExternalCallWithFrame)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        output.WriteByte(TailExternalCall);
                        int frameWidth = CompactRegisterCountWidth((int)instruction.Operands[^1].Value);
                        output.Write(code.Span[(instruction.Offset + 1)..(next - frameWidth)]);
                        copied = next;
                        continue;
                    }

                    if (instruction.Definition.Opcode == Deallocate)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = next;
                        continue;
                    }

                    EntryFrameOperandRewrite? rewrite = placement.OperandRewrites?.GetValueOrDefault(index);
                    if (rewrite?.TupleProjectionDestination is int projection)
                    {
                        int oldWidth = CompactRegisterCountWidth((int)instruction.Operands[^1].Value);
                        output.Write(code.Span[copied..(next - oldWidth)]);
                        BeamCompactWriter.Write(output, XRegisterTag, projection);
                        copied = next;
                        continue;
                    }

                    if (rewrite?.FailureLabel is int retargeted)
                    {
                        int labelOffset = instruction.Offset + 1;
                        var cursor = new BeamCursor(code[labelOffset..]);
                        _ = BeamCompactReader.Read(cursor);
                        output.Write(code.Span[copied..labelOffset]);
                        BeamCompactWriter.Write(output, LabelTag, retargeted);
                        copied = labelOffset + cursor.Position;
                    }

                    int occurrence = 0;
                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        output.Write(code.Span[copied..from]);
                        int register = rewrite is null ? placement.Saved[slot] : rewrite.Registers[occurrence++];
                        BeamCompactWriter.Write(output, XRegisterTag, register);
                        copied = to;
                    }
                }
            }

            start = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsPendingFramelessTail(int index)
        {
            byte opcode = body[index].Definition.Opcode;
            if (opcode != LocalCall && !(opcode == ExternalCall && isFramelessExternalTail(body[index])))
            {
                return false;
            }

            terminalCallReturns ??= FindSharedCallReturns(body);

            return FindTerminalCallReturn(body, index, terminalCallReturns) >= 0;
        }

        bool IsTemporaryDead(int index, BeamOperand value) =>
            (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(index, value);
    }

    private static int FindEntryFrameAllocation(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool allowUnframedProjections = false
    )
    {
        if (end - start < 3 || body[start].Definition.Opcode != Label)
        {
            return -1;
        }

        int allocation = start + 1;
        if (body[allocation].Definition.Opcode is Allocate or AllocateHeap)
        {
            return allocation;
        }

        int failure = EntryFrameFailureLabel(body, start);
        for (; allocation < end; allocation++)
        {
            BeamInstruction instruction = body[allocation];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap)
            {
                return allocation;
            }

            bool bif = opcode is BifZero or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree;
            // Earlier branches may leave through func_info, but cannot bypass the moved frame.
            if (!(bif || opcode == Line || IsEntryFramePredicate(opcode)
                    || allowUnframedProjections && IsUnframedEntryProjection(instruction))
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    || operand.Kind == BeamOperandParameters.Label && operand.Value != failure))
            {
                return -1;
            }
        }

        return -1;
    }

    private static int EntryFrameFailureLabel(IReadOnlyList<BeamInstruction> body, int start)
    {
        int infoLabel = start - 2;
        while (infoLabel >= 0 && body[infoLabel].Definition.Opcode == Line)
        {
            infoLabel--;
        }

        return infoLabel >= 0 && body[infoLabel].Definition.Opcode == Label
            ? (int)body[infoLabel].Operands[0].Value
            : -1;
    }

    private static bool HasCheckedEntryProjectionPair(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocation,
        int projection,
        int end
    )
    {
        if (projection + 2 >= end
            || body[projection] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.X } source, var firstIndex, { Kind: BeamOperandParameters.Y } first]
            }
            || body[projection + 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var sameSource, var secondIndex, { Kind: BeamOperandParameters.Y } second]
            }
            || source != sameSource || source.Value >= body[allocation].Operands[^1].Value
            || first.Value == second.Value
            || first.Value >= body[allocation].Operands[0].Value
            || second.Value >= body[allocation].Operands[0].Value
            || firstIndex.Kind != BeamOperandParameters.Unsigned || secondIndex.Kind != BeamOperandParameters.Unsigned)
        {
            return false;
        }

        BeamInstruction next = body[projection + 2];
        if (!IsEntryFramePredicate(next.Definition.Opcode)
            || !next.Operands.Any(operand => SameProjectionRegister(operand, first)
                || SameProjectionRegister(operand, second)))
        {
            return false;
        }

        int failure = EntryFrameFailureLabel(body, start);
        if (failure < 0)
        {
            return false;
        }

        var prefix = body.Skip(start + 1).Take(allocation - start - 1)
            .Where(instruction => instruction.Definition.Opcode != Line).ToArray();
        BeamOperand? arity = prefix switch
        {
            [
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [var failed, var tuple, var count, _]
            }
            ] when ChecksInput(failed, tuple) => count,
            [
            {
                Definition.Opcode: IsTuple,
                Operands: [var firstFailure, var tuple]
            },
            {
                Definition.Opcode: TestArity,
                Operands: [var secondFailure, var sameTuple, var count]
            }
            ] when ChecksInput(firstFailure, tuple) && ChecksInput(secondFailure, sameTuple) => count,
            _ => null
        };

        return arity is { Kind: BeamOperandParameters.Unsigned }
            && firstIndex.Value.Sign >= 0 && secondIndex.Value.Sign >= 0
            && firstIndex.Value < arity.Value && secondIndex.Value < arity.Value;

        bool ChecksInput(BeamOperand failed, BeamOperand tuple) =>
            failed.Kind == BeamOperandParameters.Label && failed.Value == failure
            && SameProjectionRegister(tuple, source);
    }

    private sealed record EntryFramePlacement(
        int First,
        Dictionary<int, int> Saved,
        HashSet<int> Delayed,
        IReadOnlyDictionary<int, int> Frames,
        IReadOnlyDictionary<int, int> HeapLives,
        IReadOnlyDictionary<int, (int Slot, int Register)[]> ProjectedStores,
        IReadOnlyDictionary<int, EntryFrameOperandRewrite>? OperandRewrites = null,
        IReadOnlySet<int>? Omitted = null
    );

    private sealed record EntryFrameOperandRewrite(
        int[] Registers,
        int? FailureLabel = null,
        int? TupleProjectionDestination = null
    );

    private static int CompactRegisterCountWidth(int live) => live < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;

    private static EntryFramePlacement? PlanEntryFramePlacement(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocationIndex,
        int end,
        bool extendEntry = false,
        Func<int, BeamOperand, bool>? valueDead = null,
        bool allowFramelessTail = false,
        bool allowLeadingProjection = false,
        Func<BeamInstruction, bool>? isFramelessExternalTail = null,
        Func<int, bool>? isPendingFramelessTail = null
    )
    {
        BeamInstruction allocation = body[allocationIndex];
        int frameSize = (int)allocation.Operands[0].Value;
        int live = (int)allocation.Operands[^1].Value;
        var saved = new Dictionary<int, int>();
        int first = allocationIndex + 1;
        while (first < end)
        {
            BeamInstruction instruction = body[first];
            if (instruction is { Definition.Opcode: Move, Operands: [var source, var destination] }
                && source.Kind == BeamOperandParameters.X && source.Value < live
                && destination.Kind == BeamOperandParameters.Y)
            {
                saved[(int)destination.Value] = (int)source.Value;
            }
            else if (instruction.Definition.Opcode == InitYRegisters)
            {
                foreach (var slot in instruction.Operands.Skip(1))
                {
                    saved.Remove((int)slot.Value);
                }
            }
            else
            {
                break;
            }

            first++;
        }

        int predicate = first;
        while (predicate < end && body[predicate].Definition.Opcode == Line)
        {
            predicate++;
        }

        if (predicate == end)
        {
            return null;
        }

        byte firstOpcode = body[predicate].Definition.Opcode;
        bool leadingProjection = allowLeadingProjection && firstOpcode == GetTupleElement;
        bool checkedPair = leadingProjection && allocationIndex != start + 1 && HasCheckedEntryProjectionPair(
            body,
            start,
            allocationIndex,
            predicate,
            end
        );
        if ((allowLeadingProjection && !leadingProjection) || (!leadingProjection && !IsDelayedFramePredicate(firstOpcode))
            || extendEntry && ((allocationIndex != start + 1 && !checkedPair) || allocation.Definition.Opcode != Allocate
                || !allowFramelessTail && !leadingProjection
                    && firstOpcode is not (IsTaggedTuple or IsTuple or IsNonemptyList)))
        {
            return null;
        }

        if (leadingProjection && !checkedPair && (predicate + 1 >= end
            || !IsEntryFramePredicate(body[predicate + 1].Definition.Opcode)
            || !body[predicate + 1].Operands.Contains(body[predicate].Operands[^1])))
        {
            return null;
        }

        var labels = new Dictionary<int, int>();
        for (int index = start; index < end; index++)
        {
            if (body[index] is { Definition.Opcode: Label, Operands: [var label] })
            {
                labels.Add((int)label.Value, index);
            }
        }

        if (checkedPair && !HasBranchOwnedFrameCalls(predicate + 2))
        {
            return null;
        }

        int ownFailure = !extendEntry && allocation.Definition.Opcode == Allocate
            && body[start - 1] is { Definition.Opcode: FunctionInfo, Operands: [_, _, var functionArity] } && functionArity.Value <= live
            && body.Skip(start + 1).Take(allocationIndex - start - 1).All(instruction => instruction.Definition.Opcode == Line)
                ? EntryFrameFailureLabel(body, start) : -1;
        int terminalListMatchRelay = ReadTerminalListMatchRelay();
        var delayed = new HashSet<int>();
        var required = new HashSet<int>();
        var receiveHeaders = new HashSet<int>();
        var parents = new Dictionary<int, int> { [first] = -1 };
        var registerLimits = new Dictionary<int, (int Live, bool Projected, bool MaybeProjected)>
        {
            [first] = (live, false, false)
        };
        int projectedSlot = -1;
        int pairedSlot = -1;
        int pairedRegister = -1;
        int tupleProjectionEnd = -1;
        var heapLives = new Dictionary<int, int>();
        var constructors = new HashSet<int>();
        var work = new PriorityQueue<int, int>();
        int sequence = 0;
        work.Enqueue(first, sequence++);
        while (work.TryDequeue(out int index, out _))
        {
            if (index < first)
            {
                return null;
            }

            if (checkedPair && index > predicate + 1)
            {
                // The guard after the projections can still fail into its original framed path.
                required.Add(index);
                continue;
            }

            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            bool deallocates = index + 1 < end && IsEntryFrameReturn(index + 1);
            bool supported = IsDelayedFramePredicate(opcode)
                || opcode is Label or Line or Jump or BadMatch or BadRecord or CaseEnd or IfEnd or Return
                || index == terminalListMatchRelay
                || opcode == Move && deallocates && instruction.Operands[1] is { Kind: BeamOperandParameters.X, Value.IsZero: true }
                || IsEntryFrameReturn(index) || IsEntryFunctionClauseRelay(index);
            var (registers, projected, maybeProjected) = extendEntry
                ? registerLimits[index] : (live, false, false);
            if (allowFramelessTail)
            {
                // Keep a later list or tuple dispatch together when its paths may join again.
                supported = supported && opcode is not (IsNonemptyList or IsTuple or IsTaggedTuple)
                    || IsFramelessTail(instruction) && instruction.Operands[^1].Value == frameSize
                        && instruction.Operands[0].Value <= registers;
                if (opcode == TestHeap
                    && ReadExternalTailAfterConstructor(index + 1) is { } tail
                    && HeapRegisters(index + 1, projected: false) is int constructorLive)
                {
                    int arity = (int)tail.Operands[0].Value;
                    int heapLive = Math.Max(constructorLive, arity > 1 ? arity : 0);
                    if (heapLive <= registers)
                    {
                        supported = true;
                        constructors.Add(index + 1);
                        if (heapLive > instruction.Operands[^1].Value)
                        {
                            heapLives.Add(index, heapLive);
                        }
                    }
                }
                else
                {
                    supported |= constructors.Contains(index);
                }

                if (opcode == Label && index + 1 < end && body[index + 1].Definition.Opcode == LoopReceive
                    && HasOnlyReceiveRetryEdges(index))
                {
                    // The unframed edge falls through the allocation. Retry edges enter at the label.
                    receiveHeaders.Add(index);
                    supported = false;
                }
            }
            else if (extendEntry)
            {
                if (index == tupleProjectionEnd)
                {
                    supported = true;
                }
                else if (instruction is { Definition.Opcode: GetTupleElement, Operands: [var tupleSource, _, var firstField] }
                    && index + 1 < end
                    && body[index + 1] is { Definition.Opcode: GetTupleElement, Operands: [var nextTuple, _, var secondField] }
                    && tupleSource == nextTuple && registers == live
                    && TryProjectPair(
                        tupleSource,
                        firstField,
                        secondField,
                        index + 1
                    ))
                {
                    // The second extraction is reached only by fallthrough from the first.
                    tupleProjectionEnd = index + 1;
                    supported = true;
                    projected = maybeProjected = true;
                }
                else if (opcode is GetTupleElement or GetHead or GetTail
                    && (instruction.Operands[0].Kind != BeamOperandParameters.X || instruction.Operands[0].Value < registers))
                {
                    BeamOperand source = instruction.Operands[0];
                    BeamOperand destination = instruction.Operands[^1];
                    if (destination.Kind == BeamOperandParameters.X && destination.Value <= registers
                        && CanOverwriteX(destination, maybeProjected))
                    {
                        supported = true;
                        registers = Math.Max(registers, (int)destination.Value + 1);
                    }
                    else if (destination.Kind == BeamOperandParameters.Y && registers == live && pairedSlot < 0
                        && !saved.ContainsKey((int)destination.Value)
                        && (projectedSlot < 0 || destination.Value == projectedSlot)
                        && (source.Kind != BeamOperandParameters.Y
                            || saved.ContainsKey((int)source.Value) || projected && source.Value == projectedSlot))
                    {
                        supported = true;
                        projectedSlot = (int)destination.Value;
                        projected = maybeProjected = true;
                    }
                }
                else if (instruction is { Definition.Opcode: GetList, Operands: [var listSource, var projectedHead, var projectedTail] }
                    && registers == live
                    && TryProjectPair(
                        listSource,
                        projectedHead,
                        projectedTail,
                        index
                    ))
                {
                    supported = true;
                    projected = maybeProjected = true;
                }
                else if (instruction is
                {
                    Definition.Opcode: GetList, Operands: [var list,
                    { Kind: BeamOperandParameters.X } head, { Kind: BeamOperandParameters.X } tail]
                }
                    && (list.Kind != BeamOperandParameters.X || list.Value < registers)
                    && head.Value != tail.Value
                    && CanOverwriteX(head, maybeProjected) && CanOverwriteX(tail, maybeProjected))
                {
                    int firstWrite = Math.Min((int)head.Value, (int)tail.Value);
                    int lastWrite = Math.Max((int)head.Value, (int)tail.Value);
                    if (firstWrite <= registers && lastWrite <= registers + (firstWrite == registers ? 1 : 0))
                    {
                        supported = true;
                        registers = Math.Max(registers, lastWrite + 1);
                    }
                }
                else if (opcode == TestHeap && IsTerminalConstructor(index + 1)
                    && HeapRegisters(index + 1, projected) is int heapLive
                    && heapLive <= registers + (projected ? ProjectionCount() : 0))
                {
                    supported = true;
                    constructors.Add(index + 1);
                    if (heapLive > instruction.Operands[^1].Value)
                    {
                        heapLives.Add(index, heapLive);
                    }
                }
                else
                {
                    supported |= constructors.Contains(index);
                }
            }

            if (!supported || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    && !saved.ContainsKey((int)operand.Value)
                    && !(projected && (operand.Value == projectedSlot || operand.Value == pairedSlot))))
            {
                required.Add(index);
                continue;
            }

            delayed.Add(index);
            foreach (int next in Successors(index))
            {
                if (extendEntry)
                {
                    if (next <= index)
                    {
                        return null;
                    }

                    if (!registerLimits.TryAdd(next, (registers, projected, maybeProjected)))
                    {
                        var prior = registerLimits[next];
                        if (prior.Live != registers)
                        {
                            return null;
                        }

                        registerLimits[next] = (registers, projected && prior.Projected, maybeProjected || prior.MaybeProjected);
                    }
                }

                if (parents.TryAdd(next, index))
                {
                    work.Enqueue(next, extendEntry ? next : sequence++);
                }
            }
        }

        if (!delayed.Contains(first)
            || (checkedPair && tupleProjectionEnd != predicate + 1)
            || extendEntry && !(leadingProjection ? delayed.Contains(predicate)
                : delayed.Any(index => allowFramelessTail
                    ? IsFramelessTail(body[index]) : body[index].Definition.Opcode == Return)))
        {
            return null;
        }

        var stores = new Dictionary<int, (int Slot, int Register)[]>();
        if (required.Count == 0)
        {
            RecordProjections();

            return new(
                first,
                saved,
                delayed,
                new Dictionary<int, int>(),
                heapLives,
                stores
            );
        }

        if (extendEntry)
        {
            // A framed arm can share a tail with another framed arm, but must never enter an
            // unframed block or allocate the frame again at another entry.
            var framed = new HashSet<int>();
            var pending = new Queue<int>(required.SelectMany(Successors));
            while (pending.TryDequeue(out int reached))
            {
                if (reached < first || delayed.Contains(reached) || required.Contains(reached) && !receiveHeaders.Contains(reached))
                {
                    return null;
                }

                if (framed.Add(reached))
                {
                    foreach (int next in Successors(reached))
                    {
                        pending.Enqueue(next);
                    }
                }
            }

            var frames = new Dictionary<int, int>();
            foreach (int entry in required)
            {
                var state = registerLimits[entry];
                if (state.Projected != state.MaybeProjected)
                {
                    return null;
                }

                frames.Add(entry, state.Live + (state.Projected ? ProjectionCount() : 0));
                if (state.Projected)
                {
                    stores.Add(entry, pairedSlot < 0 ? [(projectedSlot, live)] : [(projectedSlot, live), (pairedSlot, pairedRegister)]);
                }
            }

            RecordProjections();

            return new(
                first,
                saved,
                delayed,
                frames,
                heapLives,
                stores
            );
        }

        // Every common dominator lies on the recorded path to any required instruction.
        for (int frame = required.First(); frame != first; frame = parents[frame])
        {
            HashSet<int>? prefix = required.Count == 1 ? delayed : CollectUnframedPrefix(frame);
            if (prefix is null || HasMixedFramePaths(frame, prefix))
            {
                if (required.Count == 1)
                {
                    return null;
                }

                continue;
            }

            // Branches enter through the allocation label. Keep trailing line information with its code.
            int site = frame;
            if (body[frame].Definition.Opcode == Label)
            {
                site++;
            }
            else
            {
                while (prefix.Contains(site - 1) && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }
            }

            return new(
                first,
                saved,
                prefix,
                new Dictionary<int, int> { [site] = live },
                heapLives,
                stores
            );
        }

        return null;

        bool IsEntryFunctionClauseRelay(int index)
        {
            if (ownFailure < 0 || index + 1 >= end || body[index].Definition.Opcode != Deallocate
                || body[index].Operands[0].Value != frameSize
                || body[index + 1] is not { Definition.Opcode: Jump, Operands: [var target] }
                || target.Kind != BeamOperandParameters.Label || target.Value != ownFailure)
            {
                return false;
            }

            int relay = index - 1;
            while (relay > first && body[relay].Definition.Opcode == Line)
            {
                relay--;
            }

            if (relay <= first || body[relay].Definition.Opcode != Label)
            {
                return false;
            }

            int testsEnd = first;
            while (testsEnd < relay && (body[testsEnd].Definition.Opcode == Line
                || IsEntryFramePredicate(body[testsEnd].Definition.Opcode)))
            {
                testsEnd++;
            }

            // Func_info reads the original arity's X registers. Only the straight-line,
            // read-only entry predicates may reach the relay. A framed edge must deallocate.
            bool hasEntry = false;
            for (int predecessor = start; predecessor < end; predecessor++)
            {
                if (!Successors(predecessor).Contains(relay))
                {
                    continue;
                }

                if (predecessor < first || predecessor >= testsEnd
                    || !IsEntryFramePredicate(body[predecessor].Definition.Opcode))
                {
                    return false;
                }

                hasEntry = true;
            }

            return hasEntry;
        }

        int ReadTerminalListMatchRelay()
        {
            if (body[predicate] is not { Definition.Opcode: IsNonemptyList, Operands: [var failure, var source] }
                || failure.Kind != BeamOperandParameters.Label
                || !labels.TryGetValue((int)failure.Value, out int failedArm)
                || failedArm <= predicate + 1)
            {
                return -1;
            }

            int move = SkipLines(failedArm + 1);
            if (move >= end || body[move] is not { Definition.Opcode: Move, Operands: [var input, var output] }
                || input != source
                || output is not { Kind: BeamOperandParameters.X, Value.IsZero: true })
            {
                return -1;
            }

            int terminal = SkipLines(move + 1);
            if (terminal >= end || body[terminal] is not { Definition.Opcode: BadMatch, Operands: [var rejected] }
                || rejected != output)
            {
                return -1;
            }

            // The checked input is available before allocation. Reject a second entry, including
            // fallthrough, that could have changed its X register or arrived with a live frame.
            for (int index = start; index < end; index++)
            {
                if (index != predicate && Successors(index).Contains(failedArm))
                {
                    return -1;
                }
            }

            return move;

            int SkipLines(int index)
            {
                while (index < end && body[index].Definition.Opcode == Line)
                {
                    index++;
                }

                return index;
            }
        }

        // Group the straight-line refinements that reject the same match arm.
        bool HasBranchOwnedFrameCalls(int consumer)
        {
            if (body[consumer].Operands[0] is not { Kind: BeamOperandParameters.Label } failure
                || !labels.TryGetValue((int)failure.Value, out int failedArm) || failedArm <= consumer + 1)
            {
                return false;
            }

            int refinementsEnd = consumer + 1;
            while (refinementsEnd < failedArm)
            {
                BeamInstruction test = body[refinementsEnd];
                byte opcode = test.Definition.Opcode;
                if (!(opcode is Line or Move or GetTupleElement or GetList or GetHead or GetTail
                    || IsEntryFramePredicate(opcode) && test.Operands[0] == failure))
                {
                    break;
                }

                refinementsEnd++;
            }

            HashSet<int> success = FrameCalls(consumer + 1, skipRefinementFailures: true);
            HashSet<int> failed = FrameCalls(failedArm, skipRefinementFailures: false);

            return success.Count + failed.Count > 0 && !success.Overlaps(failed);

            HashSet<int> FrameCalls(int entry, bool skipRefinementFailures)
            {
                var calls = new HashSet<int>();
                var seen = new HashSet<int>();
                var pending = new Stack<int>();
                pending.Push(entry);
                while (pending.TryPop(out int index))
                {
                    if (!seen.Add(index))
                    {
                        continue;
                    }

                    if (body[index].Definition.Opcode is LocalCall or ExternalCall or CallFun or CallFunTyped or DynamicApply
                        && isPendingFramelessTail?.Invoke(index) != true)
                    {
                        calls.Add(index);
                    }

                    foreach (int next in Successors(index))
                    {
                        if (skipRefinementFailures && index >= consumer + 1 && index < refinementsEnd && next == failedArm)
                        {
                            continue;
                        }

                        pending.Push(next);
                    }
                }

                return calls;
            }
        }



        HashSet<int>? CollectUnframedPrefix(int frame)
        {
            var seen = new HashSet<int>();
            var pending = new Queue<int>();
            pending.Enqueue(first);
            while (pending.TryDequeue(out int index))
            {
                if (index == frame || !seen.Add(index))
                {
                    continue;
                }

                if (required.Contains(index))
                {
                    return null;
                }

                foreach (int next in Successors(index))
                {
                    pending.Enqueue(next);
                }
            }

            return seen;
        }

        bool HasMixedFramePaths(int frame, HashSet<int> prefix)
        {
            var seen = new HashSet<int>();
            var pending = new Queue<int>(Successors(frame));
            while (pending.TryDequeue(out int index))
            {
                if (index < first || index == frame || prefix.Contains(index))
                {
                    return true;
                }

                if (seen.Add(index))
                {
                    foreach (int next in Successors(index))
                    {
                        pending.Enqueue(next);
                    }
                }
            }

            return false;
        }

        bool IsTerminalConstructor(int index) => index + 2 < end
            && body[index].Definition.Opcode is PutTuple or PutList
            && IsEntryFrameReturn(index + 1);

        BeamInstruction? ReadExternalTailAfterConstructor(int constructor)
        {
            if (constructor >= end || body[constructor].Definition.Opcode is not (PutTuple or PutList))
            {
                return null;
            }

            int next = constructor + 1;
            while (next < end && body[next].Definition.Opcode == Line)
            {
                next++;
            }

            return next < end && body[next].Definition.Opcode == TailExternalCallWithFrame
                && body[next].Operands[^1].Value == frameSize && IsFramelessTail(body[next])
                ? body[next] : null;
        }

        int? HeapRegisters(int constructor, bool projected)
        {
            BeamInstruction instruction = body[constructor];
            int resultIndex = instruction.Definition.Opcode == PutTuple ? 0 : instruction.Operands.Count - 1;
            if (instruction.Operands[resultIndex] is not { Kind: BeamOperandParameters.X, Value.IsZero: true })
            {
                return null;
            }

            int registers = 0;
            for (int index = 0; index < instruction.Operands.Count; index++)
            {
                if (index == resultIndex)
                {
                    continue;
                }

                BeamOperand operand = instruction.Operands[index];
                if (operand.Kind == BeamOperandParameters.X)
                {
                    registers = Math.Max(registers, (int)operand.Value + 1);
                }
                else if (operand.Kind == BeamOperandParameters.Y)
                {
                    if (!saved.TryGetValue((int)operand.Value, out int input))
                    {
                        if (!projected || operand.Value != projectedSlot && operand.Value != pairedSlot)
                        {
                            return null;
                        }

                        input = operand.Value == pairedSlot ? pairedRegister : live;
                    }

                    registers = Math.Max(registers, input + 1);
                }
            }

            return registers;
        }

        bool IsEntryFrameReturn(int index) => index + 1 < end
            && body[index].Definition.Opcode == Deallocate && body[index].Operands[0].Value == frameSize
            && body[index + 1].Definition.Opcode == Return;

        int ProjectionCount() => pairedRegister < live ? 1 : 2;

        bool TryProjectPair(
            BeamOperand source,
            BeamOperand firstField,
            BeamOperand secondField,
            int lastRead
        )
        {
            if (firstField.Kind != BeamOperandParameters.Y || secondField.Kind != BeamOperandParameters.Y
                || firstField.Value == secondField.Value || live + 2 > BeamOperandParameters.MaximumRegister
                || (projectedSlot >= 0 && (projectedSlot != firstField.Value || pairedSlot != secondField.Value))
                || saved.ContainsKey((int)firstField.Value) || saved.ContainsKey((int)secondField.Value)
                || !(source.Kind == BeamOperandParameters.X && source.Value < live
                    || source.Kind == BeamOperandParameters.Y && saved.ContainsKey((int)source.Value)))
            {
                return false;
            }

            int secondRegister = source is { Kind: BeamOperandParameters.X, TypeIndex: null }
                && !saved.Values.Contains((int)source.Value)
                && valueDead?.Invoke(lastRead, source) == true ? (int)source.Value : live + 1;
            if (pairedSlot >= 0 && pairedRegister != secondRegister)
            {
                return false;
            }

            projectedSlot = (int)firstField.Value;
            pairedSlot = (int)secondField.Value;
            pairedRegister = secondRegister;

            return true;
        }

        bool CanOverwriteX(BeamOperand target, bool maybeProjected) => !saved.Values.Contains((int)target.Value)
            && (!maybeProjected || target.Value < live && target.Value != pairedRegister);

        void RecordProjections()
        {
            if (projectedSlot >= 0)
            {
                saved.Add(projectedSlot, live);
            }

            if (pairedSlot >= 0)
            {
                saved.Add(pairedSlot, pairedRegister);
            }
        }

        bool HasOnlyReceiveRetryEdges(int header)
        {
            var label = body[header].Operands[0].Value;
            bool retries = false;
            for (int index = first; index < end; index++)
            {
                if (!body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                    && operand.Value == label))
                {
                    continue;
                }

                if (index <= header || body[index].Definition.Opcode is not (Wait or WaitTimeout or LoopReceiveEnd))
                {
                    return false;
                }

                retries = true;
            }

            return retries;
        }

        IEnumerable<int> Successors(int index)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame))
            {
                foreach (var operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Label && labels.TryGetValue((int)operand.Value, out int target))
                    {
                        yield return target;
                    }
                }
            }

            if (index + 1 < end && opcode != IfEnd && !EndsBeamBlock(opcode))
            {
                yield return index + 1;
            }
        }

        bool IsFramelessTail(BeamInstruction instruction) =>
            instruction.Definition.Opcode == TailLocalCallWithFrame
            || instruction.Definition.Opcode == TailExternalCallWithFrame
                && isFramelessExternalTail?.Invoke(instruction) == true;

        bool IsDelayedFramePredicate(byte opcode) => IsEntryFramePredicate(opcode)
            || (extendEntry || allocationIndex != start + 1) && opcode is SelectValue or SelectTupleArity;
    }

    private static bool IsEntryFramePredicate(byte opcode) => IsTrimTest(opcode)
        && opcode is not (GetMapElements or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree);
}
