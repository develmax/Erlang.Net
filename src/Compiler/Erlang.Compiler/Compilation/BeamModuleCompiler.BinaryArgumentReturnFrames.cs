using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SeparateBinaryArgumentReturnFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        for (int info = 0; info + 15 < body.Count; info++)
        {
            int allocation = info + 2;
            int check = allocation + 2;
            int heap = check + 1;
            int tuple = heap + 1;
            int rejected = tuple + 3;
            if (body[info].Definition.Opcode != FunctionInfo
                || body[info + 1].Definition.Opcode != Label
                || body[allocation] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.IsOne: true }, var live]
                }
                || live.Value > body[info].Operands[^1].Value
                || body[allocation + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } argument,
                    { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null } saved
                    ]
                }
                || argument.Value >= live.Value
                || body[check] is not { Definition.Opcode: IsBinary, Operands: [var failed, var tested] }
                || tested != saved
                || body[heap] is not { Definition.Opcode: TestHeap, Operands: [var words, { Value.IsZero: true }] }
                || body[tuple] is not { Definition.Opcode: PutTuple, Operands: [var target, _, var tag, var field] }
                || target != result
                || !IsNonRegisterValue(tag)
                || field != saved
                || !IsSingleSlotReturn(tuple + 1)
                || body[rejected] is not { Definition.Opcode: Label, Operands: [var label] }
                || label.Value != failed.Value
                || body[rejected + 1] is not { Definition.Opcode: Move, Operands: [var restored, var outgoing] }
                || restored != saved
                || outgoing != result)
            {
                continue;
            }

            int call = rejected + 2;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call + 5 >= body.Count
                || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || !body[call].Operands[0].Value.IsOne
                || body[call + 1] is not { Definition.Opcode: TestHeap, Operands: [var rejectedWords, { Value.IsOne: true }] }
                || rejectedWords != words
                || body[call + 2] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, var rejectedTag, var value] }
                || destination != result
                || rejectedTag != tag
                || value != result
                || !IsSingleSlotReturn(call + 3)
                || body[call + 5].Definition.Opcode != Label
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(check, (int)label.Value, out int unique)
                || unique != rejected)
            {
                continue;
            }

            // Both branches consume the incoming argument before any X clobber.
            // On success the tuple needs its argument rooted. On failure the
            // ordinary call still needs a continuation frame, with no Y slots.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(IsBinary);
            BeamCompactWriter.Write(output, LabelTag, label.Value);
            WriteRegister(output, argument);
            int heapLiveOffset = body[tuple].Offset - CompactRegisterCountWidth(0);
            output.Write(code.Span[body[heap].Offset..heapLiveOffset]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, argument.Value + 1);
            int tupleArgumentOffset = body[tuple + 1].Offset - CompactRegisterCountWidth(0);
            output.Write(code.Span[body[tuple].Offset..tupleArgumentOffset]);
            WriteRegister(output, argument);
            output.Write(code.Span[body[tuple + 2].Offset..body[rejected + 1].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            if (argument != result)
            {
                output.WriteByte(Move);
                WriteRegister(output, argument);
                WriteRegister(output, result);
            }

            output.Write(code.Span[body[rejected + 2].Offset..body[call + 3].Offset]);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            copied = body[call + 4].Offset;
            info = call + 4;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsSingleSlotReturn(int deallocate) =>
            body[deallocate] is { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
            && body[deallocate + 1].Definition.Opcode == Return;
    }
}
