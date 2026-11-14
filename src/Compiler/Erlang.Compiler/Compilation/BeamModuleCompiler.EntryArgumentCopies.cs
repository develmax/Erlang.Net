using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? EliminateEntryArgumentCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int functionStart = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = functionStart;
            functionStart = end + 1;
            if (entry + 1 >= end || body[entry].Definition.Opcode != Label
                || body[entry + 1].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            int call = entry + 2;
            while (call < end && body[call].Definition.Opcode is InitYRegisters or Move or PutTuple or PutList or Line)
            {
                call++;
            }

            if (call == end || body[call].Definition.Opcode is not
                (LocalCall or ExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame))
            {
                continue;
            }

            var remove = new Dictionary<int, BeamOperand>();
            var replace = new Dictionary<int, (int End, BeamOperand Source)>();
            for (int save = entry + 2; save < call; save++)
            {
                if (body[save] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                    || source is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                    || slot.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if (PlanEntryArgumentCopy(body, stackOperands, save, call, end, source, slot, replace))
                {
                    remove.Add(save, slot);
                }
            }

            if (remove.Count == 0)
            {
                continue;
            }

            var replacements = replace.OrderBy(edit => edit.Key).ToArray();
            int replacement = 0;
            for (int index = entry + 2; index < call; index++)
            {
                BeamInstruction instruction = body[index];
                if (remove.TryGetValue(index, out BeamOperand? slot))
                {
                    output.Write(code.Span[copied..instruction.Offset]);
                    output.WriteByte(InitYRegisters);
                    output.WriteByte(ExtendedListOperand);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                    WriteRegister(output, slot);
                    copied = body[index + 1].Offset;
                    continue;
                }

                int next = body[index + 1].Offset;
                while (replacement < replacements.Length && replacements[replacement].Key < next)
                {
                    var (start, (finish, source)) = replacements[replacement++];
                    output.Write(code.Span[copied..start]);
                    WriteRegister(output, source);
                    copied = finish;
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

    private static bool PlanEntryArgumentCopy(
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands,
        int save,
        int call,
        int end,
        BeamOperand source,
        BeamOperand slot,
        Dictionary<int, (int End, BeamOperand Source)> edits
    )
    {
        bool sourceAvailable = true;
        for (int index = save + 1; index < end; index++)
        {
            var instruction = body[index];
            var operands = instruction.Operands;
            if (index >= call)
            {
                if (instruction.Definition.Opcode == TrimFrame
                    || instruction.Definition.Name == "debug_line"
                    || operands.Any(operand => SameProjectionRegister(operand, slot)))
                {
                    return false;
                }

                continue;
            }

            BeamOperand? destination = instruction.Definition.Opcode switch
            {
                Move => operands[1],
                PutTuple => operands[0],
                PutList => operands[2],
                _ => null
            };
            if (operands.Any(operand => SameProjectionRegister(operand, slot))
                && (!sourceAvailable || destination is null || SameProjectionRegister(destination, slot)
                    || destination.Kind != BeamOperandParameters.X))
            {
                return false;
            }

            if (destination is not null && SameProjectionRegister(destination, source))
            {
                sourceAvailable = false;
            }
        }

        int first = body[save + 1].Offset;
        int last = body[call].Offset;
        int indexOfOperand = stackOperands.BinarySearch((first, 0, 0),
            Comparer<(int Start, int End, int Slot)>.Create(static (left, right) => left.Start.CompareTo(right.Start)));
        bool replaced = false;
        for (int index = indexOfOperand < 0 ? ~indexOfOperand : indexOfOperand;
            index < stackOperands.Count && stackOperands[index].Start < last;
            index++)
        {
            var (start, finish, register) = stackOperands[index];
            if (register == slot.Value)
            {
                edits.Add(start, (finish, source));
                replaced = true;
            }
        }

        return replaced;
    }
}
