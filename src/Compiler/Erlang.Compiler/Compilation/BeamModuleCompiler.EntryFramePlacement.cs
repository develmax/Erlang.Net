using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? DelayEntryStackFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int allocationIndex = FindEntryFrameAllocation(body, start, end);
            var plan = allocationIndex < 0 ? null : PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end
            ) ?? PlanEntryFramePlacement(
                body,
                start,
                allocationIndex,
                end,
                extendEntry: true
            );
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
                            int width = CompactLiveWidth(original);
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

                    if (!placement.Delayed.Contains(index))
                    {
                        continue;
                    }

                    int next = body[index + 1].Offset;
                    if (placement.HeapLives.TryGetValue(index, out int heapLive))
                    {
                        int original = (int)instruction.Operands[^1].Value;
                        output.Write(code.Span[copied..(next - CompactLiveWidth(original))]);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heapLive);
                        copied = next;
                        continue;
                    }

                    if (instruction.Definition.Opcode == Deallocate)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = next;
                        continue;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, XRegisterTag, placement.Saved[slot]);
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
    }

    private static int FindEntryFrameAllocation(IReadOnlyList<BeamInstruction> body, int start, int end)
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

        int infoLabel = start - 2;
        while (infoLabel >= 0 && body[infoLabel].Definition.Opcode == Line)
        {
            infoLabel--;
        }

        int failure = infoLabel >= 0 && body[infoLabel].Definition.Opcode == Label
            ? (int)body[infoLabel].Operands[0].Value : -1;
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
            if (!(bif || opcode == Line || IsEntryFramePredicate(opcode))
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    || operand.Kind == BeamOperandParameters.Label && operand.Value != failure))
            {
                return -1;
            }
        }

        return -1;
    }

    private sealed record EntryFramePlacement(
        int First,
        Dictionary<int, int> Saved,
        HashSet<int> Delayed,
        IReadOnlyDictionary<int, int> Frames,
        IReadOnlyDictionary<int, int> HeapLives,
        IReadOnlyDictionary<int, (int Slot, int Register)[]> ProjectedStores
    );

    private static int CompactLiveWidth(int live) => live < 1 << BeamWire.CompactSmallPayloadShift ? 1 : 2;

    private static EntryFramePlacement? PlanEntryFramePlacement(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocationIndex,
        int end,
        bool extendEntry = false
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

        if (predicate == end || !IsDelayedFramePredicate(body[predicate].Definition.Opcode)
            || extendEntry && (allocationIndex != start + 1 || allocation.Definition.Opcode != Allocate
                || body[predicate].Definition.Opcode is not (IsTaggedTuple or IsTuple or IsNonemptyList)))
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

        var delayed = new HashSet<int>();
        var required = new HashSet<int>();
        var parents = new Dictionary<int, int> { [first] = -1 };
        var registerLimits = new Dictionary<int, (int Live, bool Projected, bool MaybeProjected)>
        {
            [first] = (live, false, false)
        };
        int projectedSlot = -1;
        int pairedSlot = -1;
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

            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            bool deallocates = index + 1 < end && IsEntryFrameReturn(index + 1);
            bool supported = IsDelayedFramePredicate(opcode)
                || opcode is Label or Line or Jump or BadMatch or BadRecord or CaseEnd or IfEnd or Return
                || opcode == Move && deallocates && instruction.Operands[1] is { Kind: BeamOperandParameters.X, Value.IsZero: true }
                || IsEntryFrameReturn(index);
            var (registers, projected, maybeProjected) = extendEntry
                ? registerLimits[index] : (live, false, false);
            if (extendEntry)
            {
                if (opcode is GetTupleElement or GetHead or GetTail
                    && (instruction.Operands[0].Kind != BeamOperandParameters.X || instruction.Operands[0].Value < registers))
                {
                    BeamOperand source = instruction.Operands[0];
                    BeamOperand destination = instruction.Operands[^1];
                    if (destination.Kind == BeamOperandParameters.X && destination.Value <= registers
                        && !saved.Values.Contains((int)destination.Value)
                        && (destination.Value < live || !maybeProjected))
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
                else if (instruction is { Definition.Opcode: GetList, Operands: [var listSource,
                    { Kind: BeamOperandParameters.Y } projectedHead, { Kind: BeamOperandParameters.Y } projectedTail] }
                    && registers == live && live + 2 <= BeamOperandParameters.MaximumRegister
                    && projectedHead.Value != projectedTail.Value
                    && (projectedSlot < 0 || projectedSlot == projectedHead.Value && pairedSlot == projectedTail.Value)
                    && !saved.ContainsKey((int)projectedHead.Value) && !saved.ContainsKey((int)projectedTail.Value)
                    && (listSource.Kind == BeamOperandParameters.X && listSource.Value < registers
                        || listSource.Kind == BeamOperandParameters.Y && saved.ContainsKey((int)listSource.Value)))
                {
                    // get_list defines both fields on the same edge, so both must survive a join.
                    supported = true;
                    projectedSlot = (int)projectedHead.Value;
                    pairedSlot = (int)projectedTail.Value;
                    projected = maybeProjected = true;
                }
                else if (instruction is { Definition.Opcode: GetList, Operands: [var list,
                    { Kind: BeamOperandParameters.X } head, { Kind: BeamOperandParameters.X } tail] }
                    && (list.Kind != BeamOperandParameters.X || list.Value < registers)
                    && head.Value != tail.Value
                    && !saved.Values.Contains((int)head.Value) && !saved.Values.Contains((int)tail.Value)
                    && (!maybeProjected || head.Value < live && tail.Value < live))
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

        if (!delayed.Contains(first) || extendEntry && !delayed.Any(index => body[index].Definition.Opcode == Return))
        {
            return null;
        }

        var stores = new Dictionary<int, (int Slot, int Register)[]>();
        if (required.Count == 0)
        {
            RecordProjections();
            return new(first, saved, delayed, new Dictionary<int, int>(), heapLives, stores);
        }

        if (extendEntry)
        {
            // A framed arm can share a tail with another framed arm, but must never enter an
            // unframed block or allocate the frame again at another entry.
            var framed = new HashSet<int>();
            var pending = new Queue<int>(required.SelectMany(Successors));
            while (pending.TryDequeue(out int reached))
            {
                if (reached < first || delayed.Contains(reached) || required.Contains(reached))
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
                    stores.Add(entry, pairedSlot < 0 ? [(projectedSlot, live)] : [(projectedSlot, live), (pairedSlot, live + 1)]);
                }
            }

            RecordProjections();
            return new(first, saved, delayed, frames, heapLives, stores);
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

            return new(first, saved, prefix, new Dictionary<int, int> { [site] = live }, heapLives, stores);
        }

        return null;

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

                        input = operand.Value == pairedSlot ? live + 1 : live;
                    }

                    registers = Math.Max(registers, input + 1);
                }
            }

            return registers;
        }

        bool IsEntryFrameReturn(int index) => index + 1 < end
            && body[index].Definition.Opcode == Deallocate && body[index].Operands[0].Value == frameSize
            && body[index + 1].Definition.Opcode == Return;

        int ProjectionCount() => pairedSlot < 0 ? 1 : 2;

        void RecordProjections()
        {
            if (projectedSlot >= 0)
            {
                saved.Add(projectedSlot, live);
            }

            if (pairedSlot >= 0)
            {
                saved.Add(pairedSlot, live + 1);
            }
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

        bool IsDelayedFramePredicate(byte opcode) => IsEntryFramePredicate(opcode)
            || (extendEntry || allocationIndex != start + 1) && opcode is SelectValue or SelectTupleArity;
    }

    private static bool IsEntryFramePredicate(byte opcode) => IsTrimTest(opcode)
        && opcode is not (GetMapElements or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree);
}
