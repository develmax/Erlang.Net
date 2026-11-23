using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLiteralOrProtectedResult(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Literal literal],
                    Body: Expr.Literal direct
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable input],
                    Body: Expr.Try
                    {
                        Body: Expr.Call call,
                        After: null,
                        Clauses:
                                [{ Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Literal success }],
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
                                                            Pattern.Literal { Value: Atom exceptionClass },
                                                            Pattern.Literal reason,
                                                            Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                                        ]
                                            }
                                            ],
                                Body: Expr.Literal caught
                            }
                                ]
                    }
                }
                ]
                || input.Name == VariableScopeNames.Wildcard
                || call.Arguments is not [Expr.Variable forwarded, Expr.Literal second, var third]
                || forwarded.Name != input.Name
                || !AttributeLiteral.TryNormalizeConstant(third, out Term constant)
                || !TryResolveOrdinaryCallTarget(call, out string? module)
                || module is not null || !entries.TryGetValue((call.Function, 3), out int entry))
            {
                return false;
            }

            int callLabel = Label();
            int handler = Label();
            int unmatched = Label();
            owner.Op(ExactEqualTest, LabelTag, callLabel);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(literal.Value);
            owner.EmitConstant(direct.Value, 0);
            owner.instructions.WriteByte(Return);

            Mark(callLabel);
            EmitFrameAllocation(arity: 1, size: 1);
            owner.Op(TryEnter, YRegisterTag, 0);
            owner.Operand(LabelTag, handler);
            owner.EmitConstant(constant, 2);
            owner.EmitConstant(second.Value, 1);
            owner.EmitCallLine(call, location);
            owner.EmitFieldCall(3, entry);
            owner.Op(TryEnd, YRegisterTag, 0);
            owner.EmitConstant(success.Value, 0);
            owner.EmitFrameReturn(1);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, 0);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(exceptionClass);
            owner.Op(ExactEqualTest, LabelTag, unmatched);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(reason.Value);
            owner.EmitConstant(caught.Value, 0);
            owner.EmitFrameReturn(1);

            Mark(unmatched);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            return true;
        }

        private bool TryEmitClosedIntegerTablePredicate(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.IsDeclaredNative((function.Name, 3))
                || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable repeated, Pattern.Variable same, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Guard: Expr.Call
                    {
                        DynamicRemote: false,
                        Function: AutoImportNames.IsInteger,
                        Arguments: [Expr.Variable integer]
                    } integerGuard,
                    Body: Expr.Literal integerResult
                },
                {
                    Patterns: [Pattern.Variable atom, Pattern.Variable expected, Pattern.Variable pairs],
                    Guard: Expr.Call
                    {
                        DynamicRemote: false,
                        Function: AutoImportNames.IsAtom,
                        Arguments: [Expr.Variable key]
                    } atomGuard,
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: ListModuleNames.Module,
                            DynamicRemote: false,
                            Function: ListModuleNames.KeyFind,
                            Arguments: [Expr.Variable lookup, Expr.Literal { Value: Integer position }, Expr.Variable table]
                        } search,
                        Clauses:
                                [
                                {
                                    Guard: null,
                                    Patterns:
                                            [
                                                Pattern.Tuple
                                            {
                                                Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable found]
                                            }
                                            ],
                                    Body: Expr.Literal atomResult
                                },
                                {
                                    Guard: null,
                                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Body: Expr.Call searchFailure
                                }
                                ]
                    }
                },
                {
                    Guard: null,
                    Patterns:
                            [
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ],
                    Body: Expr.Call badType
                }
                ]
                || repeated.Name == VariableScopeNames.Wildcard || same.Name != repeated.Name || integer.Name != repeated.Name
                || !DistinctBindings(atom, expected, pairs) || found.Name != expected.Name
                || atom.Name != key.Name || atom.Name != lookup.Name || pairs.Name != table.Name
                || !owner.ResolvesErlangBif(integerGuard) || !owner.ResolvesErlangBif(atomGuard)
                || ReadStackFreeThrow(searchFailure, requireCallerHandler: true) is not { } notFound
                || ReadStackFreeThrow(badType, requireCallerHandler: true) is not { } wrongType
                || !AttributeLiteral.TryNormalizeConstant(notFound.Arguments[1], out Term missingReason)
                || !AttributeLiteral.TryNormalizeConstant(wrongType.Arguments[1], out Term typeReason))
            {
                return false;
            }

            Integer? defaultValue = null;
            if (!HasOnlyLocalCalls(
                function,
                3,
                (_, call) =>
                {
                    if (call.Arguments[1] is not Expr.Literal { Value: Integer candidate })
                    {
                        return false;
                    }

                    defaultValue ??= candidate;

                    return defaultValue.Equals(candidate);
                }
            ) || defaultValue is null)
            {
                return false;
            }

            int atomLabel = Label();
            int missing = Label();
            int wrongTypeLabel = Label();
            CompareDefault(atomLabel);
            owner.EmitConstant(integerResult.Value, 0);
            owner.instructions.WriteByte(Return);

            Mark(atomLabel);
            owner.Op(IsAtom, LabelTag, wrongTypeLabel);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(arity: 3, size: 0);
            if (!defaultValue.Equals(position))
            {
                owner.EmitConstant(position, 1);
            }

            owner.EmitCallLine(search, location);
            Call(search.Module, search.Function, 3);
            owner.Op(IsTuple, LabelTag, missing);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.Op(TestArity, LabelTag, missing);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 1, 0);
            CompareDefault(missing);
            owner.EmitConstant(atomResult.Value, 0);
            owner.EmitFrameReturn(0);

            Mark(missing);
            EmitThrow(notFound, missingReason, frame: true);
            Mark(wrongTypeLabel);
            EmitThrow(wrongType, typeReason, frame: false);

            return true;

            void CompareDefault(int failure)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(defaultValue);
            }

            void EmitThrow(Expr.Call raise, Term reason, bool frame)
            {
                owner.EmitConstant(reason, 1);
                owner.EmitConstant(Nil.Value, 2);
                owner.EmitConstant(Term.A(ErlangExceptionClasses.Throw), 0);
                owner.EmitCallLine(raise, location);
                owner.Op(frame ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, raise.Function, 3));
                if (frame)
                {
                    owner.Operand(BeamWire.UnsignedTag, 0);
                }
            }
        }
    }
}
