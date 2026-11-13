using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedRecordEvent(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom outer }, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom shortTag }, Pattern.Literal { Value: Atom shortKind }] }
                ]
                    }, Pattern.Variable firstState], Body: Expr.Tuple
                    {
                        Items: [
                    Expr.Literal { Value: Atom result }, Expr.Literal { Value: Atom reason }, Expr.Variable firstResult
                ]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom otherOuter }, Pattern.Literal { Value: Integer id },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom longTag }, Pattern.Literal { Value: Atom longKind },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        }
                ]
                    }, Pattern.Variable secondState], Body: Expr.Tuple
                    {
                        Items: [
                    Expr.Literal { Value: Atom otherResult }, Expr.Literal { Value: Atom otherReason }, Expr.Variable secondResult
                ]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable eventArg, Pattern.Variable { }], Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, Function: "error", DynamicRemote: false,
                        Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom errorTag }, Expr.Variable errorArg] }]
                    } error
                } fallback
            ] || outer.Name != otherOuter.Name || result.Name != otherResult.Name || reason.Name != otherReason.Name
                || firstState.Name != firstResult.Name || secondState.Name != secondResult.Name
                || firstState.Name == VariableScopeNames.Wildcard || secondState.Name == VariableScopeNames.Wildcard
                || !HasUnconditionalParameters(fallback) || eventArg.Name == VariableScopeNames.Wildcard
                || eventArg.Name != errorArg.Name || !owner.ResolvesErlangBif(error))
            {
                return false;
            }

            int matched = Label();
            int rejected = Label();
            EmitDetailRecordTest(
                0,
                5,
                outer,
                rejected
            );
            owner.EmitTuplePart(0, 4, 2);
            owner.Op(ExactNotEqualTest, LabelTag, matched);
            owner.Operand(XRegisterTag, 2);
            owner.LiteralOperand(Term.Tuple(shortTag, shortKind));
            owner.EmitTuplePart(0, 1, 3);
            owner.Op(ExactEqualTest, LabelTag, rejected);
            owner.Operand(XRegisterTag, 3);
            owner.LiteralOperand(id);
            EmitDetailRecordTest(
                2,
                5,
                longTag,
                rejected
            );
            owner.EmitTuplePart(2, 1, 2);
            owner.EmitAtomTest(2, longKind.Name, rejected);

            Mark(matched);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(result);
            owner.LiteralOperand(reason);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(Return);

            Mark(rejected);
            owner.EmitTaggedArgumentError(
                error,
                errorTag,
                [0],
                location
            );

            return true;
        }

        private bool TryEmitRecordCleanup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable ignored, Pattern.Tuple
                    {
                        Items: [
                    Pattern.Literal { Value: Atom state }, Pattern.Variable parent, Pattern.Variable frame, Pattern.Variable id,
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ]
                    }], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable recipient, Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable payload] }
                    ]
                        } notify,
                    Expr.Case
                        {
                            Value: Expr.Variable tested, Clauses: [
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom absent }], Body: Expr.Literal },
                            {
                                Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                Body: Expr.Call { Module: not null, DynamicRemote: false } destroy
                            }
                    ]
                        },
                    Expr.Literal { Value: Atom result }
                ]
                    }
                }
            ] || !DistinctBindings(parent, frame, id)
                || ignored.Name != VariableScopeNames.Wildcard && !DistinctBindings(
                    ignored,
                    parent,
                    frame,
                    id
                )
                || recipient.Name != parent.Name || payload.Name != id.Name || tested.Name != frame.Name
                || !MatchesDetailArguments(destroy, frame))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int done = Label();
            EmitDetailRecordTest(
                1,
                5,
                state,
                functionInfo
            );
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            GetTupleField(
                1,
                2,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(1, 3, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTuplePart(1, 1, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            var previous = location;
            EmitWindowCall(notify, ref previous);
            owner.Op(ExactNotEqualTest, LabelTag, done);
            owner.Operand(YRegisterTag, 0);
            owner.LiteralOperand(absent);
            Load(0);
            ClearDetailSlots(0);
            EmitWindowCall(destroy, ref previous);
            Mark(done);
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(1);

            return true;
        }

        private void EmitDetailRecordTest(
            int register,
            int arity,
            Atom tag,
            int failure,
            int inputType = 0
        )
        {
            owner.Op(IsTaggedTuple, LabelTag, failure);
            if (inputType != 0)
            {
                owner.EmitTypedRegister(register, inputType);
            }
            else
            {
                owner.Operand(XRegisterTag, register);
            }

            owner.Operand(BeamWire.UnsignedTag, arity);
            owner.LiteralOperand(tag);
        }
    }
}
