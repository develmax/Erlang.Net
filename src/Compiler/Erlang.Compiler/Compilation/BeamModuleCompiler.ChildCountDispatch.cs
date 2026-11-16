using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitChildCountDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parameter],
                    Body: Expr.Sequence { Items: [
                        Expr.Match { Pattern: Pattern.Variable supervisor, Value: Expr.Call
                            { Module: null, DynamicRemote: false, Arguments: [Expr.Variable selector] } nameCall },
                        Expr.Match { Pattern: Pattern.Variable children, Value: Expr.Call
                            { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable countArg] } countCall },
                        Expr.Match { Pattern: Pattern.Variable workers, Value: Expr.Call
                            { Module: not null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Atom key }, Expr.Variable countResult] } keyCall },
                        Expr.Case { Value: Expr.Variable count, Clauses: [var zero, var one] } dispatch
                    ] }
                }
            ] || !DistinctBindings(parameter, supervisor, children, workers)
                || selector.Name != parameter.Name || countArg.Name != supervisor.Name
                || countResult.Name != children.Name || count.Name != workers.Name
                || !entries.TryGetValue((nameCall.Function, 1), out int nameEntry)
                || zero is not { Guard: null, Patterns: [Pattern.Literal { Value: Integer zeroKey }], Body: Expr.Case
                    {
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Variable startArg, Expr.List { Tail: null, Items: [
                                Expr.Literal { Value: Atom childId },
                                Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Atom option }] } options
                            ] }
                        ] } start,
                        Clauses: [
                            { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                Pattern.Literal { Value: Atom error }, Pattern.Tuple { Items: [
                                    Pattern.Literal { Value: Atom conflict }, Pattern.Variable existing
                                ] }
                            ] }], Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom recovered }, Expr.Variable recoveredChild] } },
                            { Guard: null, Patterns: [AliasPattern { Left: Pattern.Tuple { Items: [
                                Pattern.Literal { Value: Atom ok }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ] }, Right: Pattern.Variable reply }], Body: Expr.Variable returned }
                        ]
                    } startCase }
                || one is not { Guard: null, Patterns: [Pattern.Literal { Value: Integer oneKey }], Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.List { Tail: null, Items: [Pattern.Tuple { Items: [
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable current,
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ] }] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable listArg] } lookup } match,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom found }, Expr.Variable foundChild] }
                ] } }
                || zeroKey.Value != 0 || oneKey.Value != 1
                || startArg.Name != supervisor.Name || listArg.Name != supervisor.Name
                || existing.Name != recoveredChild.Name || current.Name != foundChild.Name || returned.Name != reply.Name
                || !ok.Equals(recovered) || !ok.Equals(found) || error.Equals(ok)
                || !DistinctBindings(parameter, supervisor, children, workers, existing)
                || !DistinctBindings(parameter, supervisor, children, workers, current)
                || !DistinctBindings(parameter, supervisor, children, workers, reply))
            {
                return false;
            }

            int reuse = Label();
            int create = Label();
            int success = Label();
            int alreadyStarted = Label();
            int badChild = Label();
            int badStart = Label();
            int badCount = Label();
            var previous = location;
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.InitStackSlot(0);
            EmitWindowLocal(nameCall, nameEntry, ref previous);
            SaveWindowResult(0);
            EmitWindowCall(countCall, ref previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(key, 0);
            EmitWindowCall(keyCall, ref previous);
            EmitChildDispatch(0, badCount, (zeroKey, create), (oneKey, reuse));

            Mark(reuse);
            Load(0);
            TrimDetailFrame(1, 0);
            var lookupLocation = previous;
            EmitWindowCall(lookup, ref lookupLocation);
            owner.EmitNonemptyList(0, badChild);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTupleCheck(1, 4, badChild);
            owner.EmitNilTest(XRegisterTag, 2, badChild);
            EmitChildReply(ok, 0);

            Mark(create);
            owner.EmitConstant(option, 0);
            EmitWindowCall(options, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, AtomTag, owner.Intern(childId.Name));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            Load(0);
            owner.InitStackSlot(0);
            EmitWindowCall(start, ref previous);
            owner.EmitTupleCheck(0, 2, badStart);
            owner.EmitTuplePart(0, 0, 1);
            EmitChildDispatch(1, badStart, (error, alreadyStarted), (ok, success));

            Mark(success);
            owner.EmitFrameReturn(1);
            Mark(alreadyStarted);
            owner.EmitTuplePart(0, 1, 1);
            EmitDetailRecordTest(1, 2, conflict, badStart);
            EmitChildReply(ok, 1);

            Mark(badChild);
            owner.EmitLine(new(match.PreprocessorFile ?? lookupLocation.File, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badStart);
            owner.EmitLine(new(previous.File, startCase.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(badCount);
            owner.EmitLine(new(location.File, dispatch.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private void EmitChildDispatch(
            int register,
            int failure,
            (Term Key, int Label) first,
            (Term Key, int Label) second
        )
        {
            owner.Op(SelectValue, XRegisterTag, register);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(first.Key);
            owner.Operand(LabelTag, first.Label);
            owner.LiteralOperand(second.Key);
            owner.Operand(LabelTag, second.Label);
        }

        private void EmitChildReply(Atom tag, int slots)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(slots);
        }
    }
}
