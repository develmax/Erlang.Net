using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderCheckedEntryFrameRoots(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        foreach (var schedule in ReadCheckedEntryCallSchedules(body))
        {
            if (!HasLinearRootDefinitions(body, schedule)
                || ReadFrameRootOrder(
                    body,
                    schedule.Allocation + 1,
                    schedule.End,
                    schedule.Width
                ) is not { } slots
                || slots.Select((slot, index) => slot == index).All(same => same))
            {
                continue;
            }

            int start = body[schedule.Allocation + 1].Offset;
            while (operand < operands.Count && operands[operand].Start < start)
            {
                operand++;
            }

            for (int index = schedule.Allocation + 1; index < schedule.End; index++)
            {
                var instruction = body[index];
                int next = body[index + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    WriteFrameInitialization(output, instruction.Operands.Skip(1)
                        .Select(item => slots[(int)item.Value]).Order().ToArray());
                    copied = next;
                    while (operand < operands.Count && operands[operand].Start < next)
                    {
                        operand++;
                    }

                    continue;
                }

                while (operand < operands.Count && operands[operand].Start < next)
                {
                    var (from, to, slot) = operands[operand++];
                    output.Write(code.Span[copied..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, slots[slot]);
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

    private static bool HasLinearRootDefinitions(
        IReadOnlyList<BeamInstruction> body,
        CheckedEntryCallSchedule schedule
    )
    {
        var roots = new HashSet<int>();
        for (int index = schedule.Allocation + 1; index < schedule.End; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode is not
                (Line or InitYRegisters or Move or GetTupleElement or LocalCall or ExternalCall))
            {
                return false;
            }

            int destination = StackAllocationDestination(instruction);
            if (destination < 0 || instruction.Operands[destination] is not { Kind: BeamOperandParameters.Y } slot
                || !roots.Add((int)slot.Value))
            {
                continue;
            }

            if (instruction.Definition.Opcode != Move || !IsBareX0(instruction.Operands[0])
                || roots.Count > 1 && body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                return false;
            }

            if (roots.Count == schedule.Width)
            {
                return true;
            }
        }

        return false;
    }
}
