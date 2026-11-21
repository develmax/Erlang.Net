using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record MeasuredBinaryDecodePlan(
            Expr.Case Decision,
            Expr.Call Collect,
            Expr.Call Decode,
            Expr.Call Measure,
            BitPatternSegment First,
            Term Empty,
            Integer Expected,
            int CollectEntry,
            int DecodeEntry,
            int FinishEntry
        );

        private MeasuredBinaryDecodePlan? ReadMeasuredBinaryDecode(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable buffer, Pattern.Variable saved, Pattern.Variable tags],
                        Body: Expr.Case
                        {
                            Value: Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [Expr.Variable input, Expr.Variable passedTags]
                            } collect,
                            Clauses: [
                            {
                                Guard: null,
                                Patterns: [BitPattern { Segments: [var single] }],
                                Body: var empty
                            },
                            {
                                Guard: null,
                                Patterns: [BitPattern { Segments: [var first, var tail] }],
                                Body: Expr.Sequence
                                {
                                    Items: [
                                            Expr.Match
                                    {
                                        Pattern: Pattern.Variable decoded,
                                        Value: Expr.Call
                                        {
                                            Module: null, DynamicRemote: false,
                                            Arguments: [
                                                        Expr.Call
                                            {
                                                Function: AutoImportNames.ByteSize, DynamicRemote: false,
                                                Arguments: [Expr.Variable measured]
                                            } measure,
                                                        Expr.Variable prefixArgument,
                                                        Expr.Variable tailArgument
                                                    ]
                                        } decode
                                    },
                                            Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable result, Expr.Variable savedArgument]
                                    } finish
                                        ]
                                }
                            }
                            ]
                        } decision
                    }
                ]
                || input.Name != buffer.Name || passedTags.Name != tags.Name || savedArgument.Name != saved.Name
                || result.Name != decoded.Name || !owner.ResolvesErlangBif(measure)
                || !IsByte(single.Specification) || !IsByte(first.Specification)
                || single.Value is not Pattern.Literal { Value: Integer expected }
                || expected.Value < byte.MinValue || expected.Value > byte.MaxValue
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyResult) || emptyResult is not (Nil or Atom or BitString)
                || first.Value is not Pattern.Variable prefix
                || tail is not
                {
                    Value: Pattern.Variable suffix,
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null }
                }
                || !DistinctBindings(
                    buffer,
                    saved,
                    tags,
                    prefix,
                    suffix,
                    decoded
                )
                || measured.Name != suffix.Name || prefixArgument.Name != prefix.Name || tailArgument.Name != suffix.Name
                || !ProvesByteAlignedResult(collect, [])
                || !entries.TryGetValue((collect.Function, 2), out int collectEntry)
                || !entries.TryGetValue((decode.Function, 3), out int decodeEntry)
                || !entries.TryGetValue((finish.Function, 2), out int finishEntry))
            {
                return null;
            }

            return new(
                decision,
                collect,
                decode,
                measure,
                first,
                emptyResult,
                expected,
                collectEntry,
                decodeEntry,
                finishEntry
            );
        }

        private bool TryEmitMeasuredBinaryContextDecode(FunctionDefinition function)
        {
            if (ReadMeasuredBinaryDecode(function) is not { } plan
                || !localFunctions.TryGetValue((plan.Decode.Function, 3), out var target)
                || ReadResumedBitListDecode(target) is null)
            {
                return false;
            }

            int nonempty = Label();
            int invalid = Label();
            var knownTags = ReadClosedSingletonArgument(function, 2);
            EmitFrameAllocation(knownTags is null ? 3 : 2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            if (knownTags is null)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    XRegisterTag,
                    1
                );
            }
            else
            {
                owner.EmitConstant(knownTags, 1);
            }


            owner.EmitCallLine(plan.Collect, location);
            owner.EmitFieldCall(2, plan.CollectEntry);
            EmitBitMatchStart(1, invalid, knownBinary: true);
            EmitBitMatchHeader(
                1,
                invalid,
                9,
                BitSyntaxDefaults.BinaryUnit,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                plan.First.Specification,
                BitSyntaxDefaults.IntegerWidth,
                2,
                XRegisterTag,
                0
            );
            owner.Op(ExactEqualTest, LabelTag, nonempty);
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.LiteralOperand(plan.Expected);
            EmitExactBinaryTail(nonempty);
            owner.EmitConstant(plan.Empty, 0);
            owner.EmitFrameReturn(1);

            Mark(nonempty);
            var previous = CallLocation(plan.Collect, location);
            owner.EmitCallLine(plan.Measure, previous);
            EmitSizeInstruction(plan.Measure.Function, failure: 0, live: 2);
            owner.EmitTypedRegister(1, MatchableTypeBits);
            owner.Operand(XRegisterTag, 2);
            owner.Op(Swap, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(plan.Decode, CallLocation(plan.Measure, previous));
            owner.EmitFieldCall(3, plan.DecodeEntry);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            EmitTailLocal(plan.FinishEntry, 2, 1);
            EmitBinaryCaseFailure(plan.Decision, invalid, 0);

            return true;
        }
    }
}
