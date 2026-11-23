namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryBindConstructedBlockTuple(Expr.Match match)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || match.Pattern is not Pattern.Tuple pattern
                || (match.Value is Expr.Block block ? block.Body : match.Value) is not Expr.Sequence sequence
                || sequence.Items.Count < 2 || sequence.Items[^1] is not Expr.Tuple tuple
                || tuple.Items.Count != pattern.Items.Count || tuple.Items.Count < 2
                || tuple.Items.Any(item => item is not Expr.Variable)
                || pattern.Items.Any(item => item is not (Erlang.Pattern.Any or Erlang.Pattern.Variable)))
            {
                return false;
            }

            // The existing frame allocator reuses the boxed group of computed
            // arithmetic results. Keep that allocation until component lifetimes
            // can be preserved when the intermediate tuple is removed.
            if (sequence.Items.Take(sequence.Items.Count - 1).All(item => item is Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable,
                    Value: Expr.Binary
                    {
                        Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply
                            or ErlangOperators.IntegerDivide or ErlangOperators.Remainder
                    }
                }))
            {
                return false;
            }

            Pattern.Variable[] named = pattern.Items.OfType<Pattern.Variable>()
                .Where(field => field.Name != VariableScopeNames.Wildcard)
                .ToArray();
            if (!DistinctBindings(named) || named.Any(IsBound))
            {
                return false;
            }

            // Match the normal sequence emission, including its effect order and
            // proof scope. Only the final tuple value may be removed.
            var sequenceRecords = checkedRecordBindings;
            var sequenceCopyHints = freshRecordUpdateOperands;
            DiscardSequencePrefix(
                sequence.Items,
                guardFailure: null,
                rememberRecords: true,
                rememberFreshRecordOperands: true
            );
            freshRecordUpdateOperands = sequenceCopyHints;
            checkedRecordBindings = sequenceRecords;

            // The block may itself have bound a destination. That destination
            // needs the original equality test and its badmatch value.
            if (named.Any(IsBound) || tuple.Items.Cast<Expr.Variable>().Any(value => !variables.ContainsKey(value.Name)))
            {
                MatchValue(match, ValueSlot(tuple, null), returnValue: false);

                return true;
            }

            for (int index = 0; index < pattern.Items.Count; index++)
            {
                if (pattern.Items[index] is Pattern.Variable { Name: not VariableScopeNames.Wildcard } output)
                {
                    var value = (Expr.Variable)tuple.Items[index];
                    variables.Add(output.Name, variables[value.Name]);
                }
            }

            return true;

            bool IsBound(Pattern.Variable field) =>
                variables.ContainsKey(field.Name) || deferredPairMembers.ContainsKey(field.Name);
        }
    }
}
