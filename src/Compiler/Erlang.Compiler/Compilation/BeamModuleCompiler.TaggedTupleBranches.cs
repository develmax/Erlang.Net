using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? NormalizeTaggedTupleBranches(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        if (!SsaTypeInferenceEnabled)
        {
            return null;
        }

        var targets = new Dictionary<int, int>();
        for (int index = 0; index < body.Count - 1; index++)
        {
            if (body[index] is { Definition.Opcode: BeamEmissionFormat.Label, Operands: [var target] })
            {
                targets.TryAdd((int)target.Value, index + 1);
            }
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index < body.Count - 1; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var notTagged, { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } selector,
                        var arity, var tag]
                }
                || !targets.TryGetValue((int)notTagged.Value, out int alternative)
                || body[alternative] is not { Definition.Opcode: ExactEqualTest, Operands: [var failure, var left, var right] }
                || !(IsAtomTest(left, right, selector) || IsAtomTest(right, left, selector))
                || failure.Value == notTagged.Value
                || !targets.TryGetValue((int)failure.Value, out int failed)
                || !RejectsSelector(failed, selector))
            {
                continue;
            }

            // The atom branch only needs non-tuples. A tuple failing the tag
            // test would fail the atom comparison and raise the same case error.
            int registerTag = selector.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag;
            output.Write(code.Span[copied..instruction.Offset]);
            output.WriteByte(IsTuple);
            BeamCompactWriter.Write(output, LabelTag, notTagged.Value);
            BeamCompactWriter.Write(output, registerTag, selector.Value);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            output.WriteByte(ExtendedTypedRegisterOperand);
            BeamCompactWriter.Write(output, registerTag, selector.Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, GetOperandTypeIndex((TupleTypeBits, null, null, null)));
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, arity.Value);
            BeamCompactWriter.Write(output, AtomTag, tag.Value);
            copied = body[index + 1].Offset;

            if (index + 4 < body.Count
                && body[index + 1] is { Definition.Opcode: GetTupleElement, Operands: [var source, _, var field] }
                && SameRegister(source, selector) && !SameRegister(field, selector)
                && body[index + 2] is { Definition.Opcode: IsTuple, Operands: [var wrongType, var tested] }
                && SameRegister(tested, field) && tested.TypeIndex is null && wrongType.Value == notTagged.Value
                && body[index + 3] is { Definition.Opcode: TestArity, Operands: [var wrongArity, var sized, var size] }
                && SameRegister(sized, field) && sized.TypeIndex is null && wrongArity.Value == notTagged.Value)
            {
                // The outer tuple is still in its register during these
                // immediate projection tests. Their failures cannot match the atom either.
                int fieldTag = field.Kind == BeamOperandParameters.X ? XRegisterTag : YRegisterTag;
                output.Write(code.Span[copied..body[index + 2].Offset]);
                output.WriteByte(IsTuple);
                BeamCompactWriter.Write(output, LabelTag, failure.Value);
                BeamCompactWriter.Write(output, fieldTag, field.Value);
                output.WriteByte(TestArity);
                BeamCompactWriter.Write(output, LabelTag, failure.Value);
                BeamCompactWriter.Write(output, fieldTag, field.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, size.Value);
                copied = body[index + 4].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool RejectsSelector(int failure, BeamOperand selector)
        {
            // The failure location remains on both paths to the terminal case error.
            while (failure < body.Count && body[failure].Definition.Opcode == Line)
            {
                failure++;
            }

            return failure < body.Count
                && body[failure] is { Definition.Opcode: CaseEnd, Operands: [var rejected] }
                && SameRegister(rejected, selector);
        }

        static bool IsAtomTest(BeamOperand register, BeamOperand value, BeamOperand input) =>
            value.Kind == BeamOperandParameters.Atom && SameRegister(register, input);

        static bool SameRegister(BeamOperand left, BeamOperand right) =>
            left.Kind == right.Kind && left.Value == right.Value;
    }
}
