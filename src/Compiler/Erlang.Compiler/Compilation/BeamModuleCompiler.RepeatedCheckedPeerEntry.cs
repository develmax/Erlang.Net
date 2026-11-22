using EntryPattern = Erlang.Pattern;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CheckedPeerTagBucket(
            Atom Tag,
            Clause Last,
            Clause? First = null,
            int SelectorField = 0,
            Term? Key = null,
            bool ReturnsFallback = false
        );

        private bool TryEmitRepeatedCheckedPeerTags(
            IReadOnlyList<Clause> clauses,
            int input,
            int failure,
            Clause? fallback,
            Dictionary<int, int> frames,
            ref int nextFrameKey
        )
        {
            if (input != 0 || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.receiveScratchRegisters is not null || clauses.Count < 3)
            {
                return false;
            }

            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var buckets = new SortedDictionary<Atom, List<Clause>>(order);
            foreach (Clause clause in clauses)
            {
                if (clause is not { Guard: null, Patterns: [EntryPattern.Tuple primary, _] }
                    || !HasTupleTag(primary, out Atom tag))
                {
                    return false;
                }

                string[] bindings = clause.Patterns.SelectMany(Semantics.Variables).ToArray();
                if (bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Length)
                {
                    return false;
                }

                if (!buckets.TryGetValue(tag, out var bucket))
                {
                    bucket = [];
                    buckets.Add(tag, bucket);
                }

                bucket.Add(clause);
            }

            if (buckets.Count < 2 || !buckets.Values.Any(bucket => bucket.Count == 2))
            {
                return false;
            }

            var plans = new List<CheckedPeerTagBucket>();
            var context = new SharedNestedEntryContext(
                null,
                null,
                failure,
                CheckedListPrefixInput: 1
            );
            foreach (var (tag, bucket) in buckets.Reverse())
            {
                Clause last = bucket[^1];
                var primary = (EntryPattern.Tuple)last.Patterns[0];
                if (bucket.Count > 2 || !ReadVariableTupleShape(primary, out _)
                    || ReadPeerShape(last.Patterns[1]) is not { } retry
                    || !HasOwnedPeerBody(last.Body, last.Patterns[1])
                    || !CanEmit(last, []))
                {
                    return false;
                }

                if (bucket.Count == 1)
                {
                    plans.Add(new(tag, last));
                    continue;
                }

                Clause first = bucket[0];
                var firstPattern = (EntryPattern.Tuple)first.Patterns[0];
                if (ReadLiteralColumn(firstPattern) is not { } choice)
                {
                    return false;
                }

                bool returnsFallback = ReturnsOriginalPeer(first) && fallback is not null
                    && ReturnsOriginalPeer(fallback);
                if (!returnsFallback && (ReadPeerShape(first.Patterns[1]) is not { } checkedPeer
                    || !HasTupleTag(checkedPeer, out Atom checkedTag)
                    || !HasTupleTag(retry, out Atom retryTag)
                    || !checkedTag.Equals(retryTag) || checkedPeer.Items.Count != retry.Items.Count
                    || !checkedPeer.Items.Skip(1).All(IsUnconditionalField)
                    || !HasOwnedPeerBody(first.Body, first.Patterns[1])
                    || !CanEmit(first, [choice.Field])))
                {
                    // On a literal hit the first peer check must either be
                    // unconditional or cover every peer that could reach retry.
                    return false;
                }

                plans.Add(new(
                    tag,
                    last,
                    first,
                    choice.Field,
                    choice.Key,
                    returnsFallback
                ));
            }

            const int selector = 2;

            // Tuple arity is already checked. Once every miss returns only
            // the peer, both payload fields can replace the original input.
            IReadOnlyDictionary<int, int>? sharedFields = null;
            if (fallback is not null && ReturnsOriginalPeer(fallback)
                && HasLivePayloadFields(clauses))
            {
                var fields = new Dictionary<int, int> { [1] = selector + 1, [2] = input };
                var carried = context with { ProjectedRootFields = fields };
                if (plans.All(plan => CanEmit(plan.Last, [], carried)
                    && (plan.First is not { } earlier || plan.ReturnsFallback
                        || CanEmit(earlier, [plan.SelectorField], carried))))
                {
                    context = carried;
                    sharedFields = fields;
                }
            }

            var labels = plans.Select(_ => Label()).ToArray();
            owner.EmitTuplePart(input, 0, selector);
            if (sharedFields is not null)
            {
                owner.EmitTuplePart(input, 1, sharedFields[1]);
                owner.EmitTuplePart(input, 2, sharedFields[2]);
            }
            EmitAritySelect(selector, plans.Count, failure);
            for (int index = plans.Count - 1; index >= 0; index--)
            {
                owner.Operand(AtomTag, owner.Intern(plans[index].Tag.Name));
                owner.Operand(LabelTag, labels[index]);
            }

            int savedFrame = frameSize;
            int savedSlot = nextSlot;
            int maxSlots = peakSlot;
            int nextKey = nextFrameKey;
            var savedBindings = variables;
            for (int index = 0; index < plans.Count; index++)
            {
                CheckedPeerTagBucket plan = plans[index];
                Mark(labels[index]);
                if (plan.First is { } first && plan.Key is { } key)
                {
                    int compared = selector;
                    if (sharedFields?.TryGetValue(plan.SelectorField, out int field) == true)
                    {
                        compared = field;
                    }
                    else
                    {
                        owner.EmitTuplePart(input, plan.SelectorField, compared);
                    }
                    int retry = plan.ReturnsFallback ? failure : Label();
                    owner.Op(plan.ReturnsFallback ? ExactNotEqualTest : ExactEqualTest, LabelTag, retry);
                    owner.Operand(XRegisterTag, compared);
                    owner.LiteralOperand(key);
                    if (!plan.ReturnsFallback)
                    {
                        EmitLeaf(first, [plan.SelectorField]);
                        Mark(retry);
                    }
                }

                EmitLeaf(plan.Last, []);
            }

            frameSize = savedFrame;
            nextSlot = savedSlot;
            peakSlot = maxSlots;
            variables = savedBindings;
            nextFrameKey = nextKey;

            return true;

            bool CanEmit(
                Clause clause,
                HashSet<int> fields,
                SharedNestedEntryContext? projected = null
            ) => TryEmitPlannedTupleEntry(
                clause,
                failure,
                checkedRootFields: fields,
                validateOnly: true,
                sharedEntry: projected ?? context
            );

            void EmitLeaf(Clause clause, HashSet<int> fields)
            {
                int frameKey = nextKey--;
                frameSize = plannedTupleFrames?.GetValueOrDefault(frameKey, savedFrame) ?? savedFrame;
                nextSlot = 0;
                peakSlot = 0;
                variables = new(StringComparer.Ordinal);
                TryEmitPlannedTupleEntry(
                    clause,
                    failure,
                    checkedRootFields: fields,
                    sharedEntry: context
                );
                frames.Add(frameKey, peakSlot);
                maxSlots = Math.Max(maxSlots, peakSlot);
            }
        }

        private static bool HasLivePayloadFields(IReadOnlyList<Clause> clauses)
        {
            int live = 0;
            foreach (Clause clause in clauses)
            {
                if (clause.Patterns[0] is not EntryPattern.Tuple { Items.Count: 3 } primary)
                {
                    return false;
                }

                for (int field = 1; field < primary.Items.Count; field++)
                {
                    if (!IsUnconditionalField(primary.Items[field])
                        || primary.Items[field] is EntryPattern.Variable binding
                            && ReadUsedEntryParameters(new Clause([binding], null, clause.Body))?.Contains(binding.Name) == true)
                    {
                        live |= 1 << field;
                    }
                }
            }

            return live == ((1 << 1) | (1 << 2));
        }

        private static EntryPattern.Tuple? ReadPeerShape(EntryPattern pattern)
        {
            if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out _, out var shape)
                && shape is EntryPattern.Tuple tuple && HasTupleTag(tuple, out _)
                && tuple.Items.Skip(1).All(field => IsUnconditionalField(field) || field is
                    EntryPattern.List { Items.Count: > 0, Tail: EntryPattern.Variable }))
            {
                return tuple;
            }

            return null;
        }

        private static (int Field, Term Key)? ReadLiteralColumn(EntryPattern.Tuple primary)
        {
            (int Field, Term Key)? choice = null;
            for (int index = 1; index < primary.Items.Count; index++)
            {
                EntryPattern item = primary.Items[index];
                if (IsUnconditionalField(item))
                {
                    continue;
                }

                Term? key = item switch
                {
                    EntryPattern.Literal literal => literal.Value,
                    EntryPattern.List { Items.Count: 0, Tail: null } => Nil.Value,
                    _ => null
                };
                if (key is null || choice.HasValue)
                {
                    return null;
                }

                choice = (index, key);
            }

            return choice;
        }

        private static bool IsUnconditionalField(EntryPattern pattern) =>
            pattern is EntryPattern.Variable or EntryPattern.Any;

        private static bool ReturnsOriginalPeer(Clause clause) =>
            clause is { Guard: null, Patterns: [_, EntryPattern.Variable peer], Body: Expr.Variable result }
            && result.Name == peer.Name && peer.Name != VariableScopeNames.Wildcard;
    }
}
