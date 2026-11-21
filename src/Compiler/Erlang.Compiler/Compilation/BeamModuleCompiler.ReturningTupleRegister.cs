using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReuseReturningTupleRegister(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                || body[index + 1] is not { Definition.Opcode: PutTuple, Operands: [var temp, _, ..] } inner
                || temp is not { Kind: BeamOperandParameters.X, TypeIndex: null } || temp == result
                || body[index + 2] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } outer
                || destination != result || !inner.Operands.Skip(2).All(IsStable)
                || !outer.Operands.Skip(2).All(field => field == temp || IsStable(field))
                || !outer.Operands.Skip(2).Contains(temp))
            {
                continue;
            }

            int returned = index + 3;
            if (body[returned].Definition.Opcode == Deallocate)
            {
                returned++;
            }

            if (body[returned].Definition.Opcode != Return)
            {
                continue;
            }

            // These pure constructors share a heap check and have no active X
            // root at its entry. Their common output can occupy X0 throughout.
            output.Write(code.Span[copied..inner.Offset]);
            WriteTuple(inner, result, oldRegister: null);
            WriteTuple(outer, result, oldRegister: temp);
            copied = body[index + 3].Offset;
            index = returned;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsStable(BeamOperand operand) => operand.TypeIndex is null && operand.Kind is
            BeamOperandParameters.Y or BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil;

        void WriteTuple(BeamInstruction tuple, BeamOperand register, BeamOperand? oldRegister)
        {
            output.WriteByte(PutTuple);
            WriteRegister(output, register);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, tuple.Operands[1].Value);
            foreach (var field in tuple.Operands.Skip(2))
            {
                int tag = field.Kind switch
                {
                    BeamOperandParameters.Atom or BeamOperandParameters.Nil => AtomTag,
                    BeamOperandParameters.Integer => BeamWire.IntegerTag,
                    BeamOperandParameters.Y => YRegisterTag,
                    _ => XRegisterTag,
                };
                BeamCompactWriter.Write(output, tag, field == oldRegister ? register.Value : field.Value);
            }
        }
    }
}
