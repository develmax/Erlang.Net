using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepReturnedTupleInputInX(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutTuple)
            || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int frame = FindLinearFrameSuffix(body, start, end);
            if (frame < 0 || frame + 1 >= end || body[frame] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value: var width }, _]
                }
                || width < 1 || width > BeamOperandParameters.MaximumRegister
                || width.IsOne && !IsSingleReturnedTupleSlotClear(body[frame + 1])
                || PlanReturnedTupleSlot(
                    body,
                    start,
                    frame,
                    end,
                    (int)width
                ) is not { } plan)
            {
                continue;
            }

            int slot = plan.Slot;
            int reduced = (int)width - 1;
            int nextSpill = 0;
            int stack = FirstStackOperandAtOrAfter(stackOperands, body[frame].Offset);
            for (int index = frame; index < end; index++)
            {
                var instruction = body[index];
                if (nextSpill < plan.Spills.Count && plan.Spills[nextSpill] == index)
                {
                    var heap = body[index + 1];
                    output.Write(code.Span[copied..instruction.Offset]);
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        heap.Offset,
                        heap.Offset,
                        1,
                        1
                    );
                    index++;
                    nextSpill++;

                    continue;
                }

                if (LinearFrameSizeOperand(instruction.Definition.Opcode) is >= 0 and var size)
                {
                    copied = WritePostCallProjectionOperand(
                        output,
                        code,
                        instruction.Offset,
                        copied,
                        size,
                        reduced
                    );

                    continue;
                }

                int next = body[index + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    int[] clears = instruction.Operands.Skip(1)
                        .Where(operand => operand.Value != slot)
                        .Select(operand => operand.Value > slot ? (int)operand.Value - 1 : (int)operand.Value)
                        .ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (clears.Length != 0)
                    {
                        WriteFrameInitialization(output, clears);
                    }

                    copied = next;

                    continue;
                }

                while (stack < stackOperands.Count && stackOperands[stack].Start < instruction.Offset)
                {
                    stack++;
                }

                while (stack < stackOperands.Count && stackOperands[stack].Start < next)
                {
                    var (from, to, register) = stackOperands[stack++];
                    if (register < slot)
                    {
                        continue;
                    }

                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(
                        output,
                        register == slot ? XRegisterTag : YRegisterTag,
                        register == slot ? 0 : register - 1
                    );
                    copied = to;
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

    private static (int Slot, List<int> Spills)? PlanReturnedTupleSlot(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int frame,
        int end,
        int width
    )
    {
        var spills = new Dictionary<int, List<int>>();
        var retained = new bool[width];
        for (int index = frame + 1; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode is Allocate or AllocateHeap or TrimFrame or DebugLine or ExecutableLine)
            {
                return null;
            }

            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            BeamOperand? local = null;
            if (index + 2 < end && ReadReturnedTupleSpill(body, index) is { } scratch)
            {
                if (scratch.Value < 0 || scratch.Value >= width)
                {
                    return null;
                }

                int slot = (int)scratch.Value;
                if (!spills.TryGetValue(slot, out var writes))
                {
                    writes = [];
                    spills.Add(slot, writes);
                }

                writes.Add(index);
                local = scratch;
                index += 2;
                instruction = body[index];
            }

            foreach (var operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand != local)
                {
                    if (operand.Value < 0 || operand.Value >= width)
                    {
                        return null;
                    }

                    retained[(int)operand.Value] = true;
                }
            }
        }

        int removable = spills.Keys.Where(slot => !retained[slot]).DefaultIfEmpty(-1).Min();
        if (removable < 0 || ReadDisjointFrameOwnership(body, start, end) is not { } owned
            || owned.Uses.Count != 1 || !owned.Uses.ContainsKey(frame))
        {
            return null;
        }

        // Keep the producer's X0 rooted through the adjacent heap check.
        // Other live Y values keep their order and the continuation stays owned.
        return owned.Owners[frame - start] == UnframedOwner
            && spills[removable].All(index => owned.Owners[index - start] == frame)
            && Enumerable.Range(frame + 1, end - frame - 1).All(index =>
                (body[index].Definition.Opcode != InitYRegisters
                    && LinearFrameSizeOperand(body[index].Definition.Opcode) < 0)
                || owned.Owners[index - start] == frame)
            ? (removable, spills[removable])
            : null;
    }

    private static BeamOperand? ReadReturnedTupleSpill(IReadOnlyList<BeamInstruction> body, int index) =>
        body[index] is
        {
            Definition.Opcode: Move,
            Operands: [var input, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
        }
        && IsBareX0(input)
        && body[index + 1] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
        && body[index + 2] is { Definition.Opcode: PutTuple, Operands: [var output, _, ..] } tuple
        && IsBareX0(output) && tuple.Operands.Skip(2).Contains(slot)
        && tuple.Operands.Skip(2).All(operand => operand.Kind != BeamOperandParameters.X)
        ? slot
        : null;

    private static bool IsSingleReturnedTupleSlotClear(BeamInstruction instruction) => instruction is
    {
        Definition.Opcode: InitYRegisters,
        Operands: [{ Value.IsOne: true }, { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null }]
    };
}
