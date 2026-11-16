using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? StoreClosureResultsDirectly(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            BeamInstruction closure = body[index];
            if (closure.Definition.Opcode != MakeFun
                || closure.Operands[1] is not { Kind: BeamOperandParameters.X, TypeIndex: null } result)
            {
                continue;
            }

            int store = index + 1;
            if (body[store] is not { Definition.Opcode: Move, Operands: [var source, var slot] }
                || source != result || slot is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || closure.Operands.Skip(2).Any(operand => SameProjectionRegister(operand, slot))
                || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(store, result))
            {
                continue;
            }

            int operands = closure.Offset + 1;
            var cursor = new BeamCursor(code[operands..]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..(operands + cursor.Position)]);
            WriteRegister(output, slot);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[(operands + cursor.Position)..body[store].Offset]);
            copied = body[store + 1].Offset;
            index = store;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
