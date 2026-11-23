using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ForwardedChoiceMeasurement(
            DecodeCall Decoder,
            Expr.Call Measure,
            Integer Lower,
            Integer Upper,
            bool Exact,
            Expr.Call? Convert,
            Expr.Call Exit,
            Term Reason
        );

        private ForwardedChoiceMeasurement? ReadForwardedChoiceMeasurement(
            Expr expression,
            string field,
            string prepared
        )
        {
            if (expression is not Expr.Block
                {
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable value, Value: var input },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable count,
                            Value: Expr.Call
                            {
                                Function: AutoImportNames.ByteSize or AutoImportNames.Length,
                                DynamicRemote: false,
                                Arguments: [Expr.Variable measured]
                            } measure
                        },
                            Expr.If
                        {
                            Clauses:
                                [
                                { Guard: { } guard, Body: var accepted },
                                {
                                    Guard: Expr.Literal { Value: Atom { Name: "true" } },
                                    Body: Expr.Call
                                    {
                                        Function: AutoImportNames.Exit, DynamicRemote: false, Arguments: [var reason]
                                    } exit
                                }
                                ]
                        }
                        ]
                    }
                }
                || !DistinctBindings(value, count) || value.Name != measured.Name
                || value.Name == field || count.Name == field || value.Name == prepared || count.Name == prepared
                || !IsDecoderIntrinsic(measure) || !IsDecoderIntrinsic(exit)
                || ReadDecodeCall(input, field) is not { Convert: null } decoder
                || !AttributeLiteral.TryNormalizeConstant(reason, out Term error))
            {
                return null;
            }

            Expr.Call? conversion = null;
            if (accepted is not Expr.Variable result || result.Name != value.Name)
            {
                if (accepted is not Expr.Call
                    {
                        Function: AutoImportNames.BinaryToList, DynamicRemote: false, Arguments: [Expr.Variable source]
                    } convert
                    || source.Name != value.Name || !IsDecoderIntrinsic(convert))
                {
                    return null;
                }

                conversion = convert;
            }

            Integer lower;
            Integer upper;
            bool exact = guard is Expr.Binary { Operator: ErlangOperators.ExactEqual };
            if (guard is Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable tested,
                    Right: Expr.Literal { Value: Integer expected }
                } && tested.Name == count.Name)
            {
                lower = expected;
                upper = expected;
            }
            else if (guard is Expr.Binary
            {
                Operator: ErlangOperators.AndAlso,
                Left: Expr.Binary
                {
                    Operator: ErlangOperators.LessOrEqual,
                    Left: Expr.Literal { Value: Integer minimum },
                    Right: Expr.Variable first
                },
                Right: Expr.Binary
                {
                    Operator: ErlangOperators.LessOrEqual,
                    Left: Expr.Variable second,
                    Right: Expr.Literal { Value: Integer maximum }
                }
            } && first.Name == count.Name && second.Name == count.Name)
            {
                lower = minimum;
                upper = maximum;
            }
            else
            {
                return null;
            }

            bool binary = measure.Function == AutoImportNames.ByteSize;
            if (lower.Value < 0 || upper.Value < lower.Value || upper.Value >= MaximumBitSize
                || !ProvesForwardedMeasurementInput(decoder.Decode, binary))
            {
                return null;
            }

            return new(
                decoder,
                measure,
                lower,
                upper,
                exact,
                conversion,
                exit,
                error
            );
        }

        private bool ProvesForwardedMeasurementInput(Expr.Call initial, bool binary) =>
            initial is { Module: null, DynamicRemote: false }
            && owner.ProvesMeasurementReturn((initial.Function, initial.Arguments.Count), binary, localFunctions);

        private void EmitForwardedChoiceMeasurement(
            Atom tag,
            ForwardedChoiceMeasurement measured,
            int failure,
            BeamSourceLocation previous
        )
        {
            previous = EmitForwardedChoiceMeasurementCheck(measured, failure, CallLocation(measured.Decoder.Decode, previous));
            EmitTaggedRegisterResult(tag);
            owner.EmitFrameReturn(0);
            Mark(failure);
            owner.EmitConstant(measured.Reason, 0);
            owner.EmitCallLine(measured.Exit, previous);
            EmitCheckedDecoderExit(measured.Exit, 0);
        }

        private void EmitPreparedMeasurementTail(
            Atom selector,
            Expr.Call prepare,
            int preparation,
            ForwardedChoiceMeasurement measurement,
            Expr.Call conversion
        )
        {
            owner.EmitAtomTest(0, selector.Name, owner.nextLabel - 2);
            EmitFrameAllocation(2, 0);
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitCallLine(prepare, location);
            owner.EmitFieldCall(1, preparation);
            BeamSourceLocation previous = CallLocation(prepare, location);
            EmitDecodeCall(measurement.Decoder, previous);
            int failure = Label();
            previous = EmitForwardedChoiceMeasurementCheck(
                measurement,
                failure,
                CallLocation(measurement.Decoder.Decode, previous),
                emitConversion: false
            );

            owner.EmitCallLine(conversion, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, conversion.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(failure);
            owner.EmitConstant(measurement.Reason, 0);
            owner.EmitCallLine(measurement.Exit, previous);
            EmitCheckedDecoderExit(measurement.Exit, 0);
        }

        private BeamSourceLocation EmitForwardedChoiceMeasurementCheck(
            ForwardedChoiceMeasurement measured,
            int failure,
            BeamSourceLocation previous,
            bool emitConversion = true
        )
        {
            owner.EmitCallLine(measured.Measure, previous);
            previous = CallLocation(measured.Measure, previous);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, measured.Measure.Function, 1));
            if (measured.Measure.Function == AutoImportNames.ByteSize)
            {
                owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BinaryUnit);
            }
            else
            {
                owner.EmitTypedRegister(0, ProperListTypeBits);
            }

            owner.Operand(XRegisterTag, 1);
            long lower = (long)measured.Lower.Value;
            if (measured.Exact || lower != 0)
            {
                owner.Op(measured.Exact ? ExactEqualTest : IsGreaterOrEqual, LabelTag, failure);
                owner.EmitTypedRegister(
                    1,
                    IntegerTypeBits,
                    0,
                    MaximumBitSize
                );
                owner.LiteralOperand(measured.Lower);
            }

            if (!measured.Exact)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, failure);
                owner.LiteralOperand(measured.Upper);
                owner.EmitTypedRegister(
                    1,
                    IntegerTypeBits,
                    lower,
                    MaximumBitSize
                );
            }

            if (emitConversion && measured.Convert is { } conversion)
            {
                owner.EmitCallLine(conversion, previous);
                Call(ErlangModuleNames.Module, conversion.Function, 1);
            }

            return previous;
        }
    }
}
