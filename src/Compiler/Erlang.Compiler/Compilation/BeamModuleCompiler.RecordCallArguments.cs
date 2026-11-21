using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? StoreRecordUpdateInCallArgument(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            BeamInstruction update = body[index];
            if (update is not
                {
                    Definition.Opcode: UpdateRecord,
                    Operands: [
                        _,
                        _,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y } source,
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } result,
                        ..
                    ]
                }
                || body[index + 1] is not
                {
                    Definition.Opcode: Move, Operands: [var copy, { Kind: BeamOperandParameters.X, TypeIndex: null } argument]
                }
                || !SameProjectionRegister(copy, result) || argument.Value == result.Value
                || body[index + 2] is not { Definition.Opcode: Move, Operands: [var replacement, var destination] }
                || !SameProjectionRegister(destination, result) || SameProjectionRegister(replacement, result))
            {
                continue;
            }

            int call = index + 3;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call == body.Count || body[call].Definition.Opcode != ExternalCall
                || argument.Value >= body[call].Operands[0].Value)
            {
                continue;
            }

            // The outgoing operand sees the record before the following move overwrites X0.
            int start = update.Offset + 1;
            var cursor = new BeamCursor(code[start..]);
            _ = BeamCompactReader.Read(cursor);
            _ = BeamCompactReader.Read(cursor);
            _ = BeamCompactReader.Read(cursor);
            if (source.TypeIndex.HasValue)
            {
                // The extension is followed by the original register and its type ordinal.
                _ = BeamCompactReader.Read(cursor);
                _ = BeamCompactReader.Read(cursor);
            }

            output.Write(code.Span[copied..(start + cursor.Position)]);
            WriteRegister(output, argument);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[(start + cursor.Position)..body[index + 1].Offset]);
            copied = body[index + 2].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
