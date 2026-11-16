using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? UseSavedBranchOperands(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
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

            int start = functionStart;
            functionStart = end + 1;
            bool? hasHandler = null;
            for (int index = start; index + 1 < end; index++)
            {
                if (body[index] is not { Definition.Opcode: Move, Operands: [var saved, var temporary] }
                    || saved is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                    || temporary is not { Kind: BeamOperandParameters.X, TypeIndex: null })
                {
                    continue;
                }

                int use = index + 1;
                while (use < end && body[use].Definition.Opcode == Line)
                {
                    use++;
                }

                if (use == end)
                {
                    continue;
                }

                BeamInstruction branch = body[use];
                byte opcode = branch.Definition.Opcode;
                bool select = opcode is SelectValue or SelectTupleArity;
                int operandIndex = select ? 0 : 1;
                if (!(select || IsEntryFramePredicate(opcode))
                    || branch.Operands.Count <= operandIndex
                    || !SameProjectionRegister(branch.Operands[operandIndex], temporary)
                    || branch.Operands.Skip(operandIndex + 1).Any(other => SameProjectionRegister(other, temporary))
                    || (hasHandler ??= body.Skip(start).Take(end - start)
                        .Any(instruction => instruction.Definition.Opcode is TryEnter or CatchEnter))
                    || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(use, temporary))
                {
                    continue;
                }

                // Keep the original type annotation and line bytes when moving the operand to the Y bank.
                int opcodeEnd = branch.Offset + 1;
                var cursor = new BeamCursor(code[opcodeEnd..]);
                if (!select)
                {
                    _ = BeamCompactReader.Read(cursor);
                }

                int operandStart = opcodeEnd + cursor.Position;
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[copied..body[index].Offset]);
                output.Write(code.Span[body[index + 1].Offset..operandStart]);
                if (branch.Operands[operandIndex].TypeIndex is not null)
                {
                    output.WriteByte(ExtendedTypedRegisterOperand);
                    _ = BeamCompactReader.Read(cursor);
                }

                WriteRegister(output, saved);
                copied = opcodeEnd + cursor.Position;
                index = use;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
