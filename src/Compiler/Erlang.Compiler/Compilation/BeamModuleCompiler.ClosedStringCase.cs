using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedPairStringCase(
            Expr.Sequence sequence,
            List<(int Label, BeamSourceLocation Location)> failures
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null
                || sequence.Items is not
                [
                    Expr.Match
                {
                    Pattern: AliasPattern
                    {
                        Left: Pattern.Variable carrier,
                        Right: Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable payload] }
                    },
                    Value: Expr.Call { Module: not null, DynamicRemote: false } producer
                } match,
                    Expr.Case { Value: Expr.Variable selected } decision
                ]
                || selected.Name != key.Name || !DistinctBindings(carrier, key, payload)
                || !HasBindingFreeSelector(producer) || ReadClosedStringCase(decision.Clauses) is null
                || new[] { carrier, key, payload }.Any(binding =>
                    variables.ContainsKey(binding.Name) || deferredPairMembers.ContainsKey(binding.Name))
                || ReadUsedEntryParameters(new Clause([key, carrier], null,
                    decision with { Value = new Expr.Literal(Nil.Value) })) is not { } used
                || used.Contains(key.Name) || !used.Contains(carrier.Name)
                || ReadUsedEntryParameters(new Clause([payload], null, decision.Clauses[^1].Body))
                    is not { Count: 0 })
            {
                return false;
            }

            // Only accepted string arms can read the sibling field. The wildcard
            // still sees the saved pair, including when the key is not a cons.
            Expression(producer);
            int pairSlot = Save();
            int payloadSlot = Slot();
            variables.Add(carrier.Name, pairSlot);
            variables.Add(payload.Name, payloadSlot);

            int badMatch = Label();
            owner.EmitTupleCheck(0, 2, badMatch);
            owner.EmitTuplePart(0, 0, 0);
            EmitCase(
                decision,
                null,
                inputPrepared: true,
                reuseBranchSlots: true,
                returningResult: true,
                closedStringPair: (pairSlot, payloadSlot)
            );
            owner.EmitFrameReturn(frameSize);

            BeamSourceLocation failedAt = match.SourceLine == 0
                ? CallLocation(producer, location)
                : new BeamSourceLocation(match.PreprocessorFile, match.SourceLine);
            failures.Add((badMatch, failedAt));

            return true;
        }

        private static (Integer Head, Term Tail, Clause Clause)[]? ReadClosedStringCase(
            IReadOnlyList<Clause> clauses
        )
        {
            if (clauses.Count < 3 || clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                })
            {
                return null;
            }

            var keys = new HashSet<Integer>();
            var arms = new (Integer Head, Term Tail, Clause Clause)[clauses.Count - 1];
            for (int index = 0; index < arms.Length; index++)
            {
                Clause clause = clauses[index];
                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Cons key }] }
                    || ReadStringKey(key) is not { } split
                    || split.Head.Value < 0 || split.Head.Value > int.MaxValue || !keys.Add(split.Head))
                {
                    return null;
                }

                arms[index] = (split.Head, split.Tail, clause);
            }

            return arms.OrderBy(arm => arm.Head.Value).ToArray();
        }

        private void EmitClosedStringCaseSelector(
            (Integer Head, Term Tail, Clause Clause)[] arms,
            int[] entries,
            int fallback,
            int? savedSelector,
            (int Pair, int Payload)? closedStringPair
        )
        {
            if (savedSelector is int saved)
            {
                Load(saved);
            }

            // A saved selector is still available to every arm through its original Y binding.
            // Otherwise keep X0 intact so the default can retain the original selector.
            int tailRegister = savedSelector.HasValue || closedStringPair.HasValue ? 0 : 2;
            owner.EmitNonemptyList(0, fallback);
            if (closedStringPair is { } pair)
            {
                owner.Op(GetTupleElement, YRegisterTag, pair.Pair);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(YRegisterTag, pair.Payload);
            }

            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                tailRegister
            );
            owner.Op(SelectValue, XRegisterTag, 1);
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, arms.Length * 2);
            for (int index = 0; index < arms.Length; index++)
            {
                owner.LiteralOperand(arms[index].Head);
                owner.Operand(LabelTag, entries[index]);
            }
        }
    }
}
