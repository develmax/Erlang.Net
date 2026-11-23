using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RewriteTaggedTupleGuards(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int test = 0; test + 4 < body.Count; test++)
        {
            if (body[test] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } fail, { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } input]
                }
                || body[test + 1] is not
                {
                    Definition.Opcode: TestArity, Operands: [var arityFail, var tuple, { Value.Sign: > 0 } arity]
                } || fail != arityFail || !SameProjectionRegister(input, tuple)
                || body[test + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Value.IsZero: true }, { Kind: BeamOperandParameters.X } tagField]
                } || !SameProjectionRegister(input, source)
                || body[test + 3] is not
                {
                    Definition.Opcode: ExactEqualTest, Operands: [var tagFail, var left, var right]
                } || tagFail != fail)
            {
                continue;
            }

            BeamOperand? atom = ReadTagComparison(tagField, left, right) ?? ReadTagComparison(tagField, right, left);
            if (atom is not { } tag
                || !(flow ??= new BeamProjectionFlow(body)).IsBranchTemporaryDead(test + 3, tagField))
            {
                continue;
            }

            // The type, arity and tag reject to the same label. The projected
            // tag must be dead on both successors of the original comparison.
            output.Write(code.Span[copied..body[test].Offset]);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, fail.Value);
            WriteRegister(output, input);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity.Value);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            copied = body[test + 4].Offset;
            test += 3;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static BeamOperand? ReadTagComparison(BeamOperand field, BeamOperand register, BeamOperand literal) =>
        SameProjectionRegister(field, register) && literal is { Kind: BeamOperandParameters.Atom, Value.Sign: > 0 }
            ? literal : null;
}
