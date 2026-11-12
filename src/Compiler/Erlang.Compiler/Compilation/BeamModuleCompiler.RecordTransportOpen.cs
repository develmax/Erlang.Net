using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordTransportOpen(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [
                    Pattern.Variable host, Pattern.Variable port,
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom recordTag }, ..] } record,
                    Pattern.Variable timeout
                ],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable transport, Value: Expr.Call {
                        Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Element, DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable callback]
                    } element },
                    Expr.Case { Value: Expr.Call { DynamicRemote: true, Arguments: [
                        Expr.Variable target, Expr.Literal { Value: Atom member }, Expr.List { Tail: null, Items: [
                            Expr.Literal { Value: Integer initialPort }, Expr.Binary { Operator: ErlangOperators.Append,
                                Left: Expr.Variable options,
                                Right: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } defaults
                            }
                        ] }
                    ] } open, Clauses: [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom ok }, Pattern.Variable socket
                        ] }], Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Literal { Value: Atom role }, Expr.Variable hostArg, Expr.Variable portArg,
                            Expr.Tuple { Items: [
                                Expr.Tuple { Items: [Expr.Variable addrHost, Expr.Variable addrPort] }, Expr.Variable socketArg
                            ] },
                            Expr.Tuple { Items: [Expr.Variable firstOption, Expr.Variable secondOption, Expr.Literal { Value: Atom extra }] },
                            Expr.Call { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Self,
                                DynamicRemote: false, Arguments: [] } self,
                            Expr.Variable callbackArg, Expr.Variable timeoutArg
                        ] } start },
                        { Guard: null, Patterns: [AliasPattern {
                            Left: Pattern.Tuple { Items: [
                                Pattern.Literal { Value: Atom error }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ] }, Right: Pattern.Variable failed
                        }], Body: Expr.Variable returned }
                    ] } dispatch
                ] }
            }] || index.Value != 1 || !owner.ResolvesErlangBif(element) || !owner.ResolvesErlangBif(self)
                || owner.ResolvesErlangBif(start) || !entries.TryGetValue((defaults.Function, 0), out int defaultsEntry)
                || !DistinctBindings(host, port, timeout, transport, socket, failed)
                || target.Name != transport.Name || hostArg.Name != host.Name || addrHost.Name != host.Name
                || portArg.Name != port.Name || addrPort.Name != port.Name || socketArg.Name != socket.Name
                || callbackArg.Name != callback.Name || timeoutArg.Name != timeout.Name || returned.Name != failed.Name
                || error.Equals(ok))
            {
                return false;
            }

            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            var outerNames = new HashSet<string>([host.Name, port.Name, timeout.Name, transport.Name, socket.Name, failed.Name],
                StringComparer.Ordinal);
            for (int field = 1; field < record.Items.Count; field++)
            {
                if (record.Items[field] is not Pattern.Variable variable
                    || variable.Name != VariableScopeNames.Wildcard
                    && (outerNames.Contains(variable.Name) || !fields.TryAdd(variable.Name, field)))
                {
                    return false;
                }
            }

            if (!fields.TryGetValue(callback.Name, out int callbackField) || !fields.TryGetValue(options.Name, out int optionsField)
                || !fields.TryGetValue(firstOption.Name, out int firstOptionField)
                || !fields.TryGetValue(secondOption.Name, out int secondOptionField))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int success = Label();
            int failure = Label();
            int unmatched = Label();
            EmitDetailRecordTest(2, record.Items.Count, recordTag, functionInfo);
            owner.EmitTuplePart(2, callbackField, 4);
            var previous = location;
            owner.EmitCallLine(element, previous);
            previous = CallLocation(element, previous);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
            owner.LiteralOperand(index);
            owner.Operand(XRegisterTag, 4);
            owner.Operand(XRegisterTag, 5);
            owner.Op(Allocate, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 6);
            for (int slot = 0; slot < 6; slot++)
            {
                owner.MoveRegister(XRegisterTag, 5 - slot, YRegisterTag, slot);
            }

            EmitWindowLocal(defaults, defaultsEntry, ref previous);
            LoadField(optionsField, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            Load(0, 2);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(member, 3);
            owner.InitStackSlot(0);
            owner.EmitConstant(initialPort, 0);
            owner.EmitCallLine(open, previous);
            previous = CallLocation(open, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 2);
            owner.EmitTupleCheck(0, 2, unmatched);
            owner.EmitTuplePart(0, 0, 1);
            if (TermOrder.Compare(error, ok, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, unmatched, error.Name, failure, ok.Name, success);
            }
            else
            {
                owner.EmitAtomSelection(1, unmatched, ok.Name, success, error.Name, failure);
            }

            Mark(success);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 10);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            LoadField(firstOptionField, 1);
            LoadField(secondOptionField, 2);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, 3);
            owner.EmitTagMatchTuple(4, (YRegisterTag, 5), (YRegisterTag, 4));
            owner.EmitTagMatchTuple(0, (XRegisterTag, 4), (XRegisterTag, 0));
            owner.EmitTagMatchTuple(4, (XRegisterTag, 1), (XRegisterTag, 2), (AtomTag, owner.Intern(extra.Name)));
            owner.MoveRegister(XRegisterTag, 3, XRegisterTag, 5);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 3);
            Load(1, 6);
            Load(2, 7);
            Load(4, 2);
            Load(5, 1);
            owner.EmitConstant(role, 0);
            EmitConfigurationTail(start, 6, previous);
            Mark(failure);
            owner.EmitFrameReturn(6);
            Mark(unmatched);
            EmitConfigurationCaseFailure(dispatch, previous);

            return true;

            void LoadField(int field, int destination)
            {
                owner.Op(GetTupleElement, YRegisterTag, 3);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, destination);
            }
        }
    }
}
