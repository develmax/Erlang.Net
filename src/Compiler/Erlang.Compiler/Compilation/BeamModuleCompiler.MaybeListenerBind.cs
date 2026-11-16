using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMaybeListenerBind(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom addressTag }, ..] } address,
                    Pattern.Variable port, Pattern.Tuple { Items: [Pattern.Variable socket, Pattern.Variable backlog] }
                ], Body: Expr.Maybe { Items: [
                    Expr.Match { Pattern: Pattern.Variable sockaddr, Value: Expr.Map { Base: null, Fields: [
                        { Exact: false, Key: Expr.Literal { Value: Atom familyKey }, Value: Expr.Variable family },
                        { Exact: false, Key: Expr.Literal { Value: Atom addrKey }, Value: Expr.Literal addr },
                        { Exact: false, Key: Expr.Literal { Value: Atom portKey }, Value: Expr.Variable portInput }
                    ] } },
                    Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom bound }, Value: Expr.Call {
                        Module: not null, DynamicRemote: false, Arguments: [Expr.Variable bindSocket, Expr.Variable bindAddress]
                    } bind },
                    Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom listening }, Value: Expr.Call {
                        Module: not null, DynamicRemote: false, Arguments: [Expr.Variable listenSocket, Expr.Variable listenBacklog]
                    } listen },
                    Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom named }, MapPattern { Fields: [
                            { Key: Expr.Literal { Value: Atom matchedAddr }, Value: Pattern.Variable ip },
                            { Key: Expr.Literal { Value: Atom matchedPort }, Value: Pattern.Variable assigned }
                        ] }
                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable nameSocket] } name },
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom result }, Expr.Tuple { Items: [
                        Expr.Variable returnedSocket, Expr.Tuple { Items: [Expr.Variable returnedIp, Expr.Variable returnedPort] }
                    ] }] }
                ], Clauses: [{ Guard: null, Patterns: [AliasPattern errorAlias], Body: Expr.Variable returned }] } conditional
            }] || !TryReadPatternAlias(errorAlias, out var error, out var errorShape)
                || errorShape is not Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom errorTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ] }
                || !DistinctBindings(port, socket, backlog, sockaddr, ip, assigned, new(family.Name))
                || !DistinctBindings(port, socket, backlog, error, new(family.Name))
                || error.Name != returned.Name || portInput.Name != port.Name || bindSocket.Name != socket.Name
                || bindAddress.Name != sockaddr.Name || listenSocket.Name != socket.Name || listenBacklog.Name != backlog.Name
                || nameSocket.Name != socket.Name || returnedSocket.Name != socket.Name
                || returnedIp.Name != ip.Name || returnedPort.Name != assigned.Name
                || !matchedAddr.Equals(addrKey) || !matchedPort.Equals(portKey)
                || new HashSet<Atom> { familyKey, addrKey, portKey }.Count != 3
                || owner.ResolvesErlangBif(bind) || owner.ResolvesErlangBif(listen) || owner.ResolvesErlangBif(name))
            {
                return false;
            }

            int familyField = -1;
            for (int index = 1; index < address.Items.Count; index++)
            {
                if (address.Items[index] is Pattern.Variable variable && variable.Name == family.Name && familyField < 0)
                {
                    familyField = index;
                }
                else if (address.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            if (familyField < 0)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int listenFailed = Label();
            int bindFailed = Label();
            int nameFailed = Label();
            int elseFailed = Label();
            EmitDetailRecordTest(0, address.Items.Count, addressTag, functionInfo);
            owner.EmitTupleCheck(2, 2, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            InitSlots(0, 1);
            owner.EmitTuplePart(0, familyField, 0);
            SaveTupleField(1, 2);
            owner.Op(PutMapAssoc, LabelTag, 0);
            owner.LiteralOperand(new MapTerm([]));
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 6);
            (Atom Key, Term? Value, int Register)[] fields = [(addrKey, addr.Value, 0), (familyKey, null, 0), (portKey, null, 1)];
            foreach (var field in fields.OrderBy(item => item.Key.Name, StringComparer.Ordinal))
            {
                owner.LiteralOperand(field.Key);
                if (field.Value is { } value)
                {
                    owner.LiteralOperand(value);
                }
                else
                {
                    owner.Operand(XRegisterTag, field.Register);
                }
            }

            SaveTupleField(0, 1);
            Load(1);
            var previous = location;
            EmitWindowCall(bind, ref previous);
            SaveWindowResult(0);
            EmitConfigurationAtomTest(bound, bindFailed);
            Load(2, 1);
            InitSlots(0, 2);
            Load(1);
            EmitWindowCall(listen, ref previous);
            SaveWindowResult(2);
            EmitConfigurationAtomTest(listening, listenFailed);
            owner.InitStackSlot(2);
            Load(1);
            EmitWindowCall(name, ref previous);
            EmitDetailRecordTest(0, 2, named, nameFailed);
            owner.EmitTuplePart(0, 1, 1);
            EmitMapTest(1, nameFailed);
            owner.Op(GetMapElements, LabelTag, nameFailed);
            owner.EmitTypedRegister(1, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(portKey);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(addrKey);
            owner.Operand(XRegisterTag, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 2), (XRegisterTag, 1));
            owner.EmitTagMatchTuple(0, (YRegisterTag, 1), (XRegisterTag, 0));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(result.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(3);
            Mark(listenFailed);
            Load(2);
            JumpTo(nameFailed);
            Mark(bindFailed);
            Load(0);
            Mark(nameFailed);
            EmitMaybeListenerElse(conditional, errorTag, elseFailed);

            return true;

            void SaveTupleField(int field, int slot)
            {
                owner.Op(GetTupleElement, XRegisterTag, 2);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(YRegisterTag, slot);
            }

            void InitSlots(int first, int second)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(YRegisterTag, first);
                owner.Operand(YRegisterTag, second);
            }
        }
    }
}
