using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? PlaceCheckedFieldCallFrame(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 4; index + 4 < body.Count; index++)
        {
            if (body[index - 4] is not { Definition.Opcode: IsTuple, Operands: [var failed, var tuple] }
                || !PlainX(tuple) || tuple.Value < 2
                || body[index - 3] is not { Definition.Opcode: TestArity, Operands: [var wrongArity, var checkedTuple, var arity] }
                || wrongArity != failed || checkedTuple != tuple
                || body[index - 2] is not { Definition.Opcode: GetTupleElement, Operands: [var earlier, var key, var projected] }
                || earlier != tuple || !PlainX(projected) || projected.Value <= tuple.Value || key.Value >= arity.Value
                || body[index - 1] is not { Definition.Opcode: ExactEqualTest, Operands: [var mismatch, var left, var right] }
                || mismatch != failed || (left != projected && right != projected)
                || body[index] is not { Definition.Opcode: GetTupleElement, Operands: [var source, var field, var scratch] }
                || source != tuple || !PlainX(scratch) || scratch.Value <= projected.Value || field.Value >= arity.Value
                || body[index + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value: var slots }, { Value: var live }] }
                || slots != 0 || live != scratch.Value + 1
                || body[index + 2] is not { Definition.Opcode: Move, Operands: [var first, var second] }
                || !PlainX(first) || first.Value != 0 || !PlainX(second) || second.Value != 1
                || body[index + 3] is not { Definition.Opcode: Move, Operands: [var loaded, var destination] }
                || loaded != scratch || destination != first)
            {
                continue;
            }

            int next = index + 4;
            if (body[next].Definition.Opcode == Line)
            {
                next++;
            }

            if (next >= body.Count || body[next] is not { Definition.Opcode: ExternalCall, Operands: [{ Value: var args }, _] }
                || args != 2)
            {
                continue;
            }

            // The tuple check already dominates both field reads. The earlier
            // discriminator is dead and the next call consumes only X0 and X1.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tuple.Value + 1);
            output.WriteByte(GetTupleElement);
            BeamCompactWriter.Write(output, XRegisterTag, tuple.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            BeamCompactWriter.Write(output, XRegisterTag, 1);
            output.WriteByte(Swap);
            BeamCompactWriter.Write(output, XRegisterTag, 0);
            BeamCompactWriter.Write(output, XRegisterTag, 1);
            copied = body[index + 4].Offset;
            index += 3;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool PlainX(BeamOperand operand) => operand.Kind == BeamOperandParameters.X && operand.TypeIndex is null;
    }
}
