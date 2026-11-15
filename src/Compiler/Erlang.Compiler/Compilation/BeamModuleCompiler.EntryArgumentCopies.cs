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
        BeamProjectionFlow? flow = null;
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
            if (entry + 1 >= end || body[entry].Definition.Opcode != Label)
            {
                continue;
            }

            int allocation = entry + 1;
            while (allocation < end && (body[allocation].Definition.Opcode is Label or Line
                || IsEntryFramePredicate(body[allocation].Definition.Opcode)))
            {
                allocation++;
            }

            if (allocation == end || body[allocation].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            int first = allocation + 1;
            int setupEnd = first;
            while (setupEnd < end && body[setupEnd].Definition.Opcode is InitYRegisters or Move or PutTuple or PutList or Line)
            {
                setupEnd++;
            }

            var remove = new Dictionary<int, BeamOperand>();
            var replace = new Dictionary<int, (int End, BeamOperand Source)>();
            for (int save = first; save < setupEnd; save++)
            {
                if (body[save] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                    || source is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                    || slot.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if ((flow ??= new BeamProjectionFlow(body)).CanUseEntryRegister(
                    entry,
                    save,
                    end,
                    source,
                    slot
                ))
                {
                    remove.Add(save, slot);
                    RecordEntryArgumentUses(body, stackOperands, save, end, source, slot, replace);
                }
            }

            if (remove.Count == 0)
            {
                continue;
            }

            var replacements = replace.OrderBy(edit => edit.Key).ToArray();
            int replacement = 0;
            for (int index = first; index < end; index++)
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

    private static void RecordEntryArgumentUses(
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands,
        int save,
        int end,
        BeamOperand source,
        BeamOperand slot,
        Dictionary<int, (int End, BeamOperand Source)> edits
    )
    {
        int first = body[save + 1].Offset;
        int last = body[end].Offset;
        int indexOfOperand = stackOperands.BinarySearch((first, 0, 0),
            Comparer<(int Start, int End, int Slot)>.Create(static (left, right) => left.Start.CompareTo(right.Start)));
        for (int index = indexOfOperand < 0 ? ~indexOfOperand : indexOfOperand;
            index < stackOperands.Count && stackOperands[index].Start < last;
            index++)
        {
            var (start, finish, register) = stackOperands[index];
            if (register == slot.Value)
            {
                edits.Add(start, (finish, source));
            }
        }
    }
}
