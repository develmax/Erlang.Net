using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAtomOwnedTupleEntries(FunctionDefinition function, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity < 2 || function.Clauses.Count < 2
                || function.Clauses.Any(clause => !clause.Patterns.Any(pattern => pattern is AliasPattern
                    && EntryShape(pattern) is Pattern.Tuple)))
            {
                return false;
            }

            int selected = -1;
            for (int slot = 0; slot < function.Arity; slot++)
            {
                var keys = new HashSet<Atom>();
                if (function.Clauses.All(clause => clause.Patterns[slot] is Pattern.Literal { Value: Atom atom } && keys.Add(atom)))
                {
                    selected = slot;
                    break;
                }
            }

            if (selected < 0)
            {
                return false;
            }

            // Reordering branch stack slots also reorders lifted arguments
            // unless a capture plan is available independently of those slots.
            if (function.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body)).OfType<Expr.Fun>()
                .Any(closure => ClosureCaptures.Names(closure) is not { Length: 0 }))
            {
                return false;
            }

            var context = new SharedNestedEntryContext(
                null,
                null,
                failure,
                CheckedAtomArgument: selected
            );
            if (function.Clauses.Any(clause => !TryEmitPlannedTupleEntry(
                clause,
                failure,
                validateOnly: true,
                sharedEntry: context
            )))
            {
                return false;
            }

            var ordered = function.Clauses
                .Select(clause => (Clause: clause, Key: (Atom)((Pattern.Literal)clause.Patterns[selected]).Value))
                .OrderBy(arm => arm.Key, Comparer<Atom>.Create(CompareAtoms)).ToArray();
            int[] labels = ordered.Select(_ => Label()).Reverse().ToArray();
            owner.EmitMembershipSelect(selected, failure,
                ordered.Select((arm, index) => ((Term)arm.Key, labels[index])));

            int previousFrame = frameSize;
            int previousSlot = nextSlot;
            int previousPeak = peakSlot;
            var previousVariables = variables;
            var plan = new Dictionary<int, int>();
            var failures = new List<EntryEffectFailure>();
            context = context with { Failures = failures };

            for (int index = ordered.Length - 1; index >= 0; index--)
            {
                int armKey = -1 - index;
                Mark(labels[index]);
                frameSize = plannedTupleFrames?.GetValueOrDefault(armKey, previousFrame) ?? previousFrame;
                nextSlot = previousSlot;
                peakSlot = previousSlot;
                variables = new(previousVariables, StringComparer.Ordinal);
                TryEmitPlannedTupleEntry(ordered[index].Clause, failure, sharedEntry: context);
                plan.Add(armKey, peakSlot);
                previousPeak = Math.Max(previousPeak, peakSlot);
            }

            foreach (var cold in failures)
            {
                Mark(cold.Label);
                if (cold.Line is { } line)
                {
                    owner.EmitLine(line);
                }

                owner.Op(CaseEnd, XRegisterTag, cold.Register);
            }

            frameSize = previousFrame;
            nextSlot = previousSlot;
            peakSlot = previousPeak;
            variables = previousVariables;
            TupleFrames = plan;

            return true;
        }

        private void RememberEntryPatternRecords(Pattern pattern, bool includeLists = false)
        {
            if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out _, out var shape))
            {
                RememberCheckedRecord(alias);
                pattern = shape;
            }

            if (pattern is Pattern.Tuple tuple)
            {
                foreach (Pattern child in tuple.Items)
                {
                    RememberEntryPatternRecords(child, includeLists);
                }
            }
            else if (includeLists && pattern is Pattern.List list)
            {
                foreach (Pattern child in list.Items)
                {
                    RememberEntryPatternRecords(child, includeLists);
                }

                if (list.Tail is { } tail)
                {
                    RememberEntryPatternRecords(tail, includeLists);
                }
            }
        }
    }
}
