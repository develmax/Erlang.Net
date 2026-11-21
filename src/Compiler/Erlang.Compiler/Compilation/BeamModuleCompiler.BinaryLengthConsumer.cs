using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record BinaryLengthConsumer(
            Expr.Call Decode,
            Expr.Call Forward,
            Atom Marker,
            Atom ResultTag,
            Expr.Match Split,
            int Reader,
            int Target
        );

        private bool TryEmitBinaryLengthConsumer(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || recursiveClosure is not null || ReadBinaryLengthConsumer(function) is not { } plan)
            {
                return false;
            }

            const int size = 1;
            const int context = 2;
            int definite = Label();
            int invalid = Label();
            EmitFrameAllocation(1, 0);
            owner.EmitCallLine(plan.Decode, location);
            owner.EmitFieldCall(1, plan.Reader);
            owner.EmitTupleElement(0, size);
            owner.EmitTupleElement(1, 0);
            owner.Op(ExactEqualTest, LabelTag, definite);
            owner.EmitTypedRegister(size, AtomTypeBits | IntegerTypeBits, lower: 0);
            owner.LiteralOperand(plan.Marker);
            EmitTailLocal(plan.Target, arity: 1, frame: 0);

            Mark(definite);
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern("no_fail"));
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, context);
            owner.Op(SkipBinaryBits, LabelTag, invalid);
            owner.EmitTypedRegister(context, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.EmitTypedRegister(size, IntegerTypeBits, lower: 0);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            EmitTaggedRegisterResult(plan.ResultTag);
            owner.EmitFrameReturn(0);

            Mark(invalid);
            var previous = CallLocation(plan.Decode, location);
            owner.EmitArithmeticLine(plan.Split.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private BinaryLengthConsumer? ReadBinaryLengthConsumer(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable input],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable source]
                        } decode,
                        Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom marker }, Pattern.Variable forwarded] }],
                                Body: Expr.Call
                                {
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable argument]
                                } forward
                            },
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Variable length, Pattern.Variable bytes] }],
                                Body: Expr.Sequence
                                {
                                    Items:
                                        [
                                            Expr.Match
                                        {
                                            Pattern: BitPattern
                                            {
                                                Segments:
                                                    [
                                                    {
                                                        Value: var ignored,
                                                        Specification:
                                                        {
                                                            Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.BinaryUnit,
                                                            Signed: false, Endian: BitByteOrders.Big, Size: Expr.Variable count
                                                        }
                                                    },
                                                    {
                                                        Value: Pattern.Variable remainder,
                                                        Specification:
                                                        {
                                                            Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null
                                                        }
                                                    }
                                                    ]
                                            },
                                            Value: Expr.Variable binary
                                        } split,
                                            Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable rest] }
                                        ]
                                }
                            }
                            ]
                    }
                }
                ]
                || source.Name != input.Name || argument.Name != forwarded.Name || count.Name != length.Name
                || binary.Name != bytes.Name || rest.Name != remainder.Name || !IsWildcard(ignored)
                || !DistinctBindings(input, forwarded)
                || !DistinctBindings(
                    input,
                    length,
                    bytes,
                    remainder
                )
                || !entries.TryGetValue((forward.Function, 1), out int target)
                || !entries.TryGetValue((decode.Function, 1), out int reader)
                || !localFunctions.TryGetValue((decode.Function, 1), out var decoder)
                || ReadBinaryLengthPrefix(decoder) is not { } plan || !plan.Indefinite.Equals(marker)
                || !HasBinaryLocalArgument(decoder, binaryRecursion: null, trackBinaryPairResults: true))
            {
                return null;
            }

            return new(
                decode,
                forward,
                marker,
                tag,
                split,
                reader,
                target
            );
        }
    }
}
