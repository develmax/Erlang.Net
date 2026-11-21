using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ConstructSendTupleInArgument(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        const int OneSavedRecipient = 1;
        const int EmptySendFrame = 0;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 7 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: AllocateHeap, Operands: [{ Value.IsOne: true }, _, _] }
                || body[index + 1] is not
                {
                    Definition.Opcode: Move, Operands:
                    [
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } recipient,
                    { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null } savedRecipient
                    ]
                }
                || body[index + 2] is not { Definition.Opcode: PutTuple } tuple
                || tuple.Operands[0] != recipient
                || tuple.Operands.Skip(2).Any(operand => operand.Kind == BeamOperandParameters.Y)
                || body[index + 3] is not
                {
                    Definition.Opcode: Move, Operands:
                    [
                        var constructed,
                    { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null } messageArgument
                    ]
                }
                || constructed != recipient
                || body[index + 4] is not { Definition.Opcode: Move, Operands: [var saved, var restored] }
                || saved != savedRecipient
                || restored != recipient)
            {
                continue;
            }

            int send = index + 5;
            if (body[send].Definition.Opcode == Line)
            {
                send++;
            }

            if (body[send].Definition.Opcode != Send)
            {
                continue;
            }

            int deallocate = send + 1;
            if (body[deallocate].Definition.Opcode == Line)
            {
                deallocate++;
            }

            if (deallocate < body.Count && body[deallocate] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom, TypeIndex: null }, var returned]
                } && returned == recipient)
            {
                deallocate++;
            }

            if (deallocate + 1 >= body.Count
                || body[deallocate] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || width.Value != OneSavedRecipient
                || body[deallocate + 1].Definition.Opcode != Return)
            {
                continue;
            }

            // The one slot protects only the original recipient. With no Y reads in
            // the constructor or suffix, retaining X0 and writing X1 removes that slot.
            // The send body call still requires a frame.
            int widthOffset = body[index].Offset + 1;
            output.Write(code.Span[copied..widthOffset]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, EmptySendFrame);
            output.Write(code.Span[OperandEnd(widthOffset)..body[index + 1].Offset]);
            output.WriteByte(PutTuple);
            WriteRegister(output, messageArgument);
            output.Write(code.Span[OperandEnd(tuple.Offset + 1)..body[index + 3].Offset]);
            output.Write(code.Span[body[index + 5].Offset..body[deallocate].Offset]);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, EmptySendFrame);
            copied = body[deallocate + 1].Offset;
            index = deallocate;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int OperandEnd(int offset)
        {
            var cursor = new BeamCursor(code[offset..]);
            _ = BeamCompactReader.Read(cursor);

            return offset + cursor.Position;
        }
    }
}
