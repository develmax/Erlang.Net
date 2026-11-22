using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldInitialFrameTrims(ReadOnlyMemory<byte> code, int labelCount)
    {
        var stackOperands = new List<(int Start, int End, int Slot)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordStackOperand: (start, end, slot) => stackOperands.Add((start, end, slot))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction allocation = body[index];
            if (allocation.Definition.Opcode is not (Allocate or AllocateHeap)
                || FindInitialFrameTrim(body, index) is not { } trim)
            {
                continue;
            }

            int removed = (int)body[trim].Operands[0].Value;
            int kept = (int)body[trim].Operands[1].Value;
            int sizeStart = allocation.Offset + 1;
            var cursor = new BeamCursor(code[sizeStart..]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..sizeStart]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, kept);
            copied = sizeStart + cursor.Position;
            for (int prefix = index + 1; prefix < trim; prefix++)
            {
                BeamInstruction instruction = body[prefix];
                int next = body[prefix + 1].Offset;
                if (instruction.Definition.Opcode == InitYRegisters)
                {
                    int[] slots = instruction.Operands.Skip(1).Where(slot => slot.Value >= removed)
                        .Select(slot => (int)slot.Value - removed).ToArray();
                    output.Write(code.Span[copied..instruction.Offset]);
                    if (slots.Length != 0)
                    {
                        WriteFrameInitialization(output, slots);
                    }

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
                    BeamCompactWriter.Write(output, YRegisterTag, slot - removed);
                    copied = to;
                }
            }

            output.Write(code.Span[copied..body[trim].Offset]);
            copied = body[trim + 1].Offset;
            index = trim;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? FoldTransientArgumentFrameTrims(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 5 < body.Count; index++)
        {
            if (ReadTransientArgumentTrim(body, index) is not { } plan)
            {
                continue;
            }

            // The saved register is read before its unrelated replacement.
            // The sole Y value now has no use, so keep only the zero-slot
            // call frame that the completed trim already established.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[index].Operands[1].Value);
            output.WriteByte(Move);
            WriteRegister(output, plan.Saved);
            WriteRegister(output, plan.Destination);
            output.Write(code.Span[body[index + 2].Offset..body[index + 3].Offset]);
            copied = body[index + 5].Offset;
            index += 4;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (BeamOperand Saved, BeamOperand Destination)? ReadTransientArgumentTrim(
        IReadOnlyList<BeamInstruction> body,
        int allocation
    )
    {
        if (body[allocation] is not { Definition.Opcode: Allocate, Operands: [{ Value.IsOne: true }, var live] }
            || body[allocation + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } saved,
                { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null } temporary]
            }
            || body[allocation + 2] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } other, var overwritten]
            }
            || overwritten != saved || saved.Value >= live.Value || other.Value >= live.Value
            || body[allocation + 3] is not
            {
                Definition.Opcode: Move,
                Operands: [var reload, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            }
            || reload != temporary || saved == destination || saved == other || other == destination
            || body[allocation + 4] is not
            { Definition.Opcode: TrimFrame, Operands: [{ Value.IsOne: true }, { Value.IsZero: true }] })
        {
            return null;
        }

        int call = allocation + 5;
        while (call < body.Count && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (call >= body.Count || body[call] is not
            { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            || saved.Value >= arity.Value || destination.Value >= arity.Value)
        {
            return null;
        }

        return (saved, destination);
    }

    private static int? FindInitialFrameTrim(IReadOnlyList<BeamInstruction> body, int allocation)
    {
        int size = (int)body[allocation].Operands[0].Value;
        int firstUsed = size;
        for (int index = allocation + 1; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == TrimFrame)
            {
                int removed = (int)instruction.Operands[0].Value;
                int kept = (int)instruction.Operands[1].Value;

                return removed > 0 && removed <= firstUsed && removed + kept == size ? index : null;
            }

            if (opcode == InitYRegisters)
            {
                continue;
            }

            // Once the frame can be observed along another path, the smaller frame might not fit that path.
            if (opcode is not (Line or Move or Swap or GetTupleElement or GetList or GetHead or GetTail
                or PutTuple or PutList or TestHeap))
            {
                break;
            }

            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y)
                {
                    firstUsed = Math.Min(firstUsed, (int)operand.Value);
                }
            }
        }

        return null;
    }
}
