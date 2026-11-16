using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SimplifyValueSelections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands
    )
    {
        int trueAtom = atoms.GetValueOrDefault(ErlangBooleanAtoms.True);
        int falseAtom = atoms.GetValueOrDefault(ErlangBooleanAtoms.False);
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

            bool singleton = retained.Count == 1
                && retained[0].Value.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Integer;
            bool boolean = retained.Count == 2 && retained[0].Target == retained[1].Target
                && retained.All(arm => arm.Value.Kind == BeamOperandParameters.Atom)
                && ((retained[0].Value.Value == trueAtom && retained[1].Value.Value == falseAtom)
                    || (retained[0].Value.Value == falseAtom && retained[1].Value.Value == trueAtom));
            if (retained.Count == armCount && !singleton && !boolean)
            {
                continue;
            }

            output.Write(code.Span[copied..instruction.Offset]);
            if (retained.Count == 0)
            {
                output.WriteByte(Jump);
                BeamCompactWriter.Write(output, LabelTag, defaultLabel.Label);
            }
            else if (singleton || boolean)
            {
                var arm = retained[0];
                output.WriteByte(boolean ? IsBoolean : ExactEqualTest);
                BeamCompactWriter.Write(output, LabelTag, defaultLabel.Label);
                output.Write(code.Span[(instruction.Offset + 1)..defaultLabel.Start]);
                if (singleton)
                {
                    output.Write(code.Span[arm.Start..arm.End]);
                }

                output.WriteByte(Jump);
                BeamCompactWriter.Write(output, LabelTag, arm.Target);
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
