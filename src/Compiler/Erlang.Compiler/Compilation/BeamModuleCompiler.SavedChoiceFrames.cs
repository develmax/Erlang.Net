using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? KeepSavedChoiceInFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 5 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: Jump, Operands: [var joined] }
                || body[index + 1] is not { Definition.Opcode: Label, Operands: [var retained] }
                || body[index + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } slot,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } temporary
                    ]
                }
                || body[index + 3] is not { Definition.Opcode: Label, Operands: [var joinLabel] }
                || joinLabel.Value != joined.Value || joinLabel.Value == retained.Value
                || !IsMove(body[index + 4], temporary, slot)
                || body[index + 5] is not { Definition.Opcode: Move, Operands: [var value, var overwritten] }
                || overwritten != temporary || SameProjectionRegister(value, temporary))
            {
                continue;
            }

            int jump = body[index].Offset;
            int retainedLabel = body[index + 1].Offset;
            int join = (int)joined.Value;
            int saved = (int)retained.Value;
            var incoming = references.Where(reference => reference.Label == join || reference.Label == saved).ToArray();
            if (incoming.Length != 2 || !incoming.Any(reference => reference.Label == join
                && reference.Start > jump && reference.End <= retainedLabel)
                || !incoming.Any(reference => reference.Label == saved && reference.End <= jump))
            {
                continue;
            }

            // The jump's path has its new value in X. The other path already
            // has its value in Y, and both paths overwrite X on entry to the
            // continuation. Commit the new value only on its own edge.
            int overwrite = body[index + 5].Offset;
            output.Write(code.Span[copied..jump]);
            output.Write(code.Span[body[index + 4].Offset..overwrite]);
            output.Write(code.Span[retainedLabel..body[index + 2].Offset]);
            copied = overwrite;
            index += 4;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
