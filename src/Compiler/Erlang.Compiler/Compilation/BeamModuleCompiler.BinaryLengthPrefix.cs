using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record BinaryLengthPrefix(
            BitSegment Marker,
            BitSegment ShortLength,
            BitSegment LongWidth,
            Atom Indefinite,
            int Width
        );

        private bool TryEmitBinaryLengthPrefix(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || recursiveClosure is not null || ReadBinaryLengthPrefix(function) is not { } plan
                || !HasBinaryLocalArgument(function, binaryRecursion: null, trackBinaryPairResults: true))
            {
                return false;
            }

            const int context = 0;
            const int original = 1;
            const int length = 2;
            const int payload = 3;
            int functionInfo = owner.nextLabel - 2;
            int extended = Label();
            int definite = Label();
            int invalid = Label();
            long maximum = (1L << plan.Width) - 1;

            EmitBitMatchStart(context, functionInfo, knownBinary: true);
            SavePosition(original, live: 1);
            EmitBitMatchHeader(
                context,
                invalid,
                9,
                1,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                plan.Marker,
                1,
                2,
                XRegisterTag,
                length
            );
            owner.Op(ExactEqualTest, LabelTag, extended);
            owner.Operand(XRegisterTag, length);
            owner.LiteralOperand(Term.I(0));

            EmitBitMatchHeader(
                context,
                invalid,
                13,
                plan.Width,
                BitSyntaxDefaults.BinaryUnit
            );
            EmitBitSegment(
                plan.ShortLength,
                plan.Width,
                1,
                XRegisterTag,
                original
            );
            EmitBinaryLengthTail(live: 2);
            EmitLengthResult(original, live: 2);
            Mark(extended);
            EmitBitMatchHeader(
                context,
                invalid,
                9,
                plan.Width,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                plan.LongWidth,
                plan.Width,
                2,
                XRegisterTag,
                length
            );
            SavePosition(payload, live: 3);
            owner.Op(ExactEqualTest, LabelTag, definite);
            owner.EmitTypedRegister(
                length,
                IntegerTypeBits,
                0,
                maximum
            );
            owner.LiteralOperand(Term.I(0));
            EmitBinaryPrefixSuffix(
                context,
                definite,
                0,
                tailLive: 1
            );
            EmitTaggedRegisterResult(plan.Indefinite);
            owner.instructions.WriteByte(Return);

            Mark(definite);
            RestorePosition(payload);
            owner.Op(GetBinaryInteger, LabelTag, invalid);
            owner.EmitTypedRegister(context, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTypedRegister(
                length,
                IntegerTypeBits,
                0,
                maximum
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, length);
            EmitBinaryPrefixSuffix(
                context,
                invalid,
                0,
                tailLive: 3
            );
            EmitLengthResult(length, live: 3);

            Mark(invalid);
            RestorePosition(original);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, context);
            owner.Operand(BeamWire.UnsignedTag, 1);
            JumpTo(functionInfo);

            return true;

            void SavePosition(int slot, int live)
            {
                owner.Op(GetBinaryPosition, XRegisterTag, context);
                owner.Operand(XRegisterTag, slot);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void RestorePosition(int slot)
            {
                owner.Op(SetBinaryPosition, XRegisterTag, context);
                owner.Operand(XRegisterTag, slot);
            }

            void EmitLengthResult(int slot, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.Operand(XRegisterTag, slot);
                owner.Operand(XRegisterTag, context);
                owner.instructions.WriteByte(Return);
            }

            void EmitBinaryLengthTail(int live)
            {
                owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
                owner.Operand(XRegisterTag, context);
            }
        }

        private static BinaryLengthPrefix? ReadBinaryLengthPrefix(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments: [var indefiniteMarker, var indefiniteWidth, var indefiniteTail] }],
                    Body: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom tag }, Expr.Variable indefiniteRest]
                    }
                },
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments: [var shortMarker, var shortWidth, var shortTail] }],
                    Body: Expr.Tuple { Items: [Expr.Variable shortResult, Expr.Variable shortRest] }
                },
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments: [var longMarker, var longWidth, var longNumber, var longTail] }],
                    Body: Expr.Tuple { Items: [Expr.Variable longResult, Expr.Variable longRest] }
                }
                ]
                || !ReadMarker(indefiniteMarker, 1) || !ReadMarker(shortMarker, 0) || !ReadMarker(longMarker, 1)
                || !IsUnsignedLength(indefiniteWidth.Specification, BitSyntaxDefaults.NumericUnit)
                || indefiniteWidth.Value is not Pattern.Literal { Value: Integer zero } || !zero.Value.IsZero
                || !IsUnsignedLength(shortWidth.Specification, BitSyntaxDefaults.NumericUnit)
                || !IsUnsignedLength(longWidth.Specification, BitSyntaxDefaults.NumericUnit)
                || FixedSize(indefiniteWidth.Specification) is not int width || width + 1 != BitSyntaxDefaults.BinaryUnit
                || FixedSize(shortWidth.Specification) != width || FixedSize(longWidth.Specification) != width
                || shortWidth.Value is not Pattern.Variable collected || collected.Name != shortResult.Name
                || longWidth.Value is not Pattern.Variable count || longNumber.Value is not Pattern.Variable number
                || number.Name != longResult.Name
                || !IsUnsignedLength(longNumber.Specification, BitSyntaxDefaults.BinaryUnit)
                || longNumber.Specification.Size is not Expr.Variable measured || measured.Name != count.Name
                || ReadLengthTail(indefiniteTail, indefiniteRest) is null
                || ReadLengthTail(shortTail, shortRest) is not { } shortRemainder
                || ReadLengthTail(longTail, longRest) is not { } longRemainder
                || !DistinctBindings(collected, shortRemainder) || !DistinctBindings(count, number, longRemainder))
            {
                return null;
            }

            return new(
                shortMarker.Specification,
                shortWidth.Specification,
                longWidth.Specification,
                tag,
                width
            );

            static bool ReadMarker(BitPatternSegment marker, int expected) =>
                marker.Value is Pattern.Literal { Value: Integer value } && value.Value == expected
                    && IsUnsignedLength(marker.Specification, BitSyntaxDefaults.NumericUnit)
                    && FixedSize(marker.Specification) == 1;

            static bool IsUnsignedLength(BitSegment segment, int unit) =>
                segment is { Type: BitSegmentTypes.Integer, Signed: false, Endian: BitByteOrders.Big } && segment.Unit == unit;

            static Pattern.Variable? ReadLengthTail(BitPatternSegment segment, Expr.Variable result) =>
                segment is
                {
                    Value: Pattern.Variable tail,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit }
                } && tail.Name == result.Name && tail.Name != VariableScopeNames.Wildcard ? tail : null;
        }
    }
}
