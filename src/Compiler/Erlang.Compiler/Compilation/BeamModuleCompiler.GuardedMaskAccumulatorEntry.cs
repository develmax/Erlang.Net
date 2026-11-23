using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedMaskAccumulatorEntry(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable shortInput],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.LessOrEqual,
                        Left: Expr.Variable tested, Right: Expr.Literal { Value: Integer limit }
                    },
                    Body: Expr.Tuple
                    {
                        Items:
                            [
                                Expr.List
                            {
                                Tail: null,
                                Items:
                                    [
                                        Expr.Binary
                                    {
                                        Operator: ErlangOperators.BitwiseAnd,
                                        Left: Expr.Literal { Value: Integer shortMask }, Right: Expr.Variable shortMasked
                                    } bounded
                                    ]
                            },
                                Expr.Literal { Value: Integer shortCount }
                            ]
                    }
                },
                {
                    Patterns: [Pattern.Variable longInput],
                    Guard: null,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Binary
                            {
                                Operator: ErlangOperators.ShiftRight,
                                Left: Expr.Variable shifted, Right: Expr.Literal { Value: Integer width }
                            } shift,
                                Expr.List
                            {
                                Tail: null,
                                Items:
                                    [
                                        Expr.Binary
                                    {
                                        Operator: ErlangOperators.BitwiseAnd,
                                        Left: Expr.Variable longMasked, Right: Expr.Literal { Value: Integer longMask }
                                    } mask
                                    ]
                            },
                                Expr.Literal { Value: Integer longCount }
                            ]
                    } continuation
                }
                ]
                || shortInput.Name == VariableScopeNames.Wildcard || longInput.Name == VariableScopeNames.Wildcard
                || tested.Name != shortInput.Name || shortMasked.Name != shortInput.Name
                || shifted.Name != longInput.Name || longMasked.Name != longInput.Name
                || limit.Value < 0 || limit.Value > int.MaxValue || width.Value <= 0 || width.Value > int.MaxValue
                || shortMask.Value < 0 || shortMask.Value > int.MaxValue || longMask.Value < 0 || longMask.Value > int.MaxValue
                || shortCount.Value <= 0 || shortCount.Value > int.MaxValue || longCount.Value <= 0 || longCount.Value > int.MaxValue
                || !entries.TryGetValue((continuation.Function, 3), out int entry))
            {
                return false;
            }

            // The term comparison gives a numeric upper bound on success.
            // Failure still admits non-numbers, so the shift keeps the any type
            // with its numeric lower bound. The following mask sees integers.
            long threshold = (long)limit.Value;
            int advance = Label();
            owner.Op(IsGreaterOrEqual, LabelTag, advance);
            owner.LiteralOperand(limit);
            owner.Operand(XRegisterTag, 0);

            BeamSourceLocation previous = location;
            EmitOperation(
                bounded,
                shortMask,
                1,
                0,
                NumberTypeBits,
                null,
                threshold
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSingletonList();
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (BeamWire.IntegerTag, (int)shortCount.Value));
            owner.instructions.WriteByte(Return);

            Mark(advance);
            previous = location;
            EmitOperation(
                shift,
                width,
                1,
                1,
                AnyTypeBits,
                threshold
            );
            EmitOperation(
                mask,
                longMask,
                2,
                0,
                IntegerTypeBits,
                threshold
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitSingletonList();
            owner.EmitConstant(longCount, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);

            return true;

            void EmitOperation(
                Expr.Binary operation,
                Integer constant,
                int live,
                int result,
                int type,
                long? lower,
                long? upper = null
            )
            {
                owner.EmitArithmeticLine(operation.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
                owner.EmitTypedRegister(
                    0,
                    type,
                    lower,
                    upper
                );
                owner.LiteralOperand(constant);
                owner.Operand(XRegisterTag, result);
            }

            void EmitSingletonList()
            {
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(AtomTag, NilAtomIndex);
                owner.Operand(XRegisterTag, 0);
            }
        }
    }
}
