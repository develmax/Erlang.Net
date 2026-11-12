using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListenerReuse(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable port, AliasPattern alias],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable ip, Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Atom key }, Expr.Variable socketOptions,
                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable defaultInput] } defaults]
                    } get },
                    Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable lookupIp, Expr.Variable lookupPort] } lookup, Clauses: [
                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom missing }],
                            Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                                Expr.Variable startIp, Expr.Variable startPort, Expr.Call startSelf, Expr.Variable startConfig
                            ] } start },
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom ok }, Pattern.Variable listener
                        ] }], Body: Expr.Sequence { Items: [
                            Expr.Call { Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable newOwnerListener, Expr.Call ownerSelf] } newOwner,
                            Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                Expr.Variable updateListener, Expr.Tuple { Items: [
                                    Expr.Variable userOptions, Expr.Call { Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable emulated, var defaultOptions] } prepare,
                                    Expr.Variable ssl
                                ] }
                            ] } update,
                            Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                Expr.Tuple { Items: [Expr.Call registerSelf, Expr.Variable registered] },
                                Expr.Variable registeredIp, Expr.Variable registeredPort
                            ] } register,
                            Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag },
                                Expr.Call { Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable finalConfig, Expr.Variable finalListener, Expr.Variable finalPort] } create
                            ] }
                        ] } },
                        { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returned }
                    ] }
                ] }
            }] || !TryReadPatternAlias(alias, out var config, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom configTag }, ..] } record
                || !DistinctBindings(port, config, ip, listener, new(socketOptions.Name),
                    new(userOptions.Name), new(emulated.Name), new(ssl.Name))
                || !DistinctBindings(port, config, ip, error, new(socketOptions.Name),
                    new(userOptions.Name), new(emulated.Name), new(ssl.Name))
                || socketOptions.Name != defaultInput.Name || ip.Name != lookupIp.Name || port.Name != lookupPort.Name
                || ip.Name != startIp.Name || port.Name != startPort.Name || config.Name != startConfig.Name
                || listener.Name != newOwnerListener.Name || listener.Name != updateListener.Name || listener.Name != registered.Name
                || ip.Name != registeredIp.Name || port.Name != registeredPort.Name || config.Name != finalConfig.Name
                || listener.Name != finalListener.Name || port.Name != finalPort.Name || error.Name != returned.Name
                || !IsReceiverSelf(startSelf) || !IsReceiverSelf(ownerSelf) || !IsReceiverSelf(registerSelf)
                || owner.ResolvesErlangBif(get) || owner.ResolvesErlangBif(lookup) || owner.ResolvesErlangBif(newOwner)
                || owner.ResolvesErlangBif(update) || owner.ResolvesErlangBif(register)
                || !AttributeLiteral.TryNormalizeConstant(defaultOptions, out Term defaultValue)
                || !entries.TryGetValue((defaults.Function, 1), out int defaultsEntry)
                || !entries.TryGetValue((start.Function, 4), out int startEntry)
                || !entries.TryGetValue((prepare.Function, 2), out int prepareEntry)
                || !entries.TryGetValue((create.Function, 3), out int createEntry))
            {
                return false;
            }

            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 1; index < record.Items.Count; index++)
            {
                if (record.Items[index] is not Pattern.Variable field
                    || field.Name != VariableScopeNames.Wildcard && !fields.TryAdd(field.Name, index))
                {
                    return false;
                }
            }

            if (fields.Count != 4 || !fields.TryGetValue(socketOptions.Name, out int socketField)
                || !fields.TryGetValue(userOptions.Name, out int userField)
                || !fields.TryGetValue(emulated.Name, out int emulatedField) || !fields.TryGetValue(ssl.Name, out int sslField))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int absent = Label();
            int fallback = Label();
            EmitDetailRecordTest(1, record.Items.Count, configTag, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.InitStackSlot(0);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            SaveWindowResult(3);
            owner.Op(GetTupleElement, XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, socketField);
            owner.Operand(YRegisterTag, 1);
            Load(1);
            var previous = location;
            EmitWindowLocal(defaults, defaultsEntry, ref previous);
            Load(1, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.InitStackSlot(1);
            owner.EmitConstant(key, 0);
            EmitWindowCall(get, ref previous);
            SaveWindowResult(1);
            Load(3, 1);
            EmitWindowCall(lookup, ref previous);
            owner.Op(IsTuple, LabelTag, absent);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, fallback);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(ok);
            EmitReceiverSelf(1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            Load(0);
            EmitWindowCall(newOwner, ref previous);
            LoadTupleField(2, emulatedField, XRegisterTag, 0);
            owner.EmitConstant(defaultValue, 1);
            EmitWindowLocal(prepare, prepareEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            LoadTupleField(2, sslField, XRegisterTag, 1);
            LoadTupleField(2, userField, XRegisterTag, 2);
            owner.EmitTagMatchTuple(1, (XRegisterTag, 2), (XRegisterTag, 0), (XRegisterTag, 1));
            Load(0);
            EmitWindowCall(update, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitReceiverSelf(0);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            Load(1, 1);
            Load(3, 2);
            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 1);
            TrimDetailFrame(1, 3);
            EmitWindowCall(register, ref previous);
            Load(0, 1);
            Load(2, 2);
            Load(1);
            TrimDetailFrame(3, 0);
            EmitWindowLocal(create, createEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(resultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(0);
            Mark(absent);
            EmitConfigurationAtomTest(missing, fallback);
            EmitReceiverSelf(2);
            Load(2, 3);
            Load(3, 1);
            Load(1);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 4);
            owner.Operand(LabelTag, startEntry);
            owner.Operand(BeamWire.UnsignedTag, 4);
            Mark(fallback);
            owner.EmitFrameReturn(4);

            return true;
        }
    }
}
