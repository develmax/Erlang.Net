using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTimedMonitorReceive(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom socketTag }, ..] } socket, Pattern.Variable timeout
                ],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable address, Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Atom key }, Expr.Variable opts, Expr.Call {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable defaultOpts]
                        } defaults]
                    } lookup },
                    Expr.Match { Pattern: Pattern.Variable port, Value: Expr.Call {
                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable pid, Expr.Variable oldPort]
                    } resolve },
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom ownerTag }, Expr.Variable ownerPid] },
                        Expr.Variable ipArg, Expr.Variable portArg
                    ] } register,
                    Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable closePid] } close,
                        Clauses: [
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom stop }], Body: Expr.Sequence { Items: [
                                Expr.Call { Module: ErlangModuleNames.Module, DynamicRemote: false, Arguments: [
                                    Expr.Literal { Value: Atom monitorKind }, Expr.Variable monitorPid
                                ] } monitor,
                                Expr.Receive { Timeout: Expr.Variable delay, After: { } expired, Clauses: [
                                    { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                        Pattern.Literal { Value: Atom notification }, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                        Pattern.Literal { Value: Atom messageKind }, Pattern.Variable sender,
                                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                    ] }], Body: Expr.Literal { Value: Atom received } }
                                ] }
                            ] } },
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom waiting }], Body: Expr.Literal { Value: Atom immediate } },
                            { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returned }
                        ]
                    }
                ] }
            }] || !DistinctBindings(timeout, address, port, error, new(pid.Name), new(oldPort.Name), new(opts.Name))
                || !AttributeLiteral.TryNormalizeConstant(expired, out Term timeoutResult)
                || stop.Equals(waiting) || opts.Name != defaultOpts.Name || ownerPid.Name != pid.Name || closePid.Name != pid.Name
                || monitorPid.Name != pid.Name || sender.Name != pid.Name || delay.Name != timeout.Name
                || ipArg.Name != address.Name || portArg.Name != port.Name || returned.Name != error.Name
                || !entries.TryGetValue((defaults.Function, 1), out int defaultsEntry)
                || !entries.TryGetValue((resolve.Function, 2), out int resolveEntry)
                || owner.ResolvesErlangBif(lookup) || owner.ResolvesErlangBif(register) || owner.ResolvesErlangBif(close)
                || !ReadMonitorRecord(socket, out int handle, out int configuration, out var config, out var configTag, out int optionsField))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int ready = Label();
            int stopping = Label();
            int scan = Label();
            int skip = Label();
            int wait = Label();
            int fallback = Label();
            EmitDetailRecordTest(0, socket.Items.Count, socketTag, functionInfo);
            owner.EmitTuplePart(0, handle, 2);
            owner.EmitTupleCheck(2, 2, functionInfo);
            owner.EmitTuplePart(0, configuration, 3);
            EmitDetailRecordTest(3, config.Items.Count, configTag, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            owner.Op(GetTupleElement, XRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, optionsField);
            owner.Operand(YRegisterTag, 0);
            Load(0);
            var previous = location;
            EmitWindowLocal(defaults, defaultsEntry, ref previous);
            Load(0, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.InitStackSlot(0);
            owner.EmitConstant(key, 0);
            EmitWindowCall(lookup, ref previous);
            owner.Op(GetTupleElement, YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Op(GetTupleElement, YRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 1);
            SaveWindowResult(1);
            Load(0);
            EmitWindowLocal(resolve, resolveEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(ownerTag.Name)), (YRegisterTag, 0));
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            Load(1, 1);
            owner.InitStackSlot(1);
            EmitWindowCall(register, ref previous);
            Load(0);
            EmitWindowCall(close, ref previous);
            if (TermOrder.Compare(stop, waiting, exact: true) < 0)
            {
                owner.EmitAtomSelection(0, fallback, stop.Name, stopping, waiting.Name, ready);
            }
            else
            {
                owner.EmitAtomSelection(0, fallback, waiting.Name, ready, stop.Name, stopping);
            }

            Mark(ready);
            owner.EmitConstant(immediate, 0);
            owner.EmitFrameReturn(3);
            Mark(stopping);
            Load(0, 1);
            owner.EmitConstant(monitorKind, 0);
            EmitWindowCall(monitor, ref previous);
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            EmitDetailRecordTest(0, 5, notification, skip);
            owner.EmitTuplePart(0, 2, 1);
            owner.EmitTuplePart(0, 3, 0);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(messageKind);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitConstant(received, 0);
            owner.EmitFrameReturn(3);
            Mark(skip);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(WaitTimeout, LabelTag, scan);
            owner.Operand(YRegisterTag, 2);
            owner.instructions.WriteByte(ReceiveTimeout);
            owner.EmitConstant(timeoutResult, 0);
            owner.EmitFrameReturn(3);
            Mark(fallback);
            owner.EmitFrameReturn(3);

            return true;

            bool ReadMonitorRecord(Pattern.Tuple pattern, out int handleField, out int configField,
                out Pattern.Tuple configPattern, out Atom tag, out int optionField)
            {
                handleField = -1;
                configField = -1;
                configPattern = null!;
                tag = null!;
                optionField = -1;
                for (int field = 1; field < pattern.Items.Count; field++)
                {
                    switch (pattern.Items[field])
                    {
                        case Pattern.Tuple { Items: [Pattern.Variable boundPid, Pattern.Variable boundPort] }
                            when handleField < 0 && boundPid.Name == pid.Name && boundPort.Name == oldPort.Name:
                            handleField = field;
                            break;
                        case Pattern.Tuple { Items: [Pattern.Literal { Value: Atom configName }, ..] } inner when configField < 0:
                            configField = field;
                            configPattern = inner;
                            tag = configName;
                            for (int index = 1; index < inner.Items.Count; index++)
                            {
                                if (inner.Items[index] is Pattern.Variable variable && variable.Name == opts.Name && optionField < 0)
                                {
                                    optionField = index;
                                }
                                else if (inner.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                                {
                                    return false;
                                }
                            }

                            break;
                        case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                            break;
                        default:
                            return false;
                    }
                }

                return handleField >= 0 && configField >= 0 && optionField >= 0;
            }
        }
    }
}
