using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitIgnoredTryCallExactCatch(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom shortcut }], Body: Expr.Literal immediate
                },
                {
                    Guard: null, Patterns: [Pattern.Variable argument],
                    Body: Expr.Try
                    {
                        Body: Expr.Call
                        {
                            DynamicRemote: false, Arguments: [Expr.Variable input, ..]
                        } call,
                        Clauses:
                                [
                                {
                                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Body: Expr.Literal success
                                }
                                ],
                        Catches:
                                [
                            {
                                Guard: null,
                                Patterns:
                                            [
                                                Pattern.Tuple
                                            {
                                                Items:
                                                        [
                                                            Pattern.Literal { Value: Atom kind },
                                                            Pattern.Literal reason,
                                                            Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                                        ]
                                            }
                                            ],
                                Body: Expr.Literal known
                            }
                                ],
                        After: null
                    }
                }
                ]
                || argument.Name == VariableScopeNames.Wildcard || argument.Name != input.Name
                || call.Arguments.Count >= FirstReservedXRegister
                || !TryResolveOrdinaryCallTarget(call, out string? module))
            {
                return false;
            }

            var constants = new List<(int Destination, Term Value)>();
            for (int index = 1; index < call.Arguments.Count; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[index], out Term value))
                {
                    return false;
                }

                constants.Add((index, value));
            }

            // The protected call consumes its input immediately. Its
            // value is ignored and the two exact catch operands remain in the
            // exception registers while choosing a result or reraising.
            int protectedPath = Label();
            int caught = Label();
            int reraised = Label();
            owner.Op(ExactEqualTest, LabelTag, protectedPath);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(shortcut);
            owner.EmitConstant(immediate.Value, 0);
            owner.instructions.WriteByte(Return);

            Mark(protectedPath);
            EmitFrameAllocation(arity: 1, size: 1);
            owner.Op(TryEnter, YRegisterTag, 0);
            owner.Operand(LabelTag, caught);
            owner.EmitIndependentCallMoves(constants);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, call.Arguments.Count);
            owner.Op(TryEnd, YRegisterTag, 0);
            owner.EmitConstant(success.Value, 0);
            owner.EmitFrameReturn(1);

            Mark(caught);
            owner.Op(TryCase, YRegisterTag, 0);
            owner.Op(ExactEqualTest, LabelTag, reraised);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(kind);
            owner.Op(ExactEqualTest, LabelTag, reraised);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(reason.Value);
            owner.EmitConstant(known.Value, 0);
            owner.EmitFrameReturn(1);

            Mark(reraised);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            return true;
        }
    }
}
