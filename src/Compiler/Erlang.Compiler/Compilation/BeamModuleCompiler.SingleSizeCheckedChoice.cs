using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingleSizeCheckedChoice(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable selector, Pattern.Variable payload, ..] and var parameters,
                        Body: Expr.Case
                        {
                            Value: Expr.Variable input,
                            Clauses:
                                [
                                {
                                    Guard: null,
                                    Patterns: [Pattern.Literal { Value: Atom key }],
                                    Body: var body
                                }
                                ]
                        } choice
                    }
                ]
                || selector.Name != input.Name || !DistinctBindings(selector, payload)
                || parameters.Skip(2).Any(parameter => parameter is not Pattern.Variable { })
                || !HasUnconditionalParameters(function.Clauses[0]))
            {
                return false;
            }

            var bound = parameters.Cast<Pattern.Variable>().Select(parameter => parameter.Name)
                .ToHashSet(StringComparer.Ordinal);
            if (ReadCheckedFieldSize(body, bound, out Expr decoder) is not { } size
                || ReadDecodeCall(decoder, payload.Name) is not { Convert: null } decode)
            {
                return false;
            }

            int rejected = Label();
            int wrongKey = Label();
            owner.Op(ExactEqualTest, LabelTag, wrongKey);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(key);
            EmitFrameAllocation(arity: 2, size: 0);
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitConstant(decode.Tags, 1);
            EmitDecodeInvocation(decode, location);
            var previous = CallLocation(decode.Decode, location);
            EmitCheckedFieldMeasurement(size.Measure, previous);
            owner.Op(ExactEqualTest, LabelTag, rejected);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.LiteralOperand(size.Count);
            owner.EmitFrameReturn(0);
            Mark(rejected);
            owner.EmitConstant(size.Reason, 0);
            owner.EmitCallLine(size.Failure, previous);
            EmitCheckedDecoderExit(size.Failure, 0);
            Mark(wrongKey);
            previous = location;
            owner.EmitArithmeticLine(choice.SourceLine, ref previous);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }
    }
}
