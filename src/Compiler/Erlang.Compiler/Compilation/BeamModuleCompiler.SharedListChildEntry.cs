using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedListChildEntry(FunctionDefinition function, int failure)
        {
            if (function.Clauses is not
                [
                {
                    Patterns: [Erlang.Pattern.List { Items: [Erlang.Pattern.Variable input], Tail: null }],
                    Guard: Expr.Call { Function: AutoImportNames.IsList, Arguments: [Expr.Variable tested] } guard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.List { Items: [var prefix, Expr.Variable forwarded], Tail: null }]
                    } recur
                },
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.List { Items: [Erlang.Pattern.Variable mode, Erlang.Pattern.Variable options], Tail: null }],
                    Body: Expr.Sequence body
                }
                ]
                || input.Name == VariableScopeNames.Wildcard || input.Name != tested.Name || input.Name != forwarded.Name
                || !owner.ResolvesErlangBif(guard) || recur.Function != function.Name
                || !entries.TryGetValue((function.Name, 1), out int recurse)
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term defaultMode)
                || body.Items.Where(item => item is not Expr.Literal).ToArray() is not
                [
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable policy,
                    Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable readOptions] } selectPolicy
                },
                    Expr.Match { Pattern: Erlang.Pattern.Variable flags, Value: var flagsExpression },
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable first,
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                var firstId, Expr.List { Items: [Expr.Variable firstOptions], Tail: null },
                                Expr.Variable firstPolicy, var firstMetadata
                            ]
                    } firstCall
                },
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable second,
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [var secondId, var secondOptions, Expr.Variable secondPolicy]
                    } secondCall
                },
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable third,
                    Value: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [var thirdId, Expr.List { Items: [Expr.Variable thirdMode, Expr.Variable thirdOptions], Tail: null }, Expr.Variable thirdPolicy]
                    } thirdCall
                },
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable children,
                    Value: Expr.List { Items: [Expr.Variable listedFirst, Expr.Variable listedSecond, Expr.Variable listedThird], Tail: null }
                },
                    Expr.Tuple
                {
                    Items: [var replyTag, Expr.Tuple { Items: [Expr.Variable replyFlags, Expr.Variable replyChildren] }]
                }
                ]
                || !DistinctBindings(
                    mode,
                    options,
                    policy,
                    flags,
                    first,
                    second,
                    third,
                    children
                )
                || readOptions.Name != options.Name || firstOptions.Name != options.Name || thirdOptions.Name != options.Name
                || thirdMode.Name != mode.Name || firstPolicy.Name != policy.Name || secondPolicy.Name != policy.Name
                || thirdPolicy.Name != policy.Name || listedFirst.Name != first.Name || listedSecond.Name != second.Name
                || listedThird.Name != third.Name || replyFlags.Name != flags.Name || replyChildren.Name != children.Name
                || !AttributeLiteral.TryNormalizeConstant(flagsExpression, out Term flagsValue)
                || !AttributeLiteral.TryNormalizeConstant(replyTag, out Term response)
                || !AttributeLiteral.TryNormalizeConstant(firstId, out Term firstName)
                || !AttributeLiteral.TryNormalizeConstant(secondId, out Term secondName)
                || !AttributeLiteral.TryNormalizeConstant(thirdId, out Term thirdName)
                || !AttributeLiteral.TryNormalizeConstant(firstMetadata, out Term firstExtra)
                || !AttributeLiteral.TryNormalizeConstant(secondOptions, out Term middleArgs)
                || !entries.TryGetValue((selectPolicy.Function, 1), out int policyEntry)
                || !entries.TryGetValue((firstCall.Function, 4), out int firstEntry)
                || !entries.TryGetValue((secondCall.Function, 3), out int secondEntry)
                || !entries.TryGetValue((thirdCall.Function, 3), out int thirdEntry))
            {
                return false;
            }

            const int firstResultSlot = 0;
            const int tailSlot = 1;
            const int inputSlot = 2;
            const int frame = 3;
            int singleton = Label();
            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitNonemptyList(2, singleton);
            owner.EmitListParts(
                2,
                XRegisterTag,
                1,
                XRegisterTag,
                3
            );
            owner.EmitNilTest(XRegisterTag, 3, failure);

            EmitFrameAllocation(arity: 3, size: frame);
            owner.InitStackSlot(firstResultSlot);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                tailSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                inputSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            var previous = location;
            EmitLocal(selectPolicy, policyEntry);
            owner.MoveRegister(
                YRegisterTag,
                tailSlot,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                tailSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.EmitConstant(firstExtra, 3);
            owner.EmitConstant(firstName, 0);
            EmitLocal(firstCall, firstEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                firstResultSlot
            );
            owner.MoveRegister(
                YRegisterTag,
                tailSlot,
                XRegisterTag,
                2
            );
            owner.EmitConstant(middleArgs, 1);
            owner.EmitConstant(secondName, 0);
            EmitLocal(secondCall, secondEntry);
            owner.MoveRegister(
                YRegisterTag,
                inputSlot,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                inputSlot
            );
            owner.MoveRegister(
                YRegisterTag,
                tailSlot,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                YRegisterTag,
                firstResultSlot,
                YRegisterTag,
                tailSlot
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, frame - 1);
            owner.EmitConstant(thirdName, 0);
            EmitLocal(thirdCall, thirdEntry);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 * ConsHeapWords + 6);
            owner.Operand(BeamWire.UnsignedTag, 1);
            PutStartList(0, 0, Nil.Value);
            PutStartList(1, 0, sourceTag: YRegisterTag);
            PutStartList(0, 0, sourceTag: YRegisterTag);
            TupleHeader(0, 2);
            owner.LiteralOperand(flagsValue);
            owner.Operand(XRegisterTag, 0);
            TupleHeader(0, 2);
            owner.LiteralOperand(response);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame - 1);

            Mark(singleton);
            owner.EmitNilTest(XRegisterTag, 2, failure);
            owner.Op(IsList, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(defaultMode);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, recurse);

            return true;

            void EmitLocal(Expr.Call call, int entry)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitFieldCall(call.Arguments.Count, entry);
            }
        }
    }
}
