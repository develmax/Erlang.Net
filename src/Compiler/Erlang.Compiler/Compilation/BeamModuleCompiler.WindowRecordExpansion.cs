using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWindowRecordExpansion(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable id, Pattern.Variable callback, AliasPattern
                    {
                        Left: Pattern.Tuple
                        {
                            Items: [Pattern.Literal { Value: Atom stateTag }, Pattern.Variable { Name: "_" },
                        Pattern.Variable { Name: "_" }, Pattern.Variable app, Pattern.Variable { Name: "_" },
                        Pattern.Variable opened, Pattern.Variable { Name: "_" }, Pattern.Variable { Name: "_" }]
                        },
                        Right: Pattern.Variable state
                    }], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable windows, Value: Expr.Case
                            {
                                Value: Expr.Call
                                {
                                    Module: "lists", Function: "keyfind", DynamicRemote: false, Arguments: [
                            Expr.Variable query, Expr.Literal { Value: Integer position }, Expr.Variable candidates]
                                } find,
                                Clauses: [
                                {
                                    Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }], Body: Expr.Sequence
                                    {
                                        Items: [
                                Expr.Match
                                        {
                                            Pattern: Pattern.Variable window, Value: Expr.Call
                                            {
                                                Module: not null, DynamicRemote: false,
                                                Arguments: [Expr.Variable openId, var options, Expr.RecordAccess
                                                {
                                                    Value: Expr.Variable panelState,
                                                    Index: 2, Arity: 8
                                                } panel, Expr.Variable openCallback, Expr.Variable openApp]
                                            } open
                                        },
                                Expr.Binary
                                        {
                                            Operator: ErlangOperators.Send, Left: Expr.Call
                                            {
                                                Module: "wx_object", Function: "get_pid",
                                                DynamicRemote: false
                                            } getPid, Right: Expr.Literal { Value: Atom notification }
                                        },
                                Expr.List
                                        {
                                            Items: [Expr.Tuple { Items: [Expr.Variable insertedId, Expr.Variable insertedWindow] }],
                                            Tail: Expr.Variable tail
                                        }
                            ]
                                    }
                                },
                                {
                                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Variable { Name: "_" }, Pattern.Variable existing] }],
                                    Body: Expr.Sequence { Items: [Expr.Call raise, Expr.Variable unchanged] }
                                }
                        ]
                            }
                        },
                    Expr.RecordUpdate { Value: Expr.Variable original, Arity: 8, Fields: [{ Index: 5, Value: Expr.Variable updated }] } update
                ]
                    }
                }
            ] || !DistinctBindings(
                id,
                callback,
                app,
                opened,
                state,
                windows,
                window
            )
                || !DistinctBindings(
                    id,
                    callback,
                    app,
                    opened,
                    state,
                    existing
                )
                || query.Name != id.Name || candidates.Name != opened.Name
                || position.Value < 1 || !AttributeLiteral.TryNormalizeConstant(options, out Term optionValue)
                || openId.Name != id.Name || panelState.Name != state.Name || panel.Name != stateTag.Name
                || openCallback.Name != callback.Name || openApp.Name != app.Name
                || !MatchesDetailArguments(getPid, window) || !IsWindowCall(raise, existing)
                || insertedId.Name != id.Name || insertedWindow.Name != window.Name || tail.Name != opened.Name
                || unchanged.Name != opened.Name || original.Name != state.Name || update.Name != stateTag.Name
                || updated.Name != windows.Name
                || !HasOnlyLocalCalls(
                    function,
                    3,
                    (caller, invocation) =>
                    invocation.Arguments[2] is Expr.Variable argument && caller.Clauses.Any(clause =>
                        HasRecordArgument(
                            clause,
                            argument,
                            stateTag,
                            8
                        ) && IsDirectCaseCall(clause.Body, invocation))
                ))
            {
                return false;
            }

            int missing = Label();
            int done = Label();
            int invalid = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            GetTupleField(
                2,
                5,
                YRegisterTag,
                0
            );
            Load(0, 2);
            owner.EmitConstant(position, 1);
            var previous = location;
            EmitWindowCall(find, ref previous);
            var findLocation = previous;
            owner.Op(IsTuple, LabelTag, missing);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.Op(TestArity, LabelTag, invalid);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 1, 0);
            ClearDetailSlots(2, 3);
            EmitWindowCall(raise, ref previous);
            Load(0);
            JumpTo(done);

            Mark(missing);
            ReadState(2, 2);
            ReadState(3, 4);
            owner.EmitConstant(optionValue, 1);
            Load(2, 3);
            ClearDetailSlots(2);
            Load(3);
            EmitWindowCall(open, ref previous);
            SaveWindowResult(2);
            EmitWindowCall(getPid, ref previous);
            owner.EmitConstant(notification, 1);
            owner.instructions.WriteByte(Send);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 3), (YRegisterTag, 2));
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);

            Mark(done);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(UpdateRecord, AtomTag, owner.Intern("reuse"));
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.EmitTypedRegister(1, TupleTypeBits, registerTag: YRegisterTag);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(4);

            Mark(invalid);
            owner.EmitLine(findLocation);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;

            void ReadState(int field, int register)
            {
                owner.Op(GetTupleElement, YRegisterTag, 1);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, register);
            }
        }

        private static bool HasRecordArgument(
            Clause clause,
            Expr.Variable value,
            Atom tag,
            int arity
        ) =>
            clause.Patterns.Any(pattern => pattern is AliasPattern { Left: Pattern.Tuple tuple, Right: Pattern.Variable alias }
                && alias.Name == value.Name && tuple.Items.Count == arity
                && tuple.Items[0] is Pattern.Literal { Value: Atom actual } && actual.Equals(tag));

        private static bool IsDirectCaseCall(Expr body, Expr.Call invocation) =>
            ReferenceEquals(body, invocation)
            || body is Expr.Sequence sequence && sequence.Items.Any(item => ReferenceEquals(item, invocation)
                || item is Expr.Match { Value: Expr.Case decision }
                    && decision.Clauses.Any(branch => ReferenceEquals(LastExpression(branch.Body), invocation)));
    }
}
