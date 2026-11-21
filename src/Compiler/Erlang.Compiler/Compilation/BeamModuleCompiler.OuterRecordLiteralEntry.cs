using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOuterRecordLiteralEntry(FunctionDefinition function, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity == 0 || function.Clauses.Count < 3
                || EntryShape(function.Clauses[0].Patterns[0]) is not Pattern.Tuple first
                || !HasTupleTag(first, out Atom tag)
                || function.Clauses[^1] is not { Guard: null } fallback)
            {
                return false;
            }

            var shapes = new List<Pattern.Tuple>();
            foreach (Clause clause in function.Clauses)
            {
                if (EntryShape(clause.Patterns[0]) is not Pattern.Tuple shape || shape.Items.Count != first.Items.Count
                    || !HasTupleTag(shape, out Atom other) || !tag.Equals(other))
                {
                    return false;
                }

                shapes.Add(shape);
            }

            if (!shapes[^1].Items.Skip(1).All(IsSharedEntryWildcard))
            {
                return false;
            }

            var branches = shapes.SkipLast(1).ToArray();
            int selectorField = -1;
            for (int column = 1; column < first.Items.Count; column++)
            {
                var keys = new HashSet<Integer>();
                if (branches.All(branch => branch.Items[column] is Pattern.Literal { Value: Integer key } && keys.Add(key)))
                {
                    selectorField = column;
                    break;
                }
            }

            int selector = function.Arity;
            var projections = Enumerable.Range(1, first.Items.Count - 1)
                .Where(field => branches.All(branch => branch.Items[field] is Pattern.Tuple))
                .Select((field, index) => (field, register: selector + index + 1))
                .ToDictionary(pair => pair.field, pair => pair.register);
            if (selectorField < 0 || projections.Count == 0 || selector + projections.Count >= FirstReservedXRegister)
            {
                return false;
            }

            var checkedColumns = new HashSet<int> { selectorField };
            var branchContext = new SharedNestedEntryContext(
                null,
                fallback,
                failure,
                ProjectedRootFields: projections,
                ConsumedSelectorRegister: selector
            );
            var fallbackContext = new SharedNestedEntryContext(null, null, failure);
            if (function.Clauses.SkipLast(1).Any(clause => !TryEmitPlannedTupleEntry(
                clause,
                failure,
                checkedRootFields: checkedColumns,
                validateOnly: true,
                sharedEntry: branchContext
            )) || !TryEmitPlannedTupleEntry(
                fallback,
                failure,
                checkedRootFields: [],
                validateOnly: true,
                sharedEntry: fallbackContext
            ))
            {
                return false;
            }

            int reject = Label();
            var arms = function.Clauses.SkipLast(1).Zip(branches).Select(pair =>
                (Clause: pair.First, Key: (Integer)((Pattern.Literal)pair.Second.Items[selectorField]).Value))
                .OrderBy(arm => arm.Key.Value).ToArray();
            int[] labels = arms.Select(_ => Label()).Reverse().ToArray();
            EmitDetailRecordTest(
                0,
                first.Items.Count,
                tag,
                failure
            );
            owner.EmitTuplePart(0, selectorField, selector);
            foreach (var (field, register) in projections)
            {
                owner.EmitTuplePart(0, field, register);
            }

            owner.EmitMembershipSelect(selector, reject, arms.Select((arm, index) => ((Term)arm.Key, labels[index])));
            int originalFrame = frameSize;
            int originalSlot = nextSlot;
            int maximumSlots = peakSlot;
            var originalVariables = variables;
            var frames = new Dictionary<int, int>();
            var failures = new List<EntryEffectFailure>();
            for (int index = arms.Length - 1; index >= 0; index--)
            {
                Mark(labels[index]);
                EmitArm(
                    arms[index].Clause,
                    index,
                    reject,
                    checkedColumns,
                    branchContext
                );
            }

            Mark(reject);
            EmitArm(
                fallback,
                arms.Length,
                failure,
                [],
                fallbackContext
            );
            foreach (var cold in failures)
            {
                Mark(cold.Label);
                if (cold.Line is { } line)
                {
                    owner.EmitLine(line);
                }

                owner.Op(CaseEnd, XRegisterTag, cold.Register);
            }

            frameSize = originalFrame;
            nextSlot = originalSlot;
            peakSlot = maximumSlots;
            variables = originalVariables;
            TupleFrames = frames;

            return true;

            void EmitArm(
                Clause clause,
                int ordinal,
                int noMatch,
                HashSet<int> columns,
                SharedNestedEntryContext context
            )
            {
                int key = TupleFallbackFrame - ordinal;
                frameSize = plannedTupleFrames?.GetValueOrDefault(key, originalFrame) ?? originalFrame;
                nextSlot = originalSlot;
                peakSlot = originalSlot;
                variables = new(originalVariables, StringComparer.Ordinal);
                TryEmitPlannedTupleEntry(
                    clause,
                    noMatch,
                    checkedRootFields: columns,
                    sharedEntry: context with { Failures = failures }
                );
                frames.Add(key, peakSlot);
                maximumSlots = Math.Max(maximumSlots, peakSlot);
            }
        }
    }
}
