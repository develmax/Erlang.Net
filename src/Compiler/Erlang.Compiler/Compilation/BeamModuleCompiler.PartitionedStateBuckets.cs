using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Clause[]? TryPartitionTaggedStateBuckets(
            IReadOnlyList<Clause> clauses,
            int input,
            int failure,
            Dictionary<int, int> frames,
            List<(int Label, BeamSourceLocation Location)> recordFailures,
            List<CheckedRecordEffectExits> checkedEffectExits,
            List<(int Label, BeamSourceLocation Location)> tupleMatchFailures,
            ref int nextFrameKey
        )
        {
            int selector = clauses[0].Patterns.Count;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || selector >= FirstReservedXRegister
                || owner.receiveScratchRegisters is not null || owner.OnLoadFunction is not null)
            {
                return null;
            }

            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var buckets = new SortedDictionary<Atom, List<Clause>>(order);
            foreach (Clause clause in clauses)
            {
                if (EntryShape(clause.Patterns[input]) is not Erlang.Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag))
                {
                    return null;
                }

                if (!buckets.TryGetValue(tag, out var bucket))
                {
                    bucket = [];
                    buckets.Add(tag, bucket);
                }

                bucket.Add(clause);
            }

            if (buckets.Count < 2)
            {
                return null;
            }

            var eligible = new HashSet<Atom>();
            var guarded = new List<(Atom Tag, GuardedTaggedStateEntry Plan)>();
            var splits = new Dictionary<Atom, LiteralTaggedStateSplit>();
            var checkedRetries = new Dictionary<Atom, CheckedNilStateRetry>();
            foreach (var (tag, bucket) in buckets)
            {
                if (TryEmitTaggedStateTupleGroup(
                    bucket,
                    input,
                    failure,
                    frames,
                    recordFailures,
                    ref nextFrameKey,
                    tagAlreadyChecked: true,
                    validateOnly: true
                ))
                {
                    eligible.Add(tag);
                }
                else if (selector + 2 < FirstReservedXRegister
                    && ReadLiteralTaggedStateSplit(bucket, input) is { } split
                    && TryEmitTaggedStateTupleGroup(
                        split.Owned,
                        input,
                        failure,
                        frames,
                        recordFailures,
                        ref nextFrameKey,
                        tagAlreadyChecked: true,
                        validateOnly: true
                    ))
                {
                    eligible.Add(tag);
                    splits.Add(tag, split);
                    if (ReadCheckedNilStateRetry(split.Retried, input) is { } checkedRetry)
                    {
                        checkedRetries.Add(tag, checkedRetry);
                    }
                }
                else if (ReadGuardedTaggedStateEntry(bucket, input) is { } plan)
                {
                    guarded.Add((tag, plan));
                }
            }

            Atom? guardedTag = null;
            GuardedTaggedStateEntry? guardedPlan = null;
            if (guarded is [var single])
            {
                (guardedTag, guardedPlan) = single;
                eligible.Add(single.Tag);
            }

            if (eligible.Count == 0)
            {
                return null;
            }

            int? generic = eligible.Count != buckets.Count || splits.Count != checkedRetries.Count ? Label() : null;
            var labels = buckets.Reverse().ToDictionary(
                bucket => bucket.Key,
                bucket => eligible.Contains(bucket.Key) ? Label() : generic!.Value
            );

            // The enclosing arity branch already checked the carrier. A tag
            // selected here cannot match a row in any other bucket.
            owner.EmitTuplePart(input, 0, selector);
            if (guardedPlan is { } projected)
            {
                // This bucket is inside the already checked message arity.
                // Its payload uses the first scratch register beyond the tag.
                owner.EmitTuplePart(input, projected.PayloadField, selector + 1);
            }


            owner.EmitMembershipSelect(
                selector,
                failure,
                buckets.Keys.Select(tag => ((Term)tag, labels[tag]))
            );
            foreach (var (tag, bucket) in buckets.Reverse())
            {
                if (!eligible.Contains(tag))
                {
                    continue;
                }

                Mark(labels[tag]);
                if (guardedPlan is { } plan && tag.Equals(guardedTag))
                {
                    EmitGuardedTaggedStateEntry(
                        plan,
                        input + 1,
                        failure,
                        frames,
                        recordFailures,
                        checkedEffectExits,
                        ref nextFrameKey
                    );
                    continue;
                }

                if (splits.TryGetValue(tag, out var split))
                {
                    int roleRegister = selector + (guardedPlan is null ? 1 : 2);
                    var checkedRetry = checkedRetries.GetValueOrDefault(tag);
                    int retryTarget = checkedRetry is null ? generic!.Value : Label();
                    TryEmitTaggedStateTupleGroup(
                        split.Owned,
                        input,
                        failure,
                        frames,
                        recordFailures,
                        ref nextFrameKey,
                        tagAlreadyChecked: true,
                        retry: new(split.State, retryTarget, roleRegister)
                    );
                    if (checkedRetry is not null)
                    {
                        Mark(retryTarget);
                        EmitCheckedNilStateRetry(
                            checkedRetry,
                            roleRegister,
                            guardedPlan?.PayloadField,
                            failure,
                            frames,
                            recordFailures,
                            tupleMatchFailures,
                            ref nextFrameKey
                        );
                    }

                    continue;
                }

                TryEmitTaggedStateTupleGroup(
                    bucket,
                    input,
                    failure,
                    frames,
                    recordFailures,
                    ref nextFrameKey,
                    tagAlreadyChecked: true
                );
            }

            if (generic is int label)
            {
                Mark(label);
            }

            return clauses.Where(clause =>
                EntryShape(clause.Patterns[input]) is Erlang.Pattern.Tuple tuple
                && HasTupleTag(tuple, out Atom tag)
                && (!eligible.Contains(tag)
                    || !checkedRetries.ContainsKey(tag) && splits.TryGetValue(tag, out var split) && split.Retried.Contains(clause)))
                .ToArray();
        }

        private sealed record LiteralTaggedStateSplit(Clause[] Owned, Clause[] Retried, Atom State);

        private static LiteralTaggedStateSplit? ReadLiteralTaggedStateSplit(IReadOnlyList<Clause> bucket, int input)
        {
            int state = input + 1;
            if (bucket.Count < 4 || state >= bucket[0].Patterns.Count)
            {
                return null;
            }

            var groups = new Dictionary<Atom, List<Clause>>();
            Atom? role = null;
            foreach (Clause clause in bucket)
            {
                var shape = clause.Patterns[state];
                if (shape is AliasPattern alias && TryReadPatternAlias(alias, out _, out var inner))
                {
                    shape = inner;
                }

                if (clause.Guard is not null || shape is not global::Erlang.Pattern.Tuple
                    {
                        Items:
                        [
                            global::Erlang.Pattern.Literal { Value: Atom key },
                            global::Erlang.Pattern.Literal { Value: Atom side }
                        ]
                    } || role is not null && !role.Equals(side))
                {
                    return null;
                }

                role ??= side;
                if (!groups.TryGetValue(key, out var same))
                {
                    same = [];
                    groups.Add(key, same);
                }

                same.Add(clause);
            }

            if (groups.Count != 3 || groups.Where(group => group.Value.Count > 1).ToArray() is not [var retry])
            {
                return null;
            }

            Clause[] owned = groups.Values.Where(group => group.Count == 1).Select(group => group[0]).ToArray();

            return new(owned, retry.Value.ToArray(), retry.Key);
        }
    }
}
