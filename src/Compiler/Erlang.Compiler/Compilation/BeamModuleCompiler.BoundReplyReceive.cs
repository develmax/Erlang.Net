using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitBoundReplyReceive(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [
                    Pattern.Tuple { Items: [Pattern.Variable input, Pattern.Variable ignored] },
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom recordTag }, ..] } record,
                    Pattern.Variable timeout
                ],
                Body: Expr.Case {
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable listener, Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.Self, Arguments: [] } self
                    ] } accept,
                    Clauses: [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom successTag }, Pattern.Variable sender
                        ] }], Body: Expr.Receive { Timeout: null, After: null, Clauses: [
                            { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                Pattern.Variable expectedSender, Pattern.Literal { Value: Atom messageTag }, Pattern.Variable payload
                            ] }], Body: Expr.Tuple { Items: [
                                Expr.Literal { Value: Atom resultTag }, Expr.Variable received
                            ] } }
                        ] } },
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom errorTag }, Pattern.Variable reason
                        ] }], Body: Expr.Tuple { Items: [
                            Expr.Literal { Value: Atom returnError }, Expr.Variable returnedReason
                        ] } }
                    ]
                }
            }] || listener.Name != input.Name || expectedSender.Name != sender.Name || received.Name != payload.Name
                || returnedReason.Name != reason.Name || !errorTag.Equals(returnError) || successTag.Equals(errorTag)
                || !owner.ResolvesErlangBif(self) || owner.ResolvesErlangBif(accept)
                || !DistinctBindings(input, sender, payload) || !DistinctBindings(input, reason)
                || record.Items.Skip(1).Any(field => field is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || new[] { ignored, timeout }.Any(variable => variable.Name != VariableScopeNames.Wildcard
                    && (variable.Name == input.Name || variable.Name == sender.Name
                        || variable.Name == payload.Name || variable.Name == reason.Name))
                || ignored.Name != VariableScopeNames.Wildcard && ignored.Name == timeout.Name)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int success = Label();
            int scan = Label();
            int skip = Label();
            int wait = Label();
            int error = Label();
            int unmatched = Label();
            owner.EmitTupleCheck(0, 2, functionInfo);
            EmitDetailRecordTest(1, record.Items.Count, recordTag, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.InitStackSlot(0);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, 1);
            owner.EmitTuplePart(0, 0, 0);
            var previous = location;
            EmitWindowCall(accept, ref previous);
            owner.EmitTupleCheck(0, 2, unmatched);
            owner.EmitTuplePart(0, 0, 1);
            if (TermOrder.Compare(errorTag, successTag, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, unmatched, errorTag.Name, error, successTag.Name, success);
            }
            else
            {
                owner.EmitAtomSelection(1, unmatched, successTag.Name, success, errorTag.Name, error);
            }

            Mark(success);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleCheck(0, 3, skip);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 2);
            owner.LiteralOperand(messageTag);
            owner.Op(ExactEqualTest, LabelTag, skip);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 2, 0);
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(resultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(1);
            Mark(skip);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);
            Mark(error);
            owner.EmitFrameReturn(1);
            Mark(unmatched);
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }
    }
}
