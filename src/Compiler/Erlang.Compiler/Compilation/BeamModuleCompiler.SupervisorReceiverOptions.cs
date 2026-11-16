using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSupervisorReceiverOptions(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable senderArgs, Pattern.Variable receiverArgs],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.List { Tail: null, Items: [
                        Pattern.Variable role, Pattern.Variable host, Pattern.Variable port, Pattern.Variable socket,
                        Pattern.Tuple { Items: [Pattern.Variable map, Pattern.Variable extra, Pattern.Variable trackers] },
                        Pattern.Variable user, Pattern.Variable callback
                    ] }, Value: Expr.Variable matchedArgs } unpack,
                    Expr.Match { Pattern: Pattern.Variable options, Value: Expr.Call {
                        Module: "maps", Function: "get", DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Atom option }, Expr.Variable optionMap, Expr.List { Tail: null, Items: [] }]
                    } },
                    Expr.Case {
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Literal { Value: Atom supervisor }, Expr.List { Tail: null, Items: [
                                Expr.Call parent, Expr.Variable startArgs, Expr.Variable startOptions
                            ] }
                        ] } start,
                        Clauses: [
                            { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                Pattern.Literal { Value: Atom ok }, Pattern.Variable pid
                            ] }], Body: Expr.Sequence { Items: [
                                Expr.Match { Pattern: Pattern.Tuple { Items: [
                                    Pattern.Literal { Value: Atom childOk },
                                    Pattern.Tuple { Items: [Pattern.Variable id, Pattern.Variable receiver, Pattern.Variable type, Pattern.Variable modules] }
                                ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                    Expr.Variable lookupPid, Expr.Literal { Value: Atom child }
                                ] } lookup },
                                Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable recipient,
                                    Right: Expr.Tuple { Items: [Expr.Call notifying, Expr.Literal { Value: Atom tag },
                                        Expr.List { Items: [Expr.Variable sentRole, Expr.Variable sentPid],
                                            Tail: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                                                Function: AutoImportNames.Tl, Arguments: [Expr.Variable tailArgs] } tail
                                        }
                                    ] }
                                } send,
                                Expr.Tuple { Items: [Expr.Literal { Value: Atom returnTag }, Expr.Variable returnPid] }
                            ] } },
                            { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable fallback }
                        ]
                    }
                ] }
            }] || !DistinctBindings(senderArgs, receiverArgs, role, map, options, pid, receiver)
                || !OnlyUnusedBindings(host, port, socket, extra, trackers, user, callback, id, type, modules)
                || matchedArgs.Name != receiverArgs.Name || optionMap.Name != map.Name
                || startArgs.Name != senderArgs.Name || startOptions.Name != options.Name || lookupPid.Name != pid.Name
                || recipient.Name != receiver.Name || sentRole.Name != role.Name || sentPid.Name != pid.Name
                || tailArgs.Name != receiverArgs.Name || returnPid.Name != pid.Name
                || !ok.Equals(childOk) || !ok.Equals(returnTag)
                || error.Name == VariableScopeNames.Wildcard || fallback.Name != error.Name
                || new[] { senderArgs, receiverArgs, role, map, options, host, port, socket, extra, trackers, user, callback }
                    .Any(binding => binding.Name == error.Name)
                || !IsReceiverSelf(parent) || !IsReceiverSelf(notifying) || !owner.ResolvesErlangBif(tail))
            {
                return false;
            }

            int absent = Label();
            int found = Label();
            int rejected = Label();
            int badMap = Label();
            int badChild = Label();
            int badArgs = Label();
            EmitReceiverArgumentMatch(badArgs);
            EmitMapTest(3, badMap);
            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 3);
            owner.Op(GetMapElements, LabelTag, absent);
            owner.EmitTypedRegister(3, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(option);
            owner.Operand(XRegisterTag, 1);
            JumpTo(found);
            Mark(absent);
            owner.EmitConstant(Nil.Value, 1);
            Mark(found);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitReceiverSelf(2);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(supervisor, 0);
            var previous = location;
            EmitWindowCall(start, ref previous);
            SaveWindowResult(1);
            EmitDetailRecordTest(0, 2, ok, rejected);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.EmitConstant(child, 1);
            Load(0);
            EmitWindowCall(lookup, ref previous);
            EmitDetailRecordTest(0, 2, childOk, badChild);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTupleCheck(1, 4, badChild);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords + 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitReceiverSelf(0);
            owner.Op(GetTail, YRegisterTag, 3);
            owner.Operand(XRegisterTag, 2);
            for (int slot = 0; slot <= 2; slot += 2)
            {
                owner.Op(PutList, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 2);
                owner.Operand(XRegisterTag, 2);
            }

            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 2));
            owner.EmitTuplePart(1, 1, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(YRegisterTag, 1, YRegisterTag, 3);
            TrimDetailFrame(3, 1);
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            Load(0);
            owner.EmitFrameReturn(1);
            Mark(rejected);
            owner.EmitFrameReturn(4);
            Mark(badMap);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(Term.A("badmap"));
            owner.Operand(XRegisterTag, 3);
            owner.EmitLine(default);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));
            Mark(badChild);
            owner.EmitLine(CallLocation(lookup, location));
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badArgs);
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 1);

            return true;

            bool OnlyUnusedBindings(params Pattern.Variable[] ignored)
            {
                var names = new HashSet<string>(new[] { senderArgs, receiverArgs, role, map, options, pid, receiver }
                    .Select(binding => binding.Name), StringComparer.Ordinal);
                return ignored.All(binding => binding.Name == VariableScopeNames.Wildcard || names.Add(binding.Name));
            }
        }

        private bool IsReceiverSelf(Expr.Call call) => call is
        {
            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
            Function: AutoImportNames.Self, Arguments: []
        } && owner.ResolvesErlangBif(call);

        private void EmitReceiverSelf(int destination)
        {
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, destination);
        }

        private void EmitReceiverArgumentMatch(int failure)
        {
            owner.EmitNonemptyList(1, failure);
            owner.EmitListParts(1, XRegisterTag, 2, XRegisterTag, 3);
            for (int index = 1; index < 7; index++)
            {
                owner.EmitNonemptyList(3, failure);
                if (index == 4)
                {
                    owner.EmitListParts(3, XRegisterTag, 4, XRegisterTag, 3);
                    owner.EmitTupleCheck(4, 3, failure);
                }
                else
                {
                    owner.Op(GetTail, XRegisterTag, 3);
                    owner.Operand(XRegisterTag, 3);
                }
            }

            owner.EmitNilTest(XRegisterTag, 3, failure);
            owner.EmitTuplePart(4, 0, 3);
        }
    }
}
