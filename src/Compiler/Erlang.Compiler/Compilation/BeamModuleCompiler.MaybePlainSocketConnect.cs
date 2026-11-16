using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMaybePlainSocketConnect(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [AliasPattern alias, Pattern.Variable timer, Pattern.Variable options],
                Body: Expr.Maybe
                {
                    Items: [
                        Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom opened }, Pattern.Variable socket
                        ] }, Value: Expr.Call { Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Variable family, Expr.Literal { Value: Atom stream }] } open },
                        Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom configured },
                            Value: Expr.Call { Module: null, DynamicRemote: false,
                                Arguments: [Expr.Variable setSocket, Expr.Variable setOptions] } set },
                        Expr.Match { Pattern: Pattern.Variable address, Value: Expr.Map { Base: null, Fields: [
                            { Exact: false, Key: Expr.Literal { Value: Atom familyKey }, Value: Expr.Variable addressFamily },
                            { Exact: false, Key: Expr.Literal { Value: Atom addrKey }, Value: Expr.Variable ip },
                            { Exact: false, Key: Expr.Literal { Value: Atom portKey }, Value: Expr.Variable port }
                        ] } },
                        Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom connected },
                            Value: Expr.Call { Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable connectSocket, Expr.Variable connectAddress] } connect },
                        Expr.Call { Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable startConfig, Expr.Variable startSocket] } start
                    ],
                    Clauses: [{ Guard: null, Patterns: [AliasPattern errorAlias], Body: Expr.Variable returned }]
                } conditional
            }] || !TryReadPatternAlias(alias, out var config, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } record
                || !TryReadPatternAlias(errorAlias, out var error, out var errorShape)
                || errorShape is not Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom errorTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ] }
                || !DistinctBindings(config, options, socket, address, new(family.Name), new(ip.Name), new(port.Name))
                || !DistinctBindings(config, options, error, new(family.Name), new(ip.Name), new(port.Name))
                || timer.Name != VariableScopeNames.Wildcard && new[] { config.Name, options.Name, socket.Name, address.Name,
                    family.Name, ip.Name, port.Name, error.Name }.Contains(timer.Name)
                || error.Name != returned.Name || addressFamily.Name != family.Name
                || setSocket.Name != socket.Name || setOptions.Name != options.Name
                || connectSocket.Name != socket.Name || connectAddress.Name != address.Name
                || startConfig.Name != config.Name || startSocket.Name != socket.Name
                || new HashSet<Atom> { familyKey, addrKey, portKey }.Count != 3
                || owner.ResolvesErlangBif(open) || owner.ResolvesErlangBif(connect)
                || !entries.TryGetValue((set.Function, 2), out int setEntry)
                || !entries.TryGetValue((start.Function, 2), out int startEntry)
                || ReadConnectRecord(record, family.Name, null, ip.Name, port.Name) is not { } fields)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int setFailed = Label();
            int openFailed = Label();
            int connectFailed = Label();
            int elseFailed = Label();
            EmitDetailRecordTest(0, record.Items.Count, tag, functionInfo);
            owner.EmitTuplePart(0, fields.Address, 3);
            owner.EmitTupleCheck(3, 2, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.InitStackSlot(0);
            owner.MoveRegister(XRegisterTag, 3, YRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 3);
            SaveWindowResult(4);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, fields.Family);
            owner.Operand(YRegisterTag, 1);
            owner.EmitConstant(stream, 1);
            Load(1);
            var previous = location;
            EmitWindowCall(open, ref previous);
            SaveWindowResult(0);
            EmitDetailRecordTest(0, 2, opened, openFailed);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            Load(3, 1);
            owner.InitStackSlot(3);
            Load(0);
            EmitWindowLocal(set, setEntry, ref previous);
            SaveWindowResult(3);
            EmitConfigurationAtomTest(configured, setFailed);
            LoadTupleField(2, 0, XRegisterTag, 0);
            LoadTupleField(2, 1, XRegisterTag, 1);
            owner.Op(PutMapAssoc, LabelTag, 0);
            owner.LiteralOperand(new MapTerm([]));
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 6);
            foreach (Atom key in new[] { addrKey, familyKey, portKey }.OrderBy(atom => atom, MapKeyOrder))
            {
                owner.LiteralOperand(key);
                if (key.Equals(familyKey))
                {
                    owner.Operand(YRegisterTag, 1);
                }
                else
                {
                    owner.Operand(XRegisterTag, key.Equals(addrKey) ? 0 : 1);
                }
            }
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 3);
            for (int slot = 1; slot <= 3; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            Load(0);
            EmitWindowCall(connect, ref previous);
            EmitConfigurationAtomTest(connected, connectFailed);
            Load(0, 1);
            Load(4);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, startEntry);
            owner.Operand(BeamWire.UnsignedTag, 5);
            Mark(setFailed);
            Load(3);
            JumpTo(connectFailed);
            Mark(openFailed);
            Load(0);
            Mark(connectFailed);
            EmitMaybeListenerElse(conditional, errorTag, elseFailed, 5);

            return true;
        }
    }
}
