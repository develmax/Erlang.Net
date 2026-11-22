using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOrderedPairTake(FunctionDefinition function, int functionInfo)
        {
            const int input = 1;
            const int accumulator = 2;
            const int head = 3;
            const int tail = 4;
            const int payloadRegister = 5;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 3 || function.Clauses is not
                    [var before, var later, { Guard: null } matched, { Guard: null } nil])
            {
                return false;
            }

            if (ReadOrderedPairUpdateArm(before, input) is not { } earlier
                || ReadOrderedPairUpdateArm(later, input) is not { HeadAlias: { } pair } scan
                || ReadOrderedPairUpdateArm(matched, input) is not { } hit
                || !HasLookupComparison(
                    before.Guard,
                    ErlangOperators.Less,
                    earlier.Parameters[0],
                    earlier.Index
                )
                || !HasLookupComparison(
                    later.Guard,
                    ErlangOperators.Greater,
                    scan.Parameters[0],
                    scan.Index
                )
                || later.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments:
                    [
                        Expr.Variable search,
                        Expr.Variable nextTail,
                        Expr.List { Items: [Expr.Variable retained], Tail: Expr.Variable previous }
                    ]
                } recur
                || recur.Function != function.Name || !entries.TryGetValue((recur.Function, 3), out int entry)
                || search.Name != scan.Parameters[0].Name || nextTail.Name != scan.Tail.Name
                || retained.Name != pair || previous.Name != scan.Parameters[1].Name
                || matched.Body is not Expr.Tuple
                {
                    Items:
                    [
                        Expr.Variable payload,
                        Expr.Call { Arguments: [Expr.Variable saved, Expr.Variable suffix] } finish
                    ]
                }
                || payload.Name != hit.Value.Name || saved.Name != hit.Parameters[1].Name || suffix.Name != hit.Tail.Name
                || !TryResolveOrdinaryCall(finish, out string? callee)
                || nil.Patterns is not [Pattern.Variable nilKey, var nilShape, Pattern.Variable nilAcc]
                || !DistinctBindings(new[] { nilKey, nilAcc }.Where(binding => binding.Name != VariableScopeNames.Wildcard).ToArray())
                || ListEntryTest(nilShape) != IsNil
                || !AttributeLiteral.TryNormalizeConstant(before.Body, out Term failed)
                || !AttributeLiteral.TryNormalizeConstant(nil.Body, out Term exhausted) || !failed.Equals(exhausted))
            {
                return false;
            }

            int found = Label();
            int empty = Label();
            int miss = Label();
            owner.EmitNonemptyList(input, empty);
            owner.EmitListParts(
                input,
                XRegisterTag,
                head,
                XRegisterTag,
                tail
            );
            owner.EmitTupleCheck(head, 2, functionInfo);
            owner.EmitTuplePart(head, 0, input);
            owner.frameEntryTupleReads.Add((entry, head, 1, payloadRegister));
            owner.EmitTuplePart(head, 1, payloadRegister);
            owner.Op(IsGreaterOrEqual, LabelTag, miss);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, input);
            EmitOrderedPairLess(input, 0, found);

            // Only the original pair and accumulated tail cross the recursive
            // edge. The payload projection remains in X5 for the other arm.
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, payloadRegister);
            owner.Op(PutList, XRegisterTag, head);
            owner.Operand(XRegisterTag, accumulator);
            owner.Operand(XRegisterTag, accumulator);
            owner.MoveRegister(
                XRegisterTag,
                tail,
                XRegisterTag,
                input
            );
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);

            Mark(found);
            EmitFrameAllocation(payloadRegister + 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                payloadRegister,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                tail,
                XRegisterTag,
                1
            );
            owner.MoveToX0(XRegisterTag, accumulator);
            owner.EmitCallLine(finish, location);
            Call(callee, finish.Function, 2);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            Mark(miss);
            owner.EmitConstant(failed, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
