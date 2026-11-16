using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int BinaryComprehensionInitialCapacity = 256;

        private void EmitBinaryComprehension(Expr.BinaryComprehension comprehension)
        {
            Expr.Bits output = comprehension.Body as Expr.Bits ?? new Expr.Bits([
                new BitSegment(comprehension.Body, null, BitSegmentTypes.Binary, BitSyntaxDefaults.BitStringUnit)
            ]);
            owner.EmitConstant(Term.I(BinaryComprehensionInitialCapacity), 0);
            owner.instructions.WriteByte(InitializeWritableBinary);
            int accumulator = Save();
            EmitComprehensionQualifiers(comprehension.Qualifiers, [output], () =>
            {
                EmitBits(output, failure: null, writablePrefix: accumulator);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, accumulator);
            });
            Load(accumulator);
        }
    }
}
