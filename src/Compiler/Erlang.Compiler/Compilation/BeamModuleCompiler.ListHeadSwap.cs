using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? FoldListHeadSwap(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 2 < body.Count; index++)
        {
            if (body[index - 1] is not
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } argument,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved] }
                || body[index] is not
                { Definition.Opcode: GetList, Operands: [
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } list,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } head,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } tail] }
                || body[index + 1] is not
                { Definition.Opcode: Swap, Operands: [var left, var right] }
                || argument == head || saved == tail
                || !(left == head && right == argument || right == head && left == argument))
            {
                continue;
            }

            // Place the extracted head directly in its final register and restore
            // the other argument from the saved copy the frame already holds.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(GetList);
            WriteRegister(output, list);
            WriteRegister(output, argument);
            WriteRegister(output, tail);
            output.WriteByte(Move);
            WriteRegister(output, saved);
            WriteRegister(output, head);
            copied = body[index + 2].Offset;
            index++;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
