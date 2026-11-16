using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly record struct BinaryCapture(int TotalBits, int CaptureBits, bool Tail);

    private static BinaryCapture? ReadBinaryCapture(Clause clause)
    {
        if (clause is not { Guard: null, Patterns: [BitPattern pattern], Body: Expr.Variable result }
            || result.Name == VariableScopeNames.Wildcard || pattern.Segments.Count == 0)
        {
            return null;
        }

        int total = 0;
        int capture = 0;
        for (int index = 0; index < pattern.Segments.Count; index++)
        {
            var segment = pattern.Segments[index];
            var spec = segment.Specification;
            if (segment.Value is not Pattern.Variable variable || spec.Signed || spec.Endian != BitByteOrders.Big)
            {
                return null;
            }
            if (spec.Type == BitSegmentTypes.Binary && spec.Size is null && spec.Unit == BitSyntaxDefaults.BinaryUnit
                && index == pattern.Segments.Count - 1 && variable.Name == result.Name && capture == 0 && total > 0)
            {
                return new(total, 0, true);
            }
            if (spec.Type != BitSegmentTypes.Integer || spec.Unit != BitSyntaxDefaults.NumericUnit)
            {
                return null;
            }
            int size = spec.Size switch
            {
                null => 8,
                Expr.Literal { Value: Integer bits } when bits.Value > 0 && bits.Value <= 64 => (int)bits.Value,
                _ => 0
            };
            if (size == 0)
            {
                return null;
            }
            if (variable.Name == result.Name && index == 0)
            {
                capture = size;
            }
            else if (variable.Name != VariableScopeNames.Wildcard)
            {
                return null;
            }
            total += size;
        }

        return capture > 0 ? new(total, capture, false) : null;
    }

    private void EmitBinaryCapture(BinaryCapture pattern)
    {
        int functionInfo = nextLabel - 2;
        int restore = nextLabel++;
        Op(StartBinaryMatch, LabelTag, functionInfo);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(XRegisterTag, 1);
        Op(GetBinaryPosition, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Op(BinaryMatch, LabelTag, restore);
        Operand(XRegisterTag, 1);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, pattern.Tail ? 9 : 8);
        Operand(AtomTag, Intern(pattern.Tail ? "ensure_at_least" : "ensure_exactly"));
        Operand(BeamWire.UnsignedTag, pattern.TotalBits);
        if (pattern.Tail)
        {
            Operand(BeamWire.UnsignedTag, 8);
            Operand(AtomTag, Intern("skip"));
            Operand(BeamWire.UnsignedTag, pattern.TotalBits);
            Operand(AtomTag, Intern("get_tail"));
            Operand(BeamWire.UnsignedTag, 2);
            Operand(BeamWire.UnsignedTag, 8);
            Operand(XRegisterTag, 0);
        }
        else
        {
            Operand(AtomTag, Intern("integer"));
            Operand(BeamWire.UnsignedTag, 2);
            Operand(AtomTag, NilAtomIndex);
            Operand(BeamWire.UnsignedTag, pattern.CaptureBits);
            Operand(BeamWire.UnsignedTag, 1);
            Operand(XRegisterTag, 2);
            MoveToX0(XRegisterTag, 2);
        }
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, restore);
        Op(SetBinaryPosition, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Op(GetBinaryTail, XRegisterTag, 1);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Op(Jump, LabelTag, functionInfo);
    }
}
