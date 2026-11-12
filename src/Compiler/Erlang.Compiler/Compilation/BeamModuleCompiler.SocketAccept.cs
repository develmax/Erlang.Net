using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketAccept(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable address, Pattern.Variable listener],
                Body: Expr.Case
                {
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable listenArg] } accept,
                    Clauses: [
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom ok }, Pattern.Variable socket] }],
                            Body: Expr.Maybe
                            {
                                Items: [
                                    Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                                        Pattern.Literal { Value: Atom localTag },
                                        MapPattern { Fields: [{ Key: Expr.Literal { Value: Atom localKey }, Value: Pattern.Variable ip }] }
                                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false,
                                        Arguments: [Expr.Variable localSocket] } local },
                                    Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                                        Pattern.Literal { Value: Atom peerTag },
                                        MapPattern { Fields: [
                                            { Key: Expr.Literal { Value: Atom peerKey }, Value: Pattern.Variable peerIp },
                                            { Key: Expr.Literal { Value: Atom portKey }, Value: Pattern.Variable port }
                                        ] }
                                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false,
                                        Arguments: [Expr.Variable peerSocket] } peer },
                                    Expr.Call { Module: not null, DynamicRemote: false,
                                        Arguments: [Expr.Variable checkIp, Expr.Variable checkPeer] } check,
                                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                                        Expr.Variable finalAddress, Expr.Variable finalSocket,
                                        Expr.Variable finalPeer, Expr.Variable finalPort
                                    ] } handshake
                                ],
                                Clauses: [{
                                    Guard: null,
                                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom innerError }, Pattern.Variable innerReason] }],
                                    Body: Expr.Sequence { Items: [
                                        Expr.Match { Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                            Value: Expr.Call { Module: not null, DynamicRemote: false,
                                                Arguments: [Expr.Variable closeSocket] } close },
                                        Expr.Call innerExit
                                    ] }
                                }]
                            } conditional
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom error }, Pattern.Variable reason] }],
                            Body: Expr.Call outerExit
                        }
                    ]
                }
            }] || listenArg.Name != listener.Name || !ok.Equals(localTag) || !ok.Equals(peerTag) || ok.Equals(error)
                || !error.Equals(innerError) || peerKey.Equals(portKey)
                || !DistinctBindings(address, listener, socket, ip, peerIp, port)
                || !DistinctBindings(address, listener, reason) || !DistinctBindings(address, listener, socket, innerReason)
                || !new[] { localSocket, peerSocket, closeSocket, finalSocket }.All(arg => arg.Name == socket.Name)
                || checkIp.Name != ip.Name || checkPeer.Name != peerIp.Name || finalAddress.Name != address.Name
                || finalPeer.Name != peerIp.Name || finalPort.Name != port.Name
                || !ReadSocketExit(innerExit, innerReason, out Atom innerExitTag)
                || !ReadSocketExit(outerExit, reason, out Atom outerExitTag)
                || new[] { accept, local, peer, check, close }.Any(owner.ResolvesErlangBif)
                || !entries.TryGetValue((handshake.Function, 4), out int target))
            {
                return false;
            }

            int accepted = Label();
            int peerFailed = Label();
            int localFailed = Label();
            int cleanup = Label();
            int elseFailed = Label();
            int acceptFailed = Label();
            int caseFailed = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            for (int slot = 0; slot < 4; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            SaveWindowResult(4);
            owner.MoveToX0(XRegisterTag, 1);
            var previous = location;
            EmitWindowCall(accept, ref previous);
            owner.EmitTupleCheck(0, 2, caseFailed);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 3);
            if (TermOrder.Compare(error, ok, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, caseFailed, error.Name, acceptFailed, ok.Name, accepted);
            }
            else
            {
                owner.EmitAtomSelection(1, caseFailed, ok.Name, accepted, error.Name, acceptFailed);
            }
            Mark(accepted);
            Load(3);
            EmitWindowCall(local, ref previous);
            SaveWindowResult(2);
            EmitDetailRecordTest(0, 2, ok, localFailed);
            owner.EmitTuplePart(0, 1, 0);
            EmitMapTest(0, localFailed);
            MapFields(0, localFailed, (localKey, 1));
            owner.InitStackSlot(2);
            Load(3);
            EmitWindowCall(peer, ref previous);
            EmitDetailRecordTest(0, 2, ok, peerFailed);
            owner.EmitTuplePart(0, 1, 1);
            EmitMapTest(1, peerFailed);
            MapFields(1, peerFailed, (portKey, 0), (peerKey, 2));
            Load(2, 1);
            Load(1);
            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 1);
            Trim(1, 4);
            EmitWindowCall(check, ref previous);
            for (int slot = 0; slot < 4; slot++)
            {
                Load(slot, 3 - slot);
            }
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 4);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, 4);
            Mark(peerFailed);
            SaveWindowResult(4);
            JumpTo(cleanup);
            Mark(localFailed);
            owner.MoveRegister(YRegisterTag, 2, YRegisterTag, 4);
            Mark(cleanup);
            owner.Op(IsTaggedTuple, LabelTag, elseFailed);
            owner.Operand(YRegisterTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(error);
            Load(3);
            Trim(4, 1);
            EmitWindowCall(close, ref previous);
            HeapTuple();
            LoadTupleField(0, 1, XRegisterTag, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(innerExitTag.Name)), (XRegisterTag, 0));
            owner.EmitCallLine(innerExit, previous);
            TailFailure(AutoImportNames.Exit, 1);
            Mark(elseFailed);
            HeapTuple();
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(ErlangErrorReasons.ElseClause)), (YRegisterTag, 4));
            owner.EmitLine(new(conditional.PreprocessorFile ?? location.File, conditional.SourceLine));
            TailFailure(AutoImportNames.Error, 5);
            Mark(acceptFailed);
            HeapTuple();
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(outerExitTag.Name)), (YRegisterTag, 3));
            owner.EmitCallLine(outerExit, location);
            TailFailure(AutoImportNames.Exit, 5);
            Mark(caseFailed);
            owner.EmitLine(CallLocation(accept, location));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;

            void MapFields(int register, int failure, params (Atom Key, int Slot)[] fields)
            {
                owner.Op(GetMapElements, LabelTag, failure);
                owner.EmitTypedRegister(register, MapTypeBits);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, fields.Length * 2);
                foreach (var (key, slot) in fields)
                {
                    owner.LiteralOperand(key);
                    owner.Operand(YRegisterTag, slot);
                }
            }

            void Trim(int removed, int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, removed);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            void HeapTuple()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }

            void TailFailure(string functionName, int slots)
            {
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, functionName, 1));
                owner.Operand(BeamWire.UnsignedTag, slots);
            }
        }

        private bool ReadSocketExit(Expr.Call call, Pattern.Variable reason, out Atom tag)
        {
            if (call is { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: AutoImportNames.Exit,
                Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom value }, Expr.Variable input] }] }
                && input.Name == reason.Name && owner.ResolvesErlangBif(call))
            {
                tag = value;
                return true;
            }

            tag = null!;
            return false;
        }
    }
}
