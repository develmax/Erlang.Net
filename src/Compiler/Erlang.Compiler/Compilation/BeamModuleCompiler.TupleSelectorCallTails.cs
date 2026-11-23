using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleSelectorCallTails(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Tuple left], Body: Expr.Call leftCall },
                { Guard: null, Patterns: [Pattern.Tuple right], Body: Expr.Call rightCall }
                ]
                || left.Items.Count != right.Items.Count
                || ReadSelectorTail(left, leftCall) is not { } first
                || ReadSelectorTail(right, rightCall) is not { } second
                || first.Selector != second.Selector || first.Key.Equals(second.Key)
                || !first.Fields.SequenceEqual(second.Fields)
                || (first.Inner is null) == (second.Inner is null))
            {
                return false;
            }

            if (TermOrder.Compare(first.Key, second.Key, exact: true) > 0)
            {
                (first, second) = (second, first);
            }

            const int selectorRegister = 3;
            int higher = Label();
            int lower = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                left.Items.Count,
                functionInfo
            );
            for (int index = 0; index < first.Fields.Length; index++)
            {
                owner.EmitTuplePart(0, first.Fields[index], index + 1);
            }

            owner.EmitTuplePart(0, first.Selector, selectorRegister);
            owner.EmitAtomSelection(
                selectorRegister,
                functionInfo,
                first.Key.Name,
                lower,
                second.Key.Name,
                higher
            );
            EmitBranch(second, higher);
            EmitBranch(first, lower);

            return true;

            void EmitBranch(TupleSelectorTail branch, int label)
            {
                Mark(label);
                if (branch.Inner is not { } inner)
                {
                    owner.EmitOrderedTailMoves([(0, 1), (1, 2)], []);
                    EmitRegisterTailTarget(branch.Tail, branch.Tail.Arguments.Count);

                    return;
                }

                // Both projected fields survive the call. The tuple selector does not.
                const int frame = 2;
                EmitFrameAllocation(selectorRegister, frame);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
                owner.MoveRegister(
                    XRegisterTag,
                    2,
                    YRegisterTag,
                    1
                );
                EmitEntryCall(inner);
                owner.EmitOrderedTailMoves([(2, 0)], [], [(0, 0), (1, 1)]);
                EmitPreparedFrameTail(new EntryCall(
                    branch.Tail,
                    null,
                    [],
                    []
                ), CallLocation(inner.Call, location), frame);
            }
        }

        private TupleSelectorTail? ReadSelectorTail(Pattern.Tuple pattern, Expr.Call call)
        {
            if (!TryResolveOrdinaryCallTarget(call, out string? module) || module is not null)
            {
                return null;
            }

            Atom? key = null;
            int selector = -1;
            var fields = new List<int>(2);
            var bindings = new List<Pattern.Variable>(2);
            for (int field = 0; field < pattern.Items.Count; field++)
            {
                switch (pattern.Items[field])
                {
                    case Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding when key is null:
                        fields.Add(field);
                        bindings.Add(binding);
                        break;
                    case Pattern.Literal { Value: Atom atom } when key is null:
                        selector = field;
                        key = atom;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard } or Erlang.Pattern.Any:
                        break;
                    default:
                        return null;
                }
            }

            if (key is null || bindings is not [var left, var right] || !DistinctBindings(left, right)
                || call.Arguments is not [Expr.Variable first, Expr.Variable second, ..]
                || first.Name != left.Name || second.Name != right.Name)
            {
                return null;
            }

            EntryCall? inner = null;
            if (call.Arguments.Count == 3 && call.Arguments[2] is Expr.Call computed
                && (inner = ReadEntryCall(computed, [VariableScopeNames.Wildcard, left.Name, right.Name])) is
                { Module: null, Call.Arguments: [Expr.Variable] })
            {
                return new(
                    key,
                    selector,
                    fields.ToArray(),
                    call,
                    inner
                );
            }

            return call.Arguments.Count == 2 ? new(
                key,
                selector,
                fields.ToArray(),
                call,
                null
            ) : null;
        }

        private sealed record TupleSelectorTail(
            Atom Key,
            int Selector,
            int[] Fields,
            Expr.Call Tail,
            EntryCall? Inner
        );
    }
}
