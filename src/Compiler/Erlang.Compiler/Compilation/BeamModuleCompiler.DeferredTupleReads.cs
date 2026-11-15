using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record DeferredTupleRead(
        int Definition,
        int Use,
        BeamOperand Field,
        BeamOperand Slot,
        BeamOperand? Destination = null
    );

    private static byte[]? SinkTupleReadsAcrossCalls(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            if (PlanDeferredTupleReads(body, entry, end, ref flow) is { } plan)
            {
                var (tuple, capture, reads) = plan;
                foreach (DeferredTupleRead read in reads)
                {
                    BeamInstruction projection = body[read.Definition];
                    output.Write(code.Span[copied..projection.Offset]);
                    if (capture is not null && read.Slot == tuple)
                    {
                        output.WriteByte(Move);
                        WriteRegister(output, capture);
                        WriteRegister(output, tuple);
                    }
                    else if (!IsDeferredSlotAlreadyCleared(body, read.Definition, read.Slot))
                    {
                        WriteFrameInitialization(output, [(int)read.Slot.Value]);
                    }

                    copied = body[read.Definition + 1].Offset;
                }

                foreach (var group in reads.GroupBy(read => read.Use).OrderBy(group => group.Key))
                {
                    BeamInstruction consumer = body[group.Key];
                    int next = body[group.Key + 1].Offset;
                    output.Write(code.Span[copied..consumer.Offset]);
                    foreach (DeferredTupleRead read in group)
                    {
                        output.WriteByte(GetTupleElement);
                        WriteRegister(output, tuple);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, read.Field.Value);
                        WriteRegister(output, read.Destination!);
                    }

                    copied = consumer.Offset;
                    if (consumer.Definition.Opcode == Move)
                    {
                        copied = next;
                        continue;
                    }

                    int operand = stackOperands.BinarySearch((copied, 0, 0),
                        Comparer<(int Start, int End, int Slot)>.Create(static (a, b) => a.Start.CompareTo(b.Start)));
                    if (operand < 0)
                    {
                        operand = ~operand;
                    }

                    for (; operand < stackOperands.Count && stackOperands[operand].Start < next; operand++)
                    {
                        var (from, to, slot) = stackOperands[operand];
                        if (group.FirstOrDefault(read => read.Slot.Value == slot) is { } read)
                        {
                            output.Write(code.Span[copied..from]);
                            WriteRegister(output, read.Destination!);
                            copied = to;
                        }
                    }
                }
            }

            entry = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }

    private static (BeamOperand Tuple, BeamOperand? Capture, List<DeferredTupleRead> Reads)? PlanDeferredTupleReads(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int end,
        ref BeamProjectionFlow? flow
    )
    {
        var references = new Dictionary<int, List<int>>();
        int allocations = 0;
        bool renumbersSlots = false;
        for (int index = entry; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is TryEnter or CatchEnter || instruction.Definition.Name == "debug_line")
            {
                return null;
            }

            renumbersSlots |= opcode == TrimFrame;
            if (opcode is Allocate or AllocateHeap)
            {
                allocations++;
            }

            if (opcode != InitYRegisters)
            {
                foreach (BeamOperand operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.Y)
                    {
                        if (!references.TryGetValue((int)operand.Value, out List<int>? sites))
                        {
                            sites = [];
                            references.Add((int)operand.Value, sites);
                        }

                        sites.Add(index);
                    }
                }
            }
        }

        if (allocations != 1)
        {
            return null;
        }

        for (int call = entry + 1; call + 1 < end; call++)
        {
            if (body[call].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                continue;
            }

            int last = call + 1;
            while (last < end && (IsDeferredTupleReadNeighbor(body[last].Definition.Opcode)
                || body[last].Definition.Opcode is LocalCall or ExternalCall))
            {
                last++;
            }

            var candidates = new Dictionary<BeamOperand, List<DeferredTupleRead>>();
            for (int definition = call - 1; definition >= entry
                && IsDeferredTupleReadNeighbor(body[definition].Definition.Opcode); definition--)
            {
                if (body[definition] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [
                            { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } tuple,
                            var field,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                        ]
                    }
                    || SameProjectionRegister(tuple, slot)
                    || !references.TryGetValue((int)slot.Value, out List<int>? sites)
                    || FindDeferredTupleUse(
                        body,
                        sites,
                        definition,
                        call,
                        last,
                        entry,
                        end,
                        slot,
                        renumbersSlots,
                        ref flow
                    ) is not int use)
                {
                    continue;
                }

                if (!candidates.TryGetValue(tuple, out List<DeferredTupleRead>? fields))
                {
                    fields = [];
                    candidates.Add(tuple, fields);
                }

                fields.Add(new(definition, use, field, slot));
            }

            foreach (var (source, reads) in candidates)
            {
                reads.Reverse();
                bool capture = source.Kind == BeamOperandParameters.X;
                if (capture && (reads.Count < 2 || reads[^1].Definition - reads[0].Definition != reads.Count - 1))
                {
                    continue;
                }

                BeamOperand tupleSlot = capture ? reads[0].Slot : source;
                int first = reads[0].Definition;
                int lastUse = reads.Max(read => read.Use);
                int lastCall = lastUse - 1;
                while (lastCall > call && body[lastCall].Definition.Opcode is not (LocalCall or ExternalCall))
                {
                    lastCall--;
                }

                bool extendsPreviousProof = lastCall != call || renumbersSlots
                    || references[(int)reads[0].Slot.Value].Count != 2;
                if (!capture && reads.Count == 1 && extendsPreviousProof
                    && (flow ??= new BeamProjectionFlow(body)).IsDeferredSlotDeadAfterUse(lastCall, entry, end, source))
                {
                    // Keep one extracted field across a call when the tuple itself would otherwise die.
                    continue;
                }

                bool stable = true;
                for (int index = first; index <= lastUse; index++)
                {
                    BeamInstruction instruction = body[index];
                    if (WritesDeferredTupleSlot(instruction, tupleSlot)
                        && !(capture && index == first)
                        || instruction.Definition.Opcode == InitYRegisters
                        && reads.Any(read => read.Definition < index && read.Use >= index
                            && instruction.Operands.Any(operand => SameProjectionRegister(operand, read.Slot))))
                    {
                        stable = false;
                        break;
                    }
                }

                if (stable && AssignDeferredTupleDestinations(body, reads, ref flow))
                {
                    return (tupleSlot, capture ? source : null, reads);
                }
            }
        }

        return null;
    }

    private static bool IsDeferredSlotAlreadyCleared(
        IReadOnlyList<BeamInstruction> body,
        int definition,
        BeamOperand slot
    )
    {
        for (int index = definition - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Label or FunctionInfo or Allocate or AllocateHeap or Deallocate or TrimFrame
                || EndsBeamBlock(opcode))
            {
                return false;
            }

            if (instruction.Operands.Any(operand => SameProjectionRegister(operand, slot)))
            {
                return opcode == InitYRegisters;
            }
        }

        return false;
    }

    private static int? FindDeferredTupleUse(
        IReadOnlyList<BeamInstruction> body,
        List<int> sites,
        int definition,
        int call,
        int last,
        int entry,
        int end,
        BeamOperand slot,
        bool renumbersSlots,
        ref BeamProjectionFlow? flow
    )
    {
        int next = sites.BinarySearch(definition) + 1;
        if (next >= sites.Count)
        {
            return null;
        }

        int use = sites[next];
        if (use <= call || use >= last || !IsDeferredTupleConsumer(body[use], slot))
        {
            return null;
        }

        bool laterReference = next + 1 < sites.Count;
        if (laterReference && sites[next + 1] == use
            || (renumbersSlots || laterReference)
                && !(flow ??= new BeamProjectionFlow(body)).IsDeferredSlotDeadAfterUse(use, entry, end, slot))
        {
            return null;
        }

        return use;
    }

    private static bool IsDeferredTupleReadNeighbor(byte opcode) =>
        opcode is Line or Move or GetTupleElement or TestHeap or PutTuple or PutList or InitYRegisters;

    private static bool IsDeferredTupleConsumer(BeamInstruction instruction, BeamOperand slot) =>
        instruction is
        {
            Definition.Opcode: Move,
            Operands: [var input, { Kind: BeamOperandParameters.X, TypeIndex: null }]
        } && SameProjectionRegister(input, slot)
        || instruction.Definition.Opcode is PutTuple or PutList
        && !SameProjectionRegister(instruction.Operands[StackWriteOperand(instruction)], slot);

    private static bool WritesDeferredTupleSlot(BeamInstruction instruction, BeamOperand slot)
    {
        int destination = StackWriteOperand(instruction);
        return destination >= 0 && SameProjectionRegister(instruction.Operands[destination], slot)
            || instruction.Definition.Opcode == InitYRegisters
                && instruction.Operands.Any(operand => SameProjectionRegister(operand, slot));
    }

    private static bool AssignDeferredTupleDestinations(
        IReadOnlyList<BeamInstruction> body,
        List<DeferredTupleRead> reads,
        ref BeamProjectionFlow? flow
    )
    {
        foreach (var group in Enumerable.Range(0, reads.Count).GroupBy(index => reads[index].Use))
        {
            BeamInstruction consumer = body[group.Key];
            if (consumer.Definition.Opcode == Move)
            {
                int index = group.Single();
                reads[index] = reads[index] with { Destination = consumer.Operands[1] };
                continue;
            }

            int destination = StackWriteOperand(consumer);
            var occupied = consumer.Operands.Where((operand, index) =>
                operand.Kind == BeamOperandParameters.X && index != destination)
                .Select(operand => (int)operand.Value).ToHashSet();
            int limit = Math.Min(BeamOperandParameters.MaximumRegister, occupied.DefaultIfEmpty(0).Max() + reads.Count + 2);
            int register = 0;
            foreach (int index in group)
            {
                BeamOperand scratch;
                while (true)
                {
                    if (register >= limit)
                    {
                        return false;
                    }

                    scratch = new BeamOperand(BeamOperandParameters.X, register++);
                    if (!occupied.Contains((int)scratch.Value)
                        && (SameProjectionRegister(consumer.Operands[destination], scratch)
                            || (flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(group.Key, scratch)))
                    {
                        break;
                    }
                }

                reads[index] = reads[index] with { Destination = scratch };
            }
        }

        return true;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool IsDeferredSlotDeadAfterUse(int use, int entry, int end, BeamOperand slot)
        {
            var visited = new HashSet<(int Index, BeamOperand Slot)>();
            var pending = new Stack<(int Index, BeamOperand Slot)>(
                successors[use].Select(index => (index, slot)));
            while (pending.TryPop(out var state))
            {
                int index = state.Index;
                if (index < entry || index >= end || !visited.Add(state))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                BeamOperand current = state.Slot;
                if (opcode == TrimFrame)
                {
                    if (current.Value < instruction.Operands[0].Value)
                    {
                        continue;
                    }

                    current = current with { Value = current.Value - instruction.Operands[0].Value };
                }

                int destination = StackWriteOperand(instruction);
                bool overwrites = false;
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    if (!SameProjectionRegister(instruction.Operands[position], current))
                    {
                        continue;
                    }

                    if (opcode == InitYRegisters || position == destination || opcode == GetList && position == 2)
                    {
                        overwrites = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                // A branch on failure can still observe the old slot.
                if (opcode is Deallocate or IfEnd || overwrites
                    && instruction.Operands.All(operand => operand.Kind != BeamOperandParameters.Label))
                {
                    continue;
                }

                foreach (int target in successors[index])
                {
                    if (!overwrites || target != index + 1
                        || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                            && body[target].Definition.Opcode == Label
                            && operand.Value == body[target].Operands[0].Value))
                    {
                        pending.Push((target, current));
                    }
                }
            }

            return true;
        }
    }
}
