using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTimedProcessMonitor(FunctionDefinition function)
        {
            if (!owner.ReceiveOptimizationEnabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Patterns: [Erlang.Pattern.Variable immediateTarget, Erlang.Pattern.Variable immediateTimeout],
                    Guard: Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: Expr.Variable immediateTest, Right: Expr.Literal { Value: Integer equality } },
                    Body: Expr.Sequence direct
                },
                {
                    Guard: null, Patterns: [Erlang.Pattern.Variable target, Erlang.Pattern.Variable timeout],
                    Body: Expr.Sequence monitored
                }
                ]
                || !DistinctBindings(immediateTarget, immediateTimeout) || !DistinctBindings(target, timeout)
                || immediateTest.Name != immediateTimeout.Name
                || ReadEffects(direct) is not
                [
                    Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable directTarget, var directArgument]
                } stopDirect,
                    Expr.Literal directResult
                ]
                || directTarget.Name != immediateTarget.Name
                || !AttributeLiteral.TryNormalizeConstant(directArgument, out Term directOption)
                || ReadEffects(monitored) is not
                [
                    Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable reference,
                    Value: Expr.Call
                    {
                        Function: AutoImportNames.Monitor, DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Atom { Name: "process" } category }, Expr.Variable monitorTarget]
                    } monitor
                },
                    Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable stopTarget, var stopArgument]
                } stop,
                    Expr.Receive
                {
                    Clauses:
                        [
                        {
                            Guard: null,
                            Patterns:
                                [
                                    Erlang.Pattern.Tuple
                                {
                                    Items:
                                        [
                                            Erlang.Pattern.Literal { Value: Atom { Name: "DOWN" } notification },
                                            Erlang.Pattern.Variable matchedReference,
                                            Erlang.Pattern.Literal { Value: Atom messageCategory },
                                            Erlang.Pattern.Variable matchedTarget,
                                            Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                        ]
                                }
                                ],
                            Body: var receivedBody
                        }
                        ],
                    Timeout: Expr.Variable delay, After: Expr.Sequence expired
                }
                ]
                || !DistinctBindings(target, timeout, reference) || target.Name != monitorTarget.Name
                || target.Name != stopTarget.Name || target.Name != matchedTarget.Name || reference.Name != matchedReference.Name
                || !ReadResult(receivedBody, out Term received)
                || !category.Equals(messageCategory) || timeout.Name != delay.Name
                || !owner.ResolvesErlangBif(monitor)
                || !AttributeLiteral.TryNormalizeConstant(stopArgument, out Term stopOption)
                || ReadEffects(expired) is not
                [
                    Expr.Call
                {
                    Function: AutoImportNames.Demonitor, DynamicRemote: false,
                    Arguments: [Expr.Variable discardReference, var discardOptions]
                } cleanup,
                    var expirationResult
                ]
                || !AttributeLiteral.TryNormalizeConstant(expirationResult, out Term expiration)
                || discardReference.Name != reference.Name || !owner.ResolvesErlangBif(cleanup)
                || !AttributeLiteral.TryNormalizeConstant(discardOptions, out Term cleanupOptions)
                || !HasGuardedProcessEntry(function))
            {
                return false;
            }

            const int referenceSlot = 0;
            const int timeoutSlot = 1;
            const int targetSlot = 2;
            const int frame = 3;
            int fallback = Label();
            int scan = Label();
            int skip = Label();
            int wait = Label();

            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(equality);
            EmitFrameAllocation(1, 0);
            owner.EmitConstant(directOption, 1);
            owner.EmitCallLine(stopDirect, location);
            owner.EmitStaticRemoteCall(stopDirect);
            owner.EmitConstant(directResult.Value, 0);
            owner.EmitFrameReturn(0);

            Mark(fallback);
            EmitFrameAllocation(2, frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                timeoutSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                targetSlot
            );
            owner.Op(ReceiveMarkerReserve, YRegisterTag, referenceSlot);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(category, 0);
            owner.EmitCallLine(monitor, location);
            Call(ErlangModuleNames.Module, monitor.Function, 2);
            owner.Op(ReceiveMarkerBind, YRegisterTag, referenceSlot);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                referenceSlot
            );
            owner.MoveRegister(
                YRegisterTag,
                targetSlot,
                XRegisterTag,
                0
            );
            owner.EmitConstant(stopOption, 1);
            var previous = CallLocation(monitor, location);
            owner.EmitCallLine(stop, previous);
            owner.EmitStaticRemoteCall(stop);
            owner.Op(ReceiveMarkerUse, YRegisterTag, referenceSlot);

            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            EmitDetailRecordTest(
                0,
                5,
                notification,
                skip
            );
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTuplePart(0, 2, 2);
            owner.EmitTuplePart(0, 3, 0);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 2);
            owner.LiteralOperand(messageCategory);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTypedRegister(referenceSlot, ReferenceTypeBits, registerTag: YRegisterTag);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTypedRegister(targetSlot, PidTypeBits, registerTag: YRegisterTag);
            owner.Op(ReceiveMarkerClear, YRegisterTag, referenceSlot);
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitConstant(received, 0);
            owner.EmitFrameReturn(frame);

            Mark(skip);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(WaitTimeout, LabelTag, scan);
            owner.Operand(YRegisterTag, timeoutSlot);
            owner.instructions.WriteByte(ReceiveTimeout);
            owner.EmitConstant(cleanupOptions, 1);
            owner.MoveRegister(
                YRegisterTag,
                referenceSlot,
                YRegisterTag,
                targetSlot
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(cleanup, CallLocation(stop, previous));
            Call(ErlangModuleNames.Module, cleanup.Function, 2);
            owner.Op(ReceiveMarkerClear, YRegisterTag, 0);
            owner.EmitConstant(expiration, 0);
            owner.EmitFrameReturn(1);

            return true;

            static bool ReadResult(Expr body, out Term value)
            {
                Expr result = body is Expr.Sequence sequence
                    && sequence.Items.Take(sequence.Items.Count - 1).All(item => item is Expr.Literal)
                    ? sequence.Items[^1] : body;

                return AttributeLiteral.TryNormalizeConstant(result, out value);
            }

            static Expr[] ReadEffects(Expr.Sequence sequence) =>
                [.. sequence.Items.SkipLast(1).Where(item => item is not Expr.Literal), sequence.Items[^1]];
        }

        private bool HasGuardedProcessEntry(FunctionDefinition function) =>
            HasOnlyLocalCalls(
                function,
                2,
                (caller, call) =>
                call.Arguments[0] is Expr.Variable forwarded
                && caller.Clauses.Any(clause => IsGuardedTail(clause.Body, call, forwarded.Name))
            );

        private bool IsGuardedTail(Expr body, Expr.Call tail, string name)
        {
            Expr value = body is Expr.Sequence sequence && sequence.Items.Take(sequence.Items.Count - 1).All(item => item is Expr.Literal)
                ? sequence.Items[^1] : body;

            return value is Expr.Case decision && decision.Clauses.Any(branch =>
                ReferenceEquals(branch.Body, tail) && branch.Patterns is [Erlang.Pattern.Variable parameter] && parameter.Name == name
                && branch.Guard is Expr.Call { Function: AutoImportNames.IsPid, Arguments: [Expr.Variable tested] } test
                && tested.Name == name && owner.ResolvesErlangBif(test));
        }
    }
}
