using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMaybeSocketConnect(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [AliasPattern alias, Pattern.Variable timer, Pattern.Variable options],
                Body: Expr.Maybe { Items: [
                    Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom opened }, Pattern.Variable socket
                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable family, Expr.Literal { Value: Atom stream }] } open },
                    Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom configured }, Value: Expr.Call {
                        Module: null, DynamicRemote: false, Arguments: [
                            Expr.Variable setSocket, Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                Expr.Variable native, var additions, Expr.List { Items: [], Tail: null }
                            ] } merge
                        ]
                    } set },
                    Expr.Match { Pattern: Pattern.Variable bindAddress, Value: Expr.Map { Base: null, Fields: [
                        { Exact: false, Key: Expr.Literal { Value: Atom familyKey }, Value: Expr.Variable bindFamily },
                        { Exact: false, Key: Expr.Literal { Value: Atom addrKey }, Value: Expr.Literal address },
                        { Exact: false, Key: Expr.Literal { Value: Atom portKey }, Value: Expr.Call { Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Literal portOption, Expr.Variable bindOptions, Expr.Literal defaultPort] } lookup }
                    ] } },
                    Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom bound }, Value: Expr.Call { Module: not null,
                        DynamicRemote: false, Arguments: [Expr.Variable bindSocket, Expr.Variable bindArg] } bind },
                    Expr.Match { Pattern: Pattern.Variable connectAddress, Value: Expr.Map { Base: null, Fields: [
                        { Exact: false, Key: Expr.Literal { Value: Atom peerFamilyKey }, Value: Expr.Variable peerFamily },
                        { Exact: false, Key: Expr.Literal { Value: Atom peerAddrKey }, Value: Expr.Variable ip },
                        { Exact: false, Key: Expr.Literal { Value: Atom peerPortKey }, Value: Expr.Variable port }
                    ] } },
                    Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom connected }, Value: Expr.Call { Module: not null,
                        DynamicRemote: false, Arguments: [Expr.Variable connectSocket, Expr.Variable connectArg] } connect },
                    Expr.Match { Pattern: Pattern.Variable sslOptions, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } defaults },
                    Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom secured }, Pattern.Variable sslSocket
                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable secureSocket, Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Variable defaultArg, Expr.List { Tail: null, Items: [
                                Expr.Variable sslFamily, var first, var second, var third,
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom nameKey }, Expr.Variable host] }
                            ] }, Expr.List { Items: [], Tail: null }
                        ] } sslMerge, Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } timeout
                    ] } secure },
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable finalAddress, Expr.Variable finalSocket] } start
                ], Clauses: [{ Guard: null, Patterns: [AliasPattern errorAlias], Body: Expr.Variable returned }] } conditional
            }] || !TryReadPatternAlias(alias, out var config, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } record
                || !TryReadPatternAlias(errorAlias, out var error, out var errorShape)
                || errorShape is not Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom errorTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ] }
                || !DistinctBindings(options, config, socket, bindAddress, connectAddress, sslOptions, sslSocket,
                    new(family.Name), new(host.Name), new(ip.Name), new(port.Name))
                || error.Name == VariableScopeNames.Wildcard || error.Name != returned.Name
                || new[] { options.Name, config.Name, family.Name, host.Name, ip.Name, port.Name }.Contains(error.Name)
                || timer.Name != VariableScopeNames.Wildcard
                    && new[] { options.Name, config.Name, socket.Name, bindAddress.Name, connectAddress.Name, sslOptions.Name,
                        sslSocket.Name, family.Name, host.Name, ip.Name, port.Name, error.Name }.Contains(timer.Name)
                || !new[] { setSocket, bindSocket, connectSocket, secureSocket }.All(item => item.Name == socket.Name)
                || !new[] { bindFamily, peerFamily, sslFamily }.All(item => item.Name == family.Name)
                || native.Name != options.Name || bindOptions.Name != options.Name || bindArg.Name != bindAddress.Name
                || connectArg.Name != connectAddress.Name || defaultArg.Name != sslOptions.Name
                || finalAddress.Name != config.Name || finalSocket.Name != sslSocket.Name
                || !familyKey.Equals(peerFamilyKey) || !addrKey.Equals(peerAddrKey) || !portKey.Equals(peerPortKey)
                || new HashSet<Atom> { familyKey, addrKey, portKey }.Count != 3
                || !AttributeLiteral.TryNormalizeConstant(additions, out Term nativeAdditions)
                || !AttributeLiteral.TryNormalizeConstant(first, out Term firstOption)
                || !AttributeLiteral.TryNormalizeConstant(second, out Term secondOption)
                || !AttributeLiteral.TryNormalizeConstant(third, out Term thirdOption)
                || new[] { open, merge, lookup, bind, connect, defaults, sslMerge, timeout, secure }.Any(call => call.DynamicRemote || owner.ResolvesErlangBif(call))
                || !entries.TryGetValue((set.Function, 2), out int setEntry)
                || !entries.TryGetValue((start.Function, 2), out int startEntry)
                || ReadConnectRecord(record, family.Name, host.Name, ip.Name, port.Name) is not { } fields)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int transportFailed = Label();
            int setFailed = Label();
            int openFailed = Label();
            int sslFailed = Label();
            int elseFailed = Label();
            EmitDetailRecordTest(0, record.Items.Count, tag, functionInfo);
            owner.EmitTuplePart(0, fields.Address, 3);
            owner.EmitTupleCheck(3, 2, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 4);
            InitSlots(0, 1);
            owner.MoveRegister(XRegisterTag, 3, YRegisterTag, 3);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 4);
            SaveWindowResult(5);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, fields.Family);
            owner.Operand(YRegisterTag, 2);
            owner.EmitConstant(stream, 1);
            Load(2);
            var previous = location;
            EmitWindowCall(open, ref previous);
            SaveWindowResult(1);
            EmitDetailRecordTest(0, 2, opened, openFailed);
            owner.EmitConstant(nativeAdditions, 1);
            owner.EmitConstant(Nil.Value, 2);
            Load(4);
            EmitWindowCall(merge, ref previous);
            LoadTupleField(1, 1, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            Load(1);
            EmitWindowLocal(set, setEntry, ref previous);
            SaveWindowResult(0);
            EmitConfigurationAtomTest(configured, setFailed);
            Load(4, 1);
            owner.EmitConstant(defaultPort.Value, 2);
            InitSlots(0, 4);
            owner.EmitConstant(portOption.Value, 0);
            EmitWindowCall(lookup, ref previous);
            EmitAddress(address.Value, 1);
            Load(1);
            EmitWindowCall(bind, ref previous);
            SaveWindowResult(4);
            EmitConfigurationAtomTest(bound, transportFailed);
            LoadTupleField(3, 0, XRegisterTag, 0);
            LoadTupleField(3, 1, XRegisterTag, 1);
            EmitAddress(null, 2);
            InitSlots(3, 4);
            Load(1);
            EmitWindowCall(connect, ref previous);
            SaveWindowResult(4);
            EmitConfigurationAtomTest(connected, transportFailed);
            owner.InitStackSlot(4);
            EmitWindowCall(defaults, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + 5 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            LoadTupleField(5, fields.Host, XRegisterTag, 1);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(nameKey.Name)), (XRegisterTag, 1));
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            foreach (Term option in new[] { thirdOption, secondOption, firstOption })
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(option);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 1);
            }

            owner.Op(PutList, YRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(Nil.Value, 2);
            owner.InitStackSlot(2);
            EmitWindowCall(sslMerge, ref previous);
            SaveWindowResult(4);
            EmitWindowCall(timeout, ref previous);
            Load(4, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            Load(1);
            InitSlots(1, 4);
            EmitWindowCall(secure, ref previous);
            EmitDetailRecordTest(0, 2, secured, sslFailed);
            owner.EmitTuplePart(0, 1, 1);
            Load(5);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, startEntry);
            owner.Operand(BeamWire.UnsignedTag, 6);
            Mark(transportFailed);
            Load(4);
            JumpTo(sslFailed);
            Mark(setFailed);
            Load(0);
            JumpTo(sslFailed);
            Mark(openFailed);
            Load(1);
            Mark(sslFailed);
            EmitMaybeListenerElse(conditional, errorTag, elseFailed, 6);

            return true;

            void EmitAddress(Term? addressValue, int live)
            {
                owner.Op(PutMapAssoc, LabelTag, 0);
                owner.LiteralOperand(new MapTerm([]));
                owner.Operand(XRegisterTag, 1);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 6);
                foreach (Atom key in new[] { addrKey, familyKey, portKey }.OrderBy(atom => atom, MapKeyOrder))
                {
                    owner.LiteralOperand(key);
                    if (key.Equals(familyKey))
                    {
                        owner.Operand(YRegisterTag, 2);
                    }
                    else if (key.Equals(addrKey) && addressValue is not null)
                    {
                        owner.LiteralOperand(addressValue);
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, key.Equals(portKey) && addressValue is null ? 1 : 0);
                    }
                }
            }

            void InitSlots(int firstSlot, int secondSlot)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(YRegisterTag, firstSlot);
                owner.Operand(YRegisterTag, secondSlot);
            }
        }

        private static (int Address, int Family, int Host)? ReadConnectRecord(
            Pattern.Tuple record, string family, string? host, string ip, string port
        )
        {
            int addressField = -1;
            int familyField = -1;
            int hostField = -1;
            for (int index = 1; index < record.Items.Count; index++)
            {
                switch (record.Items[index])
                {
                    case Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }
                        when addressField < 0 && first.Name == ip && second.Name == port:
                        addressField = index;
                        break;
                    case Pattern.Variable value when value.Name == family && familyField < 0:
                        familyField = index;
                        break;
                    case Pattern.Variable value when value.Name == host && hostField < 0:
                        hostField = index;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    default:
                        return null;
                }
            }

            return addressField >= 0 && familyField >= 0 && (host is null || hostField >= 0)
                ? (addressField, familyField, hostField) : null;
        }
    }
}
