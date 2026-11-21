using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitUnpaddedBitStringContext(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Variable inputTags]
                        } collector,
                        Clauses:
                                [
                                {
                                    Guard: null, Patterns: [BitPattern { Segments: [var zero] }],
                                    Body: var empty
                                },
                                {
                                    Guard: null, Patterns: [BitPattern { Segments: [var count, var data] }],
                                    Body: Expr.Sequence
                                    {
                                        Items:
                                                [
                                                    Expr.Match
                                                {
                                                    Pattern: Pattern.Variable size,
                                                    Value: Expr.Binary
                                                    {
                                                        Operator: ErlangOperators.Minus,
                                                        Left: Expr.Call
                                                        {
                                                            Function: AutoImportNames.BitSize, DynamicRemote: false,
                                                            Arguments: [Expr.Variable measured]
                                                        } bitSize,
                                                        Right: Expr.Variable unused
                                                    } subtract
                                                },
                                                    Expr.Match
                                                {
                                                    Pattern: BitPattern { Segments: [var payload, var suffix] },
                                                    Value: Expr.Variable matched
                                                } partition,
                                                    Expr.Variable result
                                                ]
                                    }
                                }
                                ]
                    } decision
                }
                ]
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyBinary)
                || emptyBinary is not BitString { BitLength: 0 }
                || input.Name != source.Name || tags.Name != inputTags.Name
                || !owner.ResolvesErlangBif(bitSize) || !entries.TryGetValue((collector.Function, 2), out int entry)
                || !IsUnsignedByte(zero) || zero.Value is not Pattern.Literal { Value: Integer zeroValue } || !zeroValue.Value.IsZero
                || !IsUnsignedByte(count) || count.Value is not Pattern.Variable padding
                || data is not
                {
                    Value: Pattern.Variable tail,
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null }
                }
                || payload is not
                {
                    Value: Pattern.Variable value,
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BitStringUnit, Size: Expr.Variable bits }
                }
                || suffix is not
                {
                    Value: Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BitStringUnit, Size: Expr.Variable pad }
                }
                || !DistinctBindings(
                    input,
                    tags,
                    size,
                    padding,
                    tail,
                    value
                )
                || measured.Name != tail.Name || matched.Name != tail.Name || unused.Name != padding.Name
                || bits.Name != size.Name || pad.Name != padding.Name || result.Name != value.Name
                || !ProvesByteAlignedResult(collector, new HashSet<(string, int)>()))
            {
                return false;
            }

            int nonempty = Label();
            int invalidPadding = Label();
            int invalidPrefix = Label();
            EmitFrameAllocation(arity: 2, size: 0);
            owner.EmitCallLine(collector, location);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            EmitBitMatchStart(1, invalidPrefix, knownBinary: true);
            EmitBitMatchHeader(
                1,
                invalidPrefix,
                9,
                BitSyntaxDefaults.BinaryUnit,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                count.Specification,
                BitSyntaxDefaults.IntegerWidth,
                2,
                XRegisterTag,
                0
            );
            owner.Op(GetBinaryPosition, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(ExactEqualTest, LabelTag, nonempty);
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.LiteralOperand(zeroValue);
            EmitBitMatchHeader(
                1,
                nonempty,
                2,
                0,
                tailUnit: null
            );
            owner.EmitConstant(new BitString([]), 0);
            owner.EmitFrameReturn(0);

            Mark(nonempty);
            var previous = CallLocation(collector, location);
            owner.EmitCallLine(bitSize, previous);
            previous = CallLocation(bitSize, previous);
            EmitSizeInstruction(bitSize.Function, 0, 3);
            owner.EmitTypedRegister(1, MatchableTypeBits);
            owner.Operand(XRegisterTag, 3);
            owner.EmitArithmeticLine(subtract.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            owner.EmitTypedRegister(
                3,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.Operand(XRegisterTag, 3);
            owner.Op(GetBinarySegment, LabelTag, invalidPadding);
            owner.EmitTypedRegister(1, MatchableTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.EmitTypedRegister(
                3,
                IntegerTypeBits,
                -byte.MaxValue,
                MaximumBitSize
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 3);
            owner.Op(SkipBinaryBits, LabelTag, invalidPadding);
            owner.EmitTypedRegister(1, MatchableTypeBits);
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitBitMatchHeader(
                1,
                invalidPadding,
                2,
                0,
                tailUnit: null
            );
            owner.MoveToX0(XRegisterTag, 3);
            owner.EmitFrameReturn(0);

            Mark(invalidPadding);
            owner.Op(SetBinaryPosition, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Op(GetBinaryTail, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            previous = location;
            owner.EmitArithmeticLine(partition.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            Mark(invalidPrefix);
            previous = location;
            owner.EmitArithmeticLine(decision.SourceLine, ref previous);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;

            static bool IsUnsignedByte(BitPatternSegment segment) => segment.Specification is
            {
                Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit, Endian: BitByteOrders.Big, Signed: false
            } specification && FixedSize(specification) == BitSyntaxDefaults.IntegerWidth;
        }

        private bool ProvesByteAlignedResult(
            Expr expression,
            HashSet<(string Name, int Arity)> visiting,
            string? guarded = null
        )
        {
            switch (expression)
            {
                case Expr.Bits bits when AttributeLiteral.TryNormalizeConstant(bits, out Term value):
                    return value is BitString { IsBinary: true };
                case Expr.Literal { Value: BitString { IsBinary: true } }:
                    return true;
                case Expr.Variable variable:
                    return variable.Name == guarded;
                case Expr.Block block:
                    return ProvesByteAlignedResult(block.Body, visiting, guarded);
                case Expr.Sequence { Items: [.., var last] }:
                    return ProvesByteAlignedResult(last, visiting, guarded);
                case Expr.Case choice:
                    return choice.Clauses.All(branch => ProvesByteAlignedResult(branch.Body, visiting, BinaryGuard(branch.Guard) ?? guarded));
                case Expr.Call
                {
                    DynamicRemote: false, Function: AutoImportNames.ListToBinary or AutoImportNames.IolistToBinary,
                    Arguments.Count: 1
                } native when owner.ResolvesErlangBif(native):
                    return true;
                case Expr.Call { Module: null, DynamicRemote: false } local:
                    {
                        var signature = (local.Function, local.Arguments.Count);
                        if (owner.IsDeclaredNative(signature) || !localFunctions.TryGetValue(signature, out var target))
                        {
                            return false;
                        }

                        if (!visiting.Add(signature))
                        {
                            return true;
                        }

                        bool proven = target.Clauses.All(branch => ProvesByteAlignedResult(branch.Body, visiting, BinaryGuard(branch.Guard)));
                        visiting.Remove(signature);

                        return proven;
                    }
                default:
                    return false;
            }

            string? BinaryGuard(Expr? guard) => guard is Expr.Call
            {
                Function: AutoImportNames.IsBinary, DynamicRemote: false, Arguments: [Expr.Variable argument]
            } test && owner.ResolvesErlangBif(test) ? argument.Name : null;
        }
    }
}
