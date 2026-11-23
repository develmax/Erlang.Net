using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedTupleConsFrames(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                { Patterns: [Pattern.Tuple { Items.Count: 2 } guarded], Guard: not null, Body: Expr.Tuple } first,
                { Patterns: [Pattern.Tuple { Items: [Pattern.Variable left, Pattern.Variable right] }], Guard: null } second
                ]
                || !DistinctBindings(left, right))
            {
                return false;
            }

            int listField = guarded.Items[0] is AliasPattern ? 0 : 1;
            int companionField = 1 - listField;
            if (guarded.Items[listField] is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var whole, out var shape)
                || shape is not Pattern.List
                {
                    Items: [Pattern.Variable head],
                    Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                }
                || guarded.Items[companionField] is not Pattern.Variable companion
                || !DistinctBindings(whole, companion, head)
                || ReadEntryTypeGuard(first with { Patterns = [head] }, first.Guard) is not { } guard)
            {
                return false;
            }

            // Both successors own the same projections. The body planner has no
            // binding for the guard-only head, so it must prove all call inputs.
            string[] guardedParameters = [whole.Name, companion.Name];
            Pattern.Variable[] fallbackParameters = listField == 0 ? [left, right] : [right, left];
            if (ReadSavedCallSequenceTuple(first.Body, guardedParameters) is not { } hot
                || ReadEntryRegisterCase(second with { Patterns = fallbackParameters }) is not
                {
                    PreserveSelector: true,
                    Case: { Opcode: IsTaggedTuple, Tests: null, Match.DeferredField: not null }
                } cold)
            {
                return false;
            }

            // A tagged test still distinguishes the result's tag and arity.
            int? selectorType = cold.Selector.Call.Module is null
                && ReadResultType(cold.Read, [], selectorContext: true) == TupleTypeBits
                ? TupleTypeBits : null;

            int fallback = Label();
            owner.EmitTupleCheck(0, guarded.Items.Count, functionInfo);
            owner.EmitTuplePart(0, companionField, 1);
            owner.EmitTuplePart(0, listField, 0);
            owner.Op(IsNonemptyList, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetHead, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Op(guard.Opcode, LabelTag, fallback);
            owner.Operand(XRegisterTag, 2);
            EmitSavedCallSequence(hot);
            Mark(fallback);
            EmitEntryRegisterCase(cold, selectorTypeOverride: selectorType);

            return true;
        }
    }
}
