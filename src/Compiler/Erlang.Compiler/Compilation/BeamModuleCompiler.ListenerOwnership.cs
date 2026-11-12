using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListenerOwnership(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [AliasPattern alias, Pattern.Variable ip, Pattern.Variable port, Pattern.Variable socket, Pattern.Variable process],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable options, Value: Expr.Call { Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable emulated, var defaultOptions] } prepare },
                    Expr.Case { Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.List { Tail: null, Items: [Expr.Variable ownerArg, Expr.Variable portArg, Expr.Variable transportInfo,
                            Expr.Variable optionsArg, Expr.Variable ssl, Expr.Variable socketArg] }
                    ] } spawn, Clauses: [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom ok }, Pattern.Variable multiplexer
                        ] }], Body: Expr.Sequence { Items: [
                            Expr.Match { Pattern: Pattern.Literal { Value: Atom expected }, Value: Expr.Call {
                                DynamicRemote: true, Arguments: [
                                    Expr.Variable transport, Expr.Literal { Value: Atom member },
                                    Expr.List { Tail: null, Items: [Expr.Variable controlledSocket, Expr.Variable recipient] }
                                ]
                            } control } transfer,
                            Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                Expr.Tuple { Items: [Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                                    Function: AutoImportNames.Self, Arguments: [] } self, Expr.Variable registered] },
                                Expr.Variable registeredIp, Expr.Variable registeredPort
                            ] } register,
                            Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call { Module: null, DynamicRemote: false,
                                Arguments: [Expr.Variable configArg, Expr.Variable listenerArg, Expr.Variable finalPort] } create },
                            Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable value] }
                        ] } },
                        { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returned }
                    ] }
                ] }
            }] || !TryReadPatternAlias(alias, out var config, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom configTag }, ..] } record
                || !DistinctBindings(config, ip, port, socket, process, options, multiplexer, result,
                    new(emulated.Name), new(transportInfo.Name), new(ssl.Name), new(transport.Name))
                || !DistinctBindings(config, ip, port, socket, process, options, error,
                    new(emulated.Name), new(transportInfo.Name), new(ssl.Name), new(transport.Name))
                || ownerArg.Name != process.Name || portArg.Name != port.Name || optionsArg.Name != options.Name
                || socketArg.Name != socket.Name || controlledSocket.Name != socket.Name || recipient.Name != multiplexer.Name
                || registered.Name != multiplexer.Name || registeredIp.Name != ip.Name || registeredPort.Name != port.Name
                || configArg.Name != config.Name || listenerArg.Name != multiplexer.Name || finalPort.Name != port.Name
                || value.Name != result.Name || returned.Name != error.Name
                || !owner.ResolvesErlangBif(self) || owner.ResolvesErlangBif(spawn) || owner.ResolvesErlangBif(register)
                || !AttributeLiteral.TryNormalizeConstant(defaultOptions, out Term defaultValue)
                || !entries.TryGetValue((prepare.Function, 2), out int prepareEntry)
                || !entries.TryGetValue((create.Function, 3), out int createEntry)
                || ReadOwnershipFields(record, transportInfo.Name, transport.Name, emulated.Name, ssl.Name) is not { } fields)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int rejected = Label();
            int badMatch = Label();
            if (!AlwaysReceivesTaggedArgument(function, 0, configTag, record.Items.Count))
            {
                EmitDetailRecordTest(0, record.Items.Count, configTag, functionInfo);
            }

            owner.EmitTuplePart(0, fields.Transport, 5);
            owner.EmitTupleCheck(5, 5, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 6);
            for (int slot = 0; slot < 6; slot++)
            {
                owner.MoveRegister(XRegisterTag, 5 - slot, YRegisterTag, slot);
            }

            owner.EmitTuplePart(0, fields.Emulated, 0);
            owner.EmitConstant(defaultValue, 1);
            var previous = location;
            EmitWindowLocal(prepare, prepareEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            LoadTupleField(5, fields.Ssl, XRegisterTag, 1);
            owner.Op(PutList, YRegisterTag, 2);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 2);
            PutListItem(XRegisterTag, 1, 2, 1);
            PutListItem(XRegisterTag, 0, 1, 0);
            PutListItem(YRegisterTag, 0, 0, 0);
            PutListItem(YRegisterTag, 3, 0, 0);
            PutListItem(YRegisterTag, 1, 0, 0);
            owner.InitStackSlot(1);
            EmitWindowCall(spawn, ref previous);
            EmitDetailRecordTest(0, 2, ok, rejected);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 1);
            LoadTupleField(0, 0, XRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            Load(1, 1);
            owner.EmitConstant(member, 3);
            Load(2);
            owner.MoveRegister(YRegisterTag, 1, YRegisterTag, 2);
            TrimDetailFrame(2, 4);
            owner.EmitCallLine(control, previous);
            previous = CallLocation(control, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 2);
            EmitConfigurationAtomTest(expected, badMatch);
            var controlLocation = previous;
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            Load(1, 2);
            Load(2, 1);
            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 2);
            TrimDetailFrame(1, 3);
            EmitWindowCall(register, ref previous);
            Load(1, 1);
            Load(0, 2);
            Load(2);
            TrimDetailFrame(3, 0);
            EmitWindowLocal(create, createEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(resultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(0);
            Mark(rejected);
            owner.EmitFrameReturn(6);
            Mark(badMatch);
            owner.EmitLine(new(transfer.PreprocessorFile ?? controlLocation.File, transfer.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void PutListItem(int tag, int head, int tail, int destination)
            {
                owner.Op(PutList, tag, head);
                owner.Operand(XRegisterTag, tail);
                owner.Operand(XRegisterTag, destination);
            }
        }

        private static (int Transport, int Emulated, int Ssl)? ReadOwnershipFields(
            Pattern.Tuple record,
            string transportInfo,
            string transport,
            string emulated,
            string ssl
        )
        {
            int transportField = -1;
            int emulatedField = -1;
            int sslField = -1;
            for (int index = 1; index < record.Items.Count; index++)
            {
                switch (record.Items[index])
                {
                    case AliasPattern alias when transportField < 0
                        && TryReadPatternAlias(alias, out var info, out var shape) && info.Name == transportInfo
                        && shape is Pattern.Tuple { Items: [Pattern.Variable module, ..] } tuple
                        && module.Name == transport && tuple.Items.Count == 5
                        && tuple.Items.Skip(1).All(item => item is Pattern.Variable { Name: VariableScopeNames.Wildcard }):
                        transportField = index;
                        break;
                    case Pattern.Variable value when value.Name == emulated && emulatedField < 0:
                        emulatedField = index;
                        break;
                    case Pattern.Variable value when value.Name == ssl && sslField < 0:
                        sslField = index;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    default:
                        return null;
                }
            }

            return transportField >= 0 && emulatedField >= 0 && sslField >= 0
                ? (transportField, emulatedField, sslField) : null;
        }
    }
}
