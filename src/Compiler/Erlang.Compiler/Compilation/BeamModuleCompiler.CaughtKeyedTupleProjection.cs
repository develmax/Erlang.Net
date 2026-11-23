using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCaughtKeyedTupleProjection(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable key, Pattern.Variable input],
                    Guard: var inputGuard,
                    Body: Expr.Try
                    {
                        After: null, Clauses.Count: 0,
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable matched, Pattern.Variable value, ..] } tuple,
                                    Value: Expr.Call
                                    {
                                        Arguments: [Expr.Variable lookedUp, Expr.Literal { Value: Integer position }, Expr.Call produce]
                                    } lookup
                                },
                                    Expr.Variable returned
                                ]
                        },
                        Catches: [var recognized, var fallback]
                    }
                } clause
                ]
                || !DistinctBindings(key, input, value) || key.Name != matched.Name || key.Name != lookedUp.Name
                || tuple.Items.Skip(2).Any(pattern => pattern is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || returned.Name != value.Name || ReadEntryTypeGuard(clause, inputGuard) != (IsPid, 1)
                || lookup.Module != LookupListModule || lookup.Function != KeyFindFunction
                || lookup.DynamicRemote || !position.Value.IsOne
                || produce.Arguments is not [Expr.Variable source] || source.Name != input.Name
                || !TryResolveOrdinaryCall(produce, out string? producerModule) || producerModule is null
                || ReadFiniteAtomArguments(function, 0) is not { Count: > 0 })
            {
                return false;
            }

            if (recognized is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Pattern.Tuple
                    {
                        Items:
                            [
                                Pattern.Literal { Value: Atom exceptionClass },
                                Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom failureReason }, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                            },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ]
                    }
                    ]
                }
                || !AttributeLiteral.TryNormalizeConstant(recognized.Body, out Term handled)
                || fallback is not
                {
                    Guard: null, Patterns:
                    [
                        Pattern.Tuple
                    {
                        Items:
                            [
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ]
                    }
                    ],
                    Body: Expr.Tuple
                    {
                        Items:
                        [
                            Expr.Literal { Value: Atom responseTag },
                            Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom fallbackTag }, Expr.Literal { Value: Atom ownerTag }, Expr.Variable retainedKey]
                        }
                        ]
                    }
                }
                || retainedKey.Name != key.Name)
            {
                return false;
            }

            const int keySlot = 0;
            const int token = 1;
            const int frame = 2;
            int functionInfo = owner.nextLabel - 2;
            int handler = Label();
            int unrecognized = Label();
            int badmatch = Label();
            owner.Op(IsPid, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 1);
            EmitFrameAllocation(2, frame);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                keySlot
            );
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(produce, location);
            Call(producerModule, produce.Function, 1);
            owner.EmitConstant(position, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                YRegisterTag,
                keySlot,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(lookup, CallLocation(produce, location));
            Call(LookupListModule, KeyFindFunction, KeyFindArity);
            owner.Op(IsTuple, LabelTag, badmatch);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.Op(TestArity, LabelTag, badmatch);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(ExactEqualTest, LabelTag, badmatch);
            owner.EmitTypedRegister(1, AtomTypeBits);
            owner.EmitTypedRegister(keySlot, AtomTypeBits, registerTag: YRegisterTag);
            owner.Op(TryEnd, YRegisterTag, token);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitFrameReturn(frame);

            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.Op(ExactEqualTest, LabelTag, unrecognized);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(exceptionClass);
            owner.Op(IsTaggedTuple, LabelTag, unrecognized);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(failureReason);
            owner.EmitConstant(handled, 0);
            owner.EmitFrameReturn(frame);

            Mark(unrecognized);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(
                0,
                (AtomTag, owner.Intern(fallbackTag.Name)),
                (AtomTag, owner.Intern(ownerTag.Name)),
                (YRegisterTag, keySlot)
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(responseTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(frame);

            Mark(badmatch);
            owner.EmitLine(CallLocation(lookup, location));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
