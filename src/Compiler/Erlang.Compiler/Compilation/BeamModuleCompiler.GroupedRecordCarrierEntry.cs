using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGroupedRecordCarrierEntry(
            Clause clause,
            int input,
            int failure,
            int baseSlot
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null || input != 0 || clause.Guard is not null
                || clause.Patterns is not [Pattern.Tuple eventShape, var carrier]
                || !ReadVariableTupleShape(eventShape, out Atom eventTag)
                || EntryShape(carrier) is not Pattern.Tuple carrierShape || !HasTupleTag(carrierShape, out _)
                || carrierShape.Items.Skip(1).Any(field => field is not
                    (Erlang.Pattern.Variable or Erlang.Pattern.Any
                    or Pattern.List { Items.Count: > 0, Tail: Erlang.Pattern.Variable }))
                || LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                    expression is Expr.Try or Expr.Receive or Expr.Catch or Expr.Maybe or Expr.Fun))
            {
                return false;
            }

            // The surrounding group owns the event arity. Check its remaining
            // tag in X before the ordinary entry planner checks the state shape.
            var context = new SharedNestedEntryContext(
                null,
                null,
                failure,
                CheckedListPrefixInput: 1
            );
            HashSet<int> checkedEventArity = [];
            if (!TryEmitPlannedTupleEntry(
                clause,
                failure,
                checkedRootFields: checkedEventArity,
                validateOnly: true,
                sharedEntry: context
            ))
            {
                return false;
            }

            owner.EmitTuplePart(0, 0, clause.Patterns.Count);
            owner.EmitAtomTest(clause.Patterns.Count, eventTag.Name, failure);

            int savedSlot = nextSlot;
            var incoming = variables;
            nextSlot = baseSlot;
            bool emitted = TryEmitPlannedTupleEntry(
                clause,
                failure,
                checkedRootFields: checkedEventArity,
                sharedEntry: context
            );
            nextSlot = savedSlot;
            variables = incoming;

            return emitted;
        }

        private bool TryEmitCheckedPeerTagGroup(
            IReadOnlyList<Clause> clauses,
            int input,
            int failure,
            Clause? fallback,
            Dictionary<int, int> frames,
            ref int nextFrameKey
        )
        {
            if (input != 0 || clauses.Count < 2 || !owner.SsaTypeInferenceEnabled
                || owner.ModuleOptimizationDisabled || owner.receiveScratchRegisters is not null)
            {
                return false;
            }

            const int payloadField = 1;
            bool carryPayload = CanCarryCheckedPeerPayload(clauses, fallback);
            var context = new SharedNestedEntryContext(
                null,
                null,
                failure,
                CheckedListPrefixInput: 1,
                MatchRepeatedRootFields: true,
                CarriedRootField: carryPayload ? (payloadField, input) : null
            );
            HashSet<int> checkedEventArity = [];
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var members = new SortedDictionary<Atom, Clause>(order);
            bool hasListPrefix = false;
            foreach (Clause clause in clauses)
            {
                if (clause.Guard is not null || clause.Patterns is not [Pattern.Tuple eventShape, var carrier]
                    || !ReadVariableTupleShape(eventShape, out Atom tag)
                    || carrier is not AliasPattern alias || !TryReadPatternAlias(alias, out _, out var carrierShape)
                    || carrierShape is not Pattern.Tuple tuple || !HasTupleTag(tuple, out _)
                    || tuple.Items.Skip(1).Any(field => field is not
                        (Erlang.Pattern.Variable or Erlang.Pattern.Any
                        or Pattern.List { Items.Count: > 0, Tail: Erlang.Pattern.Variable }))
                    || !HasOwnedPeerBody(clause.Body, carrier) || !members.TryAdd(tag, clause)
                    || !TryEmitPlannedTupleEntry(
                        clause,
                        failure,
                        checkedRootFields: checkedEventArity,
                        validateOnly: true,
                        sharedEntry: context
                    ))
                {
                    return false;
                }

                hasListPrefix |= tuple.Items.Any(field => field is Erlang.Pattern.List);
            }

            if (!hasListPrefix)
            {
                return false;
            }

            // The arity edge owns the primary tuple. Once the tag is
            // available, the payload may replace the unused fallback input.
            var branches = members.Reverse().Select(member => (member.Key, member.Value, Label: Label())).ToArray();
            int selector = clauses[0].Patterns.Count;
            owner.EmitTuplePart(input, 0, selector);
            if (carryPayload)
            {
                owner.EmitTuplePart(input, payloadField, input);
            }

            EmitAritySelect(selector, branches.Length, failure);
            foreach (var (tag, _, label) in branches.Reverse())
            {
                owner.Operand(AtomTag, owner.Intern(tag.Name));
                owner.Operand(LabelTag, label);
            }

            int priorFrame = frameSize;
            int priorSlot = nextSlot;
            int functionPeak = peakSlot;
            var priorBindings = variables;
            foreach (var (_, clause, label) in branches)
            {
                int key = nextFrameKey--;
                frameSize = plannedTupleFrames?.GetValueOrDefault(key, priorFrame) ?? priorFrame;
                nextSlot = 0;
                peakSlot = 0;
                Mark(label);
                TryEmitPlannedTupleEntry(
                    clause,
                    failure,
                    checkedRootFields: checkedEventArity,
                    sharedEntry: context
                );
                frames.Add(key, peakSlot);
                functionPeak = Math.Max(functionPeak, peakSlot);
            }

            variables = priorBindings;
            frameSize = priorFrame;
            nextSlot = priorSlot;
            peakSlot = functionPeak;

            return true;
        }

        private static bool CanCarryCheckedPeerPayload(IReadOnlyList<Clause> clauses, Clause? fallback)
        {
            if (fallback is not { Guard: null, Patterns: [Erlang.Pattern.Variable discarded, Erlang.Pattern.Variable] }
                || discarded.Name != VariableScopeNames.Wildcard
                    && ReadUsedEntryParameters(fallback)?.Contains(discarded.Name) != false)
            {
                return false;
            }

            return clauses.All(clause => clause.Patterns is
                [Erlang.Pattern.Tuple
                {
                    Items: [_, Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard } payload]
                }, var peer]
                && (Semantics.Variables(peer).Contains(payload.Name)
                    || ReadUsedEntryParameters(new Clause([payload], null, clause.Body))?.Contains(payload.Name) == true));
        }

        private static bool HasOwnedPeerBody(Expr body, Pattern peer)
        {
            var aliases = new HashSet<(string Name, string Tag, int Arity)>();
            ReadAliases(peer);

            return LocalFunctionEntries.Walk(body).All(expression => expression switch
            {
                Expr.Fun or Expr.FunctionReference or Expr.Try or Expr.Receive or Expr.Catch or Expr.Maybe
                    or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension => false,
                Expr.RecordUpdate { Value: Expr.Variable source } update =>
                    aliases.Contains((source.Name, update.Name, update.Arity)),
                Expr.RecordUpdate => false,
                _ => true
            });

            void ReadAliases(Pattern pattern)
            {
                if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var source, out var shape))
                {
                    if (shape is Pattern.Tuple tuple && HasTupleTag(tuple, out Atom tag))
                    {
                        aliases.Add((source.Name, tag.Name, tuple.Items.Count));
                    }

                    ReadAliases(shape);

                    return;
                }

                if (pattern is Erlang.Pattern.Tuple carrier)
                {
                    foreach (Pattern field in carrier.Items)
                    {
                        ReadAliases(field);
                    }
                }
                else if (pattern is Erlang.Pattern.List list)
                {
                    foreach (Pattern head in list.Items)
                    {
                        ReadAliases(head);
                    }
                }
            }
        }

        private static bool ReadVariableTupleShape(Pattern pattern, out Atom tag)
        {
            if (pattern is Pattern.Tuple tuple && HasTupleTag(tuple, out tag)
                && tuple.Items.Skip(1).All(field => field is Erlang.Pattern.Variable or Erlang.Pattern.Any))
            {
                return true;
            }

            tag = null!;

            return false;
        }
    }
}
