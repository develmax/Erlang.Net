using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveOverwrittenFrameMoves(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteOverwrittenFrameMoves(code, decoded.Body);
    }

    private byte[]? RemoveRepeatedFallthroughFrameWrites(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var edits = new List<CodeEdit>();
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } value,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                    ]
                }
                || body[index + 1].Definition.Opcode != Label
                || body[index + 2] is not { Definition.Opcode: Move, Operands: [var joinedValue, var joinedSlot] }
                || value != joinedValue || slot != joinedSlot)
            {
                continue;
            }

            // Fallthrough executes the same copy at the join before any
            // read or GC boundary. Other predecessors keep that join copy.
            edits.Add(new CodeEdit(
                body[index].Offset,
                body[index + 1].Offset,
                0,
                0,
                IsRemoval: true
            ));
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private static byte[]? RewriteOverwrittenFrameMoves(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var overwritten = new HashSet<BeamOperand>();
        var removed = new List<int>();
        for (int index = body.Count - 1; index >= 0; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == Line)
            {
                continue;
            }

            // Stop at branches, frame operations and anything that might
            // collect or report a failure. The constructors consume the
            // space reserved by the earlier allocation or heap test.
            if (opcode is not (Move or GetTupleElement or GetList or GetHead or GetTail or PutList or PutTuple))
            {
                overwritten.Clear();
                continue;
            }

            if (instruction is { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.Y } saved] }
                && overwritten.Contains(saved))
            {
                removed.Add(index);
                continue;
            }

            int destination = StackWriteOperand(instruction);
            RecordWrite(instruction.Operands[destination]);
            if (opcode == GetList)
            {
                RecordWrite(instruction.Operands[2]);
            }

            // Sources are read before the destinations are assigned. A
            // constructor reading its own destination still needs it.
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (operand.Kind == BeamOperandParameters.Y
                    && position != destination && !(opcode == GetList && position == 2))
                {
                    overwritten.Remove(operand with { TypeIndex = null });
                }
            }
        }

        if (removed.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = removed.Count - 1; index >= 0; index--)
        {
            int dead = removed[index];
            output.Write(code.Span[copied..body[dead].Offset]);
            copied = body[dead + 1].Offset;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void RecordWrite(BeamOperand operand)
        {
            if (operand.Kind == BeamOperandParameters.Y)
            {
                overwritten.Add(operand with { TypeIndex = null });
            }
        }
    }
}
