using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CheckedNestedEntry(
            int Field,
            int Register,
            int Selector,
            IReadOnlyDictionary<int, int> Projections
        );
        private sealed record SharedNestedEntryContext(
            CheckedNestedEntry? Nested,
            Clause? Fallback,
            int EntryFailure,
            List<EntryEffectFailure>? Failures = null,
            int? CheckedAtomArgument = null,
            IReadOnlyDictionary<int, int>? ProjectedRootFields = null,
            int? ConsumedSelectorRegister = null,
            bool AllowConsTupleField = false
        );
        private sealed record SharedNestedRecordArm(Clause Source, Pattern.Tuple Shape, Atom Tag);
        private sealed record SharedNestedRecordGroup(
            int Arity,
            Atom Tag,
            int Selector,
            (Clause Source, Atom Key)[] Arms,
            IReadOnlyDictionary<int, int> Projections
        );

        private bool TryEmitSharedNestedRecordEntry(FunctionDefinition function, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity == 0 || function.Arity + 2 >= FirstReservedXRegister
                || function.Clauses.Count < 2 || function.Clauses[0].Patterns[0] is not Pattern.Tuple first
                || !HasTupleTag(first, out Atom outerTag))
            {
                return false;
            }

            int field = Array.FindIndex(first.Items.ToArray(), item => item is Pattern.Tuple);
            if (field <= 0)
            {
                return false;
            }

            int arity = first.Items.Count;
            int tuple = function.Arity;
            int selector = tuple + 1;
            var arms = new List<SharedNestedRecordArm>();
            Clause? fallback = null;
            for (int index = 0; index < function.Clauses.Count; index++)
            {
                Clause clause = function.Clauses[index];
                if (EntryShape(clause.Patterns[0]) is not Pattern.Tuple shape || shape.Items.Count != arity
                    || !HasTupleTag(shape, out Atom tag) || !tag.Equals(outerTag))
                {
                    return false;
                }

                if (shape.Items[field] is Pattern.Tuple nested && HasTupleTag(nested, out Atom nestedTag))
                {
                    arms.Add(new SharedNestedRecordArm(clause, nested, nestedTag));
                }
                else if (clause.Guard is null && index == function.Clauses.Count - 1
                    && shape.Items.Skip(1).All(IsSharedEntryWildcard))
                {
                    fallback = clause;
                }
                else
                {
                    return false;
                }
            }

            var groups = new List<SharedNestedRecordGroup>();
            var rootFields = new HashSet<int>();
            foreach (var branch in arms.GroupBy(arm => (Arity: arm.Shape.Items.Count, arm.Tag)))
            {
                int column = FindAtomSelectorColumn(branch.Select(arm => arm.Shape), branch.Key.Arity);
                if (column < 0)
                {
                    return false;
                }

                var projections = branch.SelectMany(arm => ReadDelayedNestedFields(arm.Source, arm.Shape, column))
                    .Distinct().Order().Select((position, index) => (position, register: selector + index + 1))
                    .ToDictionary(projection => projection.position, projection => projection.register);
                if (projections.Values.DefaultIfEmpty(selector).Max() >= FirstReservedXRegister)
                {
                    return false;
                }

                var dispatch = new CheckedNestedEntry(
                    field,
                    tuple,
                    column,
                    projections
                );
                var choices = new List<(Clause Source, Atom Key)>();
                foreach (var arm in branch)
                {
                    var key = (Atom)((Pattern.Literal)arm.Shape.Items[column]).Value;
                    if (!TryEmitPlannedTupleEntry(
                        arm.Source,
                        failure,
                        checkedRootFields: rootFields,
                        validateOnly: true,
                        sharedEntry: new SharedNestedEntryContext(dispatch, null, failure)
                    ))
                    {
                        return false;
                    }

                    choices.Add((arm.Source, key));
                }

                groups.Add(new SharedNestedRecordGroup(
                    branch.Key.Arity,
                    branch.Key.Tag,
                    column,
                    choices.OrderBy(choice => choice.Key, Comparer<Atom>.Create(CompareAtoms)).ToArray(),
                    projections
                ));
            }

            if (!groups.Any(group => group.Arms.Length >= 2)
                || fallback is not null && !TryEmitPlannedTupleEntry(
                    fallback,
                    failure,
                    checkedRootFields: rootFields,
                    validateOnly: true,
                    sharedEntry: new SharedNestedEntryContext(null, null, failure)
                ))
            {
                return false;
            }

            int priorFrame = frameSize;
            int initialSlot = nextSlot;
            int maximumSlots = peakSlot;
            var originalVariables = variables;
            var frames = new Dictionary<int, int>();
            int frameId = TupleFallbackFrame;
            var effectFailures = new List<EntryEffectFailure>();
            int reject = fallback is null ? failure : Label();

            EmitDetailRecordTest(
                0,
                arity,
                outerTag,
                failure
            );
            owner.EmitTuplePart(0, field, tuple);
            var widths = groups.GroupBy(group => group.Arity).OrderBy(group => group.Key).ToArray();
            int[] widthLabels = widths.Length == 1 ? [] : widths.Select(_ => Label()).Reverse().ToArray();
            if (widthLabels.Length != 0)
            {
                owner.EmitTupleArityDispatch(reject, widths.Select((width, index) => (width.Key, widthLabels[index])).ToArray(), tuple);
            }
            else if (groups.Count == 1)
            {
                EmitDetailRecordTest(
                    tuple,
                    groups[0].Arity,
                    groups[0].Tag,
                    reject
                );
            }
            else
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    tuple,
                    widths[0].Key,
                    reject
                );
            }

            for (int index = widths.Length - 1; index >= 0; index--)
            {
                if (widthLabels.Length != 0)
                {
                    Mark(widthLabels[index]);
                }

                var tags = widths[index].OrderBy(group => group.Tag, Comparer<Atom>.Create(CompareAtoms)).ToArray();
                if (tags is [var single])
                {
                    if (widthLabels.Length != 0)
                    {
                        owner.EmitTuplePart(tuple, 0, selector);
                        owner.EmitAtomTest(selector, single.Tag.Name, reject);
                    }

                    EmitSelector(single);
                    continue;
                }

                owner.EmitTuplePart(tuple, 0, selector);
                int[] targets = tags.Select(_ => Label()).Reverse().ToArray();
                owner.EmitMembershipSelect(selector, reject, tags.Select((group, ordinal) => ((Term)group.Tag, targets[ordinal])));
                for (int ordinal = tags.Length - 1; ordinal >= 0; ordinal--)
                {
                    Mark(targets[ordinal]);
                    EmitSelector(tags[ordinal]);
                }
            }

            if (fallback is not null)
            {
                Mark(reject);
                EmitArm(fallback, null, failure);
            }

            foreach (var cold in effectFailures)
            {
                Mark(cold.Label);
                if (cold.Line is { } line)
                {
                    owner.EmitLine(line);
                }

                owner.Op(CaseEnd, XRegisterTag, cold.Register);
            }

            frameSize = priorFrame;
            nextSlot = initialSlot;
            peakSlot = maximumSlots;
            variables = originalVariables;
            TupleFrames = frames;

            return true;

            void EmitSelector(SharedNestedRecordGroup group)
            {
                var shared = new CheckedNestedEntry(
                    field,
                    tuple,
                    group.Selector,
                    group.Projections
                );
                owner.EmitTuplePart(tuple, group.Selector, selector);
                foreach (var (index, register) in group.Projections)
                {
                    owner.EmitTuplePart(tuple, index, register);
                }
                if (group.Arms is [var only])
                {
                    owner.EmitAtomTest(selector, only.Key.Name, reject);
                    EmitArm(only.Source, shared, reject);

                    return;
                }

                int[] targets = group.Arms.Select(_ => Label()).Reverse().ToArray();
                owner.EmitMembershipSelect(selector, reject,
                    group.Arms.Select((arm, ordinal) => ((Term)arm.Key, targets[ordinal])));
                for (int ordinal = group.Arms.Length - 1; ordinal >= 0; ordinal--)
                {
                    Mark(targets[ordinal]);
                    EmitArm(group.Arms[ordinal].Source, shared, reject);
                }
            }

            void EmitArm(Clause arm, CheckedNestedEntry? shared, int noMatch)
            {
                nextSlot = initialSlot;
                peakSlot = initialSlot;
                variables = new(originalVariables, StringComparer.Ordinal);
                frameSize = plannedTupleFrames?.GetValueOrDefault(frameId, priorFrame) ?? priorFrame;
                TryEmitPlannedTupleEntry(
                    arm,
                    noMatch,
                    checkedRootFields: rootFields,
                    sharedEntry: new SharedNestedEntryContext(
                        shared,
                        ReferenceEquals(arm, fallback) ? null : fallback,
                        failure,
                        effectFailures
                    )
                );
                frames.Add(frameId--, peakSlot);
                maximumSlots = Math.Max(maximumSlots, peakSlot);
            }
        }

        private static int FindAtomSelectorColumn(IEnumerable<Pattern.Tuple> source, int width)
        {
            var shapes = source.ToArray();
            for (int field = 1; field < width; field++)
            {
                var keys = new HashSet<Atom>();
                if (shapes.All(pattern => pattern.Items[field] is Pattern.Literal { Value: Atom key } && keys.Add(key)))
                {
                    return field;
                }
            }

            return -1;
        }

        private static int CompareAtoms(Atom left, Atom right) => TermOrder.Compare(left, right, exact: true);

        private static bool IsSharedEntryWildcard(Pattern pattern) =>
            pattern is Erlang.Pattern.Any or Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard };
    }
}
