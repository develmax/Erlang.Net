using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int UnseenFrameOwner = -2;
    private const int UnframedOwner = -1;

    private static byte[]? CompactDisjointFrames(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    ) => RewriteDisjointFrames(
        code,
        body,
        stackOperands,
        atomicOnly: false
    );

    private byte[]? CompactCompletelyTrimmedFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteDisjointFrames(
            code,
            decoded.Body,
            decoded.StackOperands,
            atomicOnly: false,
            completeTrimsOnly: true
        );
    }

    private static byte[]? RewriteDisjointFrames(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        bool atomicOnly,
        bool completeTrimsOnly = false,
        IReadOnlySet<int>? entriesToCompact = null
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (entriesToCompact is not null
                && (start >= end || body[start] is not { Definition.Opcode: Label, Operands: [var label] }
                    || !entriesToCompact.Contains((int)label.Value)))
            {
                continue;
            }

            if (PlanDisjointFramesCore(
                body,
                start,
                end,
                atomicOnly,
                completeTrimsOnly
            ) is not { } plans)
            {
                continue;
            }

            for (int index = start; index < end; index++)
            {
                if (plans[index - start] is not { } layout)
                {
                    continue;
                }

                var (size, slots) = layout;
                BeamInstruction instruction = body[index];
                int next = body[index + 1].Offset;
                byte opcode = instruction.Definition.Opcode;
                if (opcode == Move && instruction.Operands is
                    [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y } destination]
                    && slots[(int)destination.Value] < 0)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = next;
                    continue;
                }

                if (opcode == InitYRegisters)
                {
                    int[] kept = instruction.Operands.Skip(1).Select(slot => slots[(int)slot.Value])
                        .Where(slot => slot >= 0).Distinct().Order().ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (kept.Length != 0)
                    {
                        output.WriteByte(InitYRegisters);
                        output.WriteByte(ExtendedListOperand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, kept.Length);
                        foreach (int slot in kept)
                        {
                            BeamCompactWriter.Write(output, YRegisterTag, slot);
                        }
                    }

                    copied = next;
                    continue;
                }

                if (opcode == TrimFrame)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (size != 0)
                    {
                        output.WriteByte(TrimFrame);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, size);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                    }

                    copied = next;
                    continue;
                }

                int sizeOperand = LinearFrameSizeOperand(opcode);
                if (sizeOperand >= 0)
                {
                    int offset = instruction.Offset + 1;
                    var cursor = new BeamCursor(code[offset..]);
                    for (int position = 0; position < sizeOperand; position++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }

                    output.Write(code.Span[copied..(offset + cursor.Position)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, size);
                    _ = BeamCompactReader.Read(cursor);
                    copied = offset + cursor.Position;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                {
                    operand++;
                }

                while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                {
                    var (from, to, slot) = stackOperands[operand++];
                    if (slot != slots[slot])
                    {
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, YRegisterTag, slots[slot]);
                        copied = to;
                    }
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Size, int[] Slots)?[]? PlanDisjointFrames(IReadOnlyList<BeamInstruction> body, int start, int end) =>
        PlanDisjointFramesCore(
            body,
            start,
            end,
            atomicOnly: false
        );

    private static (int Size, int[] Slots)?[]? PlanDisjointFramesCore(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool atomicOnly,
        bool completeTrimsOnly = false
    )
    {
        if (completeTrimsOnly && !Enumerable.Range(start, end - start)
            .Any(index => body[index].Definition.Opcode == TrimFrame))
        {
            return null;
        }

        if (atomicOnly && (Enumerable.Range(start, end - start).Count(index =>
                body[index].Definition.Opcode is Allocate or AllocateHeap) < 2
            || !Enumerable.Range(start, end - start).Any(index => IsAtomicStackAliasCandidate(body[index]))))
        {
            return null;
        }

        if (ReadDisjointFrameOwnership(
            body,
            start,
            end,
            allowCompleteTrims: completeTrimsOnly
        ) is not { } ownership)
        {
            return null;
        }

        var (owners, uses) = ownership;
        var trimmedOwners = completeTrimsOnly
            ? Enumerable.Range(start, end - start)
                .Where(index => body[index].Definition.Opcode == TrimFrame)
                .Select(index => owners[index - start]).ToHashSet()
            : null;

        var regions = new Dictionary<int, (int End, int Count)>();
        if (!completeTrimsOnly && uses.Keys.Count(allocation => owners[allocation - start] != UnseenFrameOwner) > 1)
        {
            for (int index = 0; index < owners.Length; index++)
            {
                int owner = owners[index];
                int instruction = index + start;
                if (owner == UnframedOwner && index > 0 && body[instruction].Definition.Opcode == Return
                    && body[instruction - 1].Definition.Opcode == Deallocate)
                {
                    owner = owners[index - 1];
                }

                if (owner >= 0)
                {
                    regions[owner] = (instruction + 1, regions.GetValueOrDefault(owner).Count + 1);
                }
            }
        }

        var layouts = new Dictionary<int, (int Size, int[] Slots)>();
        foreach (var (allocation, used) in uses)
        {
            // Keep unreachable allocations intact.
            if (owners[allocation - start] == UnseenFrameOwner || trimmedOwners?.Contains(allocation) == false)
            {
                continue;
            }

            // The ownership walk rejects any edge joining two different frames.
            // Include the immediate Return after each Deallocate in this frame's liveness region.
            if (regions.TryGetValue(allocation, out var region) && region.Count == region.End - allocation - 1
                && (atomicOnly
                    ? PlanAtomicProjectionStackSlots(
                        body,
                        start,
                        region.End,
                        linearAllocation: allocation,
                        allowGcBifExceptionExit: true
                    )
                    : PlanBranchStackSlots(
                        body,
                        start,
                        region.End,
                        linearAllocation: allocation,
                        allowGcBifExceptionExit: true
                    )) is { Trim: null } plan)
            {
                layouts.Add(allocation, (plan.Count, plan.Slots));
                continue;
            }

            if (atomicOnly || used.All(slot => slot))
            {
                continue;
            }

            int size = 0;
            var slots = new int[used.Length];
            for (int slot = 0; slot < used.Length; slot++)
            {
                slots[slot] = used[slot] ? size++ : -1;
            }

            layouts.Add(allocation, (size, slots));
        }

        if (layouts.Count == 0)
        {
            return null;
        }

        var result = new (int Size, int[] Slots)?[owners.Length];
        for (int index = 0; index < owners.Length; index++)
        {
            int owner = owners[index];
            int allocation = owner == UnframedOwner && uses.ContainsKey(index + start) ? index + start : owner;
            if (layouts.TryGetValue(allocation, out var layout))
            {
                result[index] = layout;
            }
        }

        return result;
    }

    private static (int[] Owners, Dictionary<int, bool[]> Uses)? ReadDisjointFrameOwnership(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool allowCompleteTrims = false,
        int ignoredBranchTarget = -1
    )
    {
        if (start >= end || body[start].Definition.Opcode != Label)
        {
            return null;
        }

        var labels = new Dictionary<int, int>();
        var uses = new Dictionary<int, bool[]>();
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is CatchEnter or TryEnter || instruction.Definition.Name == "debug_line")
            {
                return null;
            }

            if (opcode == TrimFrame)
            {
                if (!allowCompleteTrims || !instruction.Operands[1].Value.IsZero)
                {
                    return null;
                }

                // The call frame still exists after the trim, with no Y slots.
                uses.Add(index, []);
            }
            else if (opcode == Label)
            {
                labels.Add((int)instruction.Operands[0].Value, index);
            }
            else if (opcode is Allocate or AllocateHeap)
            {
                int size = (int)instruction.Operands[0].Value;
                if (size is < 0 or > BeamOperandParameters.MaximumRegister)
                {
                    return null;
                }

                uses.Add(index, new bool[size]);
            }
        }

        if (uses.Count == 0)
        {
            return null;
        }

        int info = start - 2;
        while (info >= 0 && body[info].Definition.Opcode == Line)
        {
            info--;
        }

        int functionFailure = info >= 0 && body[info].Definition.Opcode == Label
            ? (int)body[info].Operands[0].Value : -1;
        var owners = new int[end - start];
        Array.Fill(owners, UnseenFrameOwner);
        var work = new Queue<int>();
        Assign(start, UnframedOwner);
        while (work.TryDequeue(out int index))
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            int frame = owners[index - start];
            int outgoing = frame;
            int sizeOperand = LinearFrameSizeOperand(opcode);
            if (opcode is Allocate or AllocateHeap)
            {
                if (frame != UnframedOwner)
                {
                    return null;
                }

                frame = outgoing = index;
            }
            else if (opcode == TrimFrame)
            {
                if (frame == UnframedOwner || instruction.Operands[0].Value != uses[frame].Length)
                {
                    return null;
                }

                outgoing = index;
            }
            else if (sizeOperand >= 0)
            {
                if (frame == UnframedOwner || instruction.Operands[sizeOperand].Value != uses[frame].Length)
                {
                    return null;
                }

                outgoing = UnframedOwner;
            }
            else if (opcode is (Return or TailLocalCall or TailExternalCall) && frame != UnframedOwner)
            {
                return null;
            }

            bool spill = opcode == Move && instruction.Operands[0].Kind == BeamOperandParameters.X;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (operand.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if (frame == UnframedOwner || operand.Value < 0 || operand.Value >= uses[frame].Length)
                {
                    return null;
                }

                if (opcode != InitYRegisters && !(spill && position == 1))
                {
                    uses[frame][(int)operand.Value] = true;
                }
            }

            if (opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame))
            {
                foreach (BeamOperand label in instruction.Operands.Where(operand => operand.Kind == BeamOperandParameters.Label))
                {
                    int target = (int)label.Value;
                    if (target != 0 && target != functionFailure && target != ignoredBranchTarget
                        && (!labels.TryGetValue(target, out int destination) || !Assign(destination, outgoing)))
                    {
                        return null;
                    }
                }
            }

            if (!EndsBeamBlock(opcode) && opcode != IfEnd
                && (index + 1 == end || !Assign(index + 1, outgoing)))
            {
                return null;
            }
        }

        return (owners, uses);

        bool Assign(int index, int frame)
        {
            int owner = owners[index - start];
            if (owner != UnseenFrameOwner)
            {
                return owner == frame;
            }

            owners[index - start] = frame;
            work.Enqueue(index);

            return true;
        }
    }
}
