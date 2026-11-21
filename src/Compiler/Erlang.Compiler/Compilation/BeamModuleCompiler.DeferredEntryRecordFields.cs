namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Expr? ReadDeferredEntryRecordFields(
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            Pattern.Variable[] parameters
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || clause.Body is not Expr.Sequence { Items.Count: >= 3 } sequence
                || sequence.Items.Take(sequence.Items.Count - 1).Any(item => item is not Expr.Call call
                    || !TryResolveOrdinaryCallTarget(call, out _)))
            {
                return null;
            }

            var references = sequence.Items.Select(item => ReadUsedEntryParameters(new Clause(parameters, null, item))).ToArray();
            if (references.Any(names => names is null))
            {
                return null;
            }

            Expr[]? changed = null;
            for (int index = 1; index < sequence.Items.Count - 1; index++)
            {
                if (sequence.Items[index] is not Expr.Call { Arguments: [Expr.Variable field] } call
                    || !bindings.TryGetValue(field.Name, out var projection)
                    || projection.Field is not int position || projection.Input >= clause.Patterns.Count
                    || clause.Patterns[projection.Input] is not AliasPattern alias
                    || !TryReadPatternAlias(alias, out var record, out var shape)
                    || shape is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag)
                    || !references[^1]!.Contains(record.Name)
                    || references.Where((_, item) => item != index).Any(names => names!.Contains(field.Name)))
                {
                    continue;
                }

                // The whole checked tuple already survives the call. Read its immutable field at the use.
                changed ??= sequence.Items.ToArray();
                changed[index] = call with
                {
                    Arguments =
                    [
                        new Expr.RecordAccess(
                            new Expr.Variable(record.Name),
                            tag.Name,
                            tuple.Items.Count,
                            position
                        )
                    ]
                };
            }

            return changed is null ? null : sequence with { Items = changed };
        }
    }
}
