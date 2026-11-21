using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool HasBitStringInput(FunctionDefinition function)
        {
            if (owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return false;
            }

            bool hasGenericBitStringCaller = false;
            bool closed = HasOnlyLocalCalls(
                function,
                function.Arity,
                (caller, call) =>
            {
                if (AttributeLiteral.TryNormalizeConstant(call.Arguments[0], out Term literal))
                {
                    return literal is BitString;
                }

                if (call.Arguments[0] is not Expr.Variable input)
                {
                    return false;
                }

                foreach (var clause in caller.Clauses)
                {
                    if (!ReferenceEquals(clause.Body, call) || !clause.Patterns.Any(
                            pattern => pattern is Pattern.Variable parameter && parameter.Name == input.Name)
                        || clause.Guard is not Expr.Call
                        {
                            Function: ErlangModuleNames.IsBitString or ErlangModuleNames.IsBinary,
                            DynamicRemote: false,
                            Arguments: [Expr.Variable tested]
                        } guard
                        || tested.Name != input.Name || !owner.ResolvesErlangBif(guard))
                    {
                        continue;
                    }

                    hasGenericBitStringCaller |= guard.Function == ErlangModuleNames.IsBitString;

                    return true;
                }

                return false;
            }
            );

            return closed && hasGenericBitStringCaller;
        }

        private bool TryEmitPaddedBitStringTail(FunctionDefinition function)
        {
            const int Width = BitSyntaxDefaults.BinaryUnit;
            const int Mask = Width - 1;
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable options],
                    Body: Expr.Sequence
                    {
                        Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable padding,
                                    Value: Expr.Binary
                                    {
                                        Operator: ErlangOperators.BitwiseAnd,
                                        Left: Expr.Binary
                                        {
                                            Operator: ErlangOperators.Minus,
                                            Left: Expr.Literal { Value: Integer width },
                                            Right: Expr.Binary
                                            {
                                                Operator: ErlangOperators.BitwiseAnd,
                                                Left: Expr.Call
                                                {
                                                    Function: AutoImportNames.BitSize, DynamicRemote: false,
                                                    Arguments: [Expr.Variable measured]
                                                } bitSize,
                                                Right: Expr.Literal { Value: Integer innerMask }
                                            } inner
                                        } subtract,
                                        Right: Expr.Literal { Value: Integer outerMask }
                                    } pad
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable output,
                                    Value: Expr.Bits { Segments: [var header, var data, var zeroes] } bits
                                },
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments:
                                            [
                                                Expr.Variable tag, Expr.Variable binary,
                                                Expr.Call
                                            {
                                                Function: AutoImportNames.ByteSize, DynamicRemote: false,
                                                Arguments: [Expr.Variable encoded]
                                            } byteSize
                                            ]
                                } finish
                                ]
                    }
                }
                ]
                || !DistinctBindings(
                    input,
                    options,
                    padding,
                    output
                )
                || width.Value != Width || innerMask.Value != Mask || outerMask.Value != Mask
                || measured.Name != input.Name || tag.Name != options.Name || binary.Name != output.Name || encoded.Name != output.Name
                || !owner.ResolvesErlangBif(bitSize) || !owner.ResolvesErlangBif(byteSize)
                || !entries.TryGetValue((finish.Function, 3), out int entry)
                || header is not
                {
                    Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                    Endian: BitByteOrders.Big, Signed: false, Value: Expr.Variable count
                }
                || header.Size is not (null or Expr.Literal { Value: Integer })
                || header.Size is Expr.Literal { Value: Integer explicitSize } && explicitSize.Value != BitSyntaxDefaults.IntegerWidth
                || count.Name != padding.Name || data is not
                {
                    Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BitStringUnit, Size: null,
                    Value: Expr.Variable source
                } || source.Name != input.Name
                || zeroes is not
                {
                    Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                    Endian: BitByteOrders.Big, Signed: false,
                    Value: Expr.Literal { Value: Integer zero }, Size: Expr.Variable fill
                } || !zero.Value.IsZero || fill.Name != padding.Name)
            {
                return false;
            }

            var segments = ReadBinaryConstruction(bits);
            owner.EmitCallLine(bitSize, location);
            var previous = CallLocation(bitSize, location);
            EmitSizeInstruction(bitSize.Function, 0, 2);
            if (HasBitStringInput(function))
            {
                owner.EmitTypedRegister(0, MatchableTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
            owner.Operand(XRegisterTag, 2);
            owner.EmitArithmeticLine(inner.SourceLine, ref previous);
            EmitBif(ErlangOperators.BitwiseAnd);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.LiteralOperand(innerMask);
            owner.Operand(XRegisterTag, 2);
            owner.EmitArithmeticLine(subtract.SourceLine, ref previous);
            EmitBif(ErlangOperators.Minus);
            owner.LiteralOperand(width);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                0,
                Mask
            );
            owner.Operand(XRegisterTag, 2);
            owner.EmitArithmeticLine(pad.SourceLine, ref previous);
            EmitBif(ErlangOperators.BitwiseAnd);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                1,
                Width
            );
            owner.LiteralOperand(outerMask);
            owner.Operand(XRegisterTag, 2);

            EmitBinaryConstructionHeader(
                bits,
                segments,
                3,
                failure: null,
                previousLine: previous
            );
            EmitBinaryConstructionSegment(segments[0]);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                0,
                Mask
            );
            owner.LiteralOperand(((Expr.Literal)segments[0].Size).Value);
            EmitBinaryConstructionSegment(segments[1]);
            owner.EmitTypedRegister(0, MatchableTypeBits);
            owner.LiteralOperand(((Expr.Literal)segments[1].Size).Value);
            EmitBinaryConstructionSegment(segments[2]);
            owner.LiteralOperand(zero);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                0,
                Mask
            );

            if (bits.SourceLine != 0)
            {
                previous = previous with { Line = bits.SourceLine };
            }

            owner.EmitCallLine(byteSize, previous);
            EmitSizeInstruction(byteSize.Function, 0, 2);
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(XRegisterTag, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            EmitTailLocal(entry, 3, null);

            return true;

            void EmitBif(string operation)
            {
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation, 2));
            }
        }
    }
}
