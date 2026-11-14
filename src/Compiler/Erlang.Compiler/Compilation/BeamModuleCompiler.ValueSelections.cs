using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? SimplifyValueSelections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int labelIndex = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction is not { Definition.Opcode: SelectValue, Operands: [_, var fallback, _, ..] }
                || fallback.Kind != BeamOperandParameters.Label)
            {
                continue;
            }

            int end = body[index + 1].Offset;
            while (labelIndex < labelOperands.Count && labelOperands[labelIndex].Start < instruction.Offset)
            {
                labelIndex++;
            }

            int firstLabel = labelIndex;
            while (labelIndex < labelOperands.Count && labelOperands[labelIndex].Start < end)
            {
                labelIndex++;
            }

            int armCount = (instruction.Operands.Count - 3) / 2;
            if (labelIndex - firstLabel != armCount + 1)
            {
                continue;
            }

            var defaultLabel = labelOperands[firstLabel];
            var cursor = new BeamCursor(code[defaultLabel.End..end]);
            if (cursor.ReadByte() != ExtendedListOperand)
            {
                continue;
            }

            _ = BeamCompactReader.Read(cursor);
            int armStart = defaultLabel.End + cursor.Position;
            var retained = new List<(BeamOperand Value, int Start, int End, int Target)>();
            for (int arm = 0; arm < armCount; arm++)
            {
                var branch = labelOperands[firstLabel + arm + 1];
                if (branch.Label != defaultLabel.Label)
                {
                    retained.Add((instruction.Operands[3 + arm * 2], armStart, branch.Start, branch.Label));
                }

                armStart = branch.End;
            }

            if (retained.Count == armCount
                && (retained.Count != 1 || retained[0].Value.Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer)))
            {
                continue;
            }

            output.Write(code.Span[copied..instruction.Offset]);
            if (retained.Count == 0)
            {
                output.WriteByte(Jump);
                BeamCompactWriter.Write(output, LabelTag, defaultLabel.Label);
            }
            else if (retained is [(var value, var start, var valueEnd, var target)]
                && value.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Integer)
            {
                output.WriteByte(ExactEqualTest);
                BeamCompactWriter.Write(output, LabelTag, defaultLabel.Label);
                output.Write(code.Span[(instruction.Offset + 1)..defaultLabel.Start]);
                output.Write(code.Span[start..valueEnd]);
                output.WriteByte(Jump);
                BeamCompactWriter.Write(output, LabelTag, target);
            }
            else
            {
                // Preserve the compact operand bytes, including typed registers and literals.
                output.Write(code.Span[instruction.Offset..(defaultLabel.End + 1)]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, retained.Count * 2);
                foreach (var arm in retained)
                {
                    output.Write(code.Span[arm.Start..arm.End]);
                    BeamCompactWriter.Write(output, LabelTag, arm.Target);
                }
            }

            copied = end;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        return output.ToArray();
    }
}
