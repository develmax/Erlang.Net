namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Expr? ReadDeferredRecordListSelector(
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            Pattern.Variable[] parameters
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || clause.Guard is not null
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Erlang.Pattern.Variable earlierResult, Value: Expr.Call earlier },
                        Expr.Match
                    {
                        Pattern: Erlang.Pattern.Tuple
                        {
                            Items: [Erlang.Pattern.Variable first, Erlang.Pattern.Variable second]
                        },
                        Value: Expr.Case { Value: Expr.Variable selector } decision
                    } assignment,
                        ..,
                        Expr.RecordUpdate { Value: Expr.Variable retained } update
                    ]
                } sequence
                || !TryResolveOrdinaryCallTarget(earlier, out _)
                || earlier.Arguments.Any(argument => argument is not (Expr.Variable or Expr.Literal))
                || ReadListPairSelectorBindings(decision) is not { } pair
                || !CanJoinMixedConstructedCasePair(decision, first.Name, second.Name)
                || !DistinctBindings(first, second) || bindings.ContainsKey(first.Name) || bindings.ContainsKey(second.Name)
                || bindings.ContainsKey(earlierResult.Name) || earlierResult.Name == first.Name || earlierResult.Name == second.Name
                || pair.Head.Name == earlierResult.Name || pair.Tail.Name == earlierResult.Name
                || bindings.ContainsKey(pair.Head.Name) || bindings.ContainsKey(pair.Tail.Name)
                || !bindings.TryGetValue(selector.Name, out var projection) || projection.Field is not int field
                || !bindings.TryGetValue(retained.Name, out var carrier) || carrier.Field is not null
                || carrier.Input != projection.Input || carrier.Input >= clause.Patterns.Count
                || clause.Patterns[carrier.Input] is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var record, out var shape) || record.Name != retained.Name
                || shape is not Erlang.Pattern.Tuple tuple || tuple.Items.Count != update.Arity
                || !HasTupleTag(tuple, out Atom tag) || tag.Name != update.Name)
            {
                return null;
            }

            // The unchanged, checked carrier already crosses the earlier call.
            // Read the field at its first use, after that call returns.
            var access = new Expr.RecordAccess(
                retained,
                update.Name,
                update.Arity,
                field
            );
            var items = sequence.Items.ToArray();
            items[1] = assignment with { Value = decision with { Value = access } };
            var rewritten = sequence with { Items = items };

            return ReadUsedEntryParameters(new Clause(parameters, null, rewritten)) is { } otherUses
                && !otherUses.Contains(selector.Name)
                ? rewritten
                : null;
        }

        private static (Erlang.Pattern.Variable Head, Erlang.Pattern.Variable Tail)? ReadListPairSelectorBindings(Expr.Case decision)
        {
            return decision.Clauses is
            [
            { Guard: null, Patterns: [var empty] },
            {
                Guard: null,
                Patterns: [
                        Erlang.Pattern.List
                {
                    Items: [Erlang.Pattern.Variable head],
                    Tail: Erlang.Pattern.Variable tail
                }]
            }
            ] && IsEmptyListPattern(empty) && DistinctBindings(head, tail)
                ? (head, tail)
                : null;
        }

        private bool CanUseCheckedRecordListPairSelector(Expr.Case decision) =>
            decision.Value is Expr.RecordAccess access
            && CheckedRecordSlot(access.Value, access.Name, access.Arity) is not null
            && ReadListPairSelectorBindings(decision) is { } pair
            && !variables.ContainsKey(pair.Head.Name) && !variables.ContainsKey(pair.Tail.Name);

        private Expr? ReadDeferredRecordListTails(
            Clause clause,
            Dictionary<string, (int Input, int? Field)> bindings,
            Pattern.Variable[] parameters
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || clause.Guard is not null
                || clause.Body is not Expr.Sequence
                {
                    Items: [_, .., Expr.RecordUpdate { Value: Expr.Variable retained } update]
                } sequence
                || !bindings.TryGetValue(retained.Name, out var source) || source.Field is not null
                || source.Input >= clause.Patterns.Count || clause.Patterns[source.Input] is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var bound, out var shape) || bound.Name != retained.Name
                || shape is not Pattern.Tuple tuple || tuple.Items.Count != update.Arity
                || !HasTupleTag(tuple, out Atom tag) || tag.Name != update.Name)
            {
                return null;
            }

            var prefix = sequence.Items.SkipLast(1).ToArray();
            if (!prefix.Any(statement => ReadCall(statement) is { } call && TryResolveOrdinaryCallTarget(call, out _))
                || ReadUsedEntryParameters(new Clause(parameters, null, new Expr.Sequence(prefix))) is not { } earlier)
            {
                return null;
            }

            var references = LocalFunctionEntries.Walk(update).OfType<Expr.Variable>()
                .GroupBy(variable => variable.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            RecordUpdateField[]? fields = null;
            for (int index = 0; index < update.Fields.Count; index++)
            {
                var field = update.Fields[index];
                if (field.Value is not Expr.List { Items.Count: > 0, Tail: Expr.Variable tail } list
                    || !bindings.TryGetValue(tail.Name, out var projection) || projection.Input != source.Input
                    || projection.Field != field.Index || earlier.Contains(tail.Name) || references.GetValueOrDefault(tail.Name) != 1)
                {
                    continue;
                }

                // This is the previous value of the same field. The checked
                // immutable record already survives the earlier call.
                fields ??= update.Fields.ToArray();
                fields[index] = field with
                {
                    Value = list with
                    {
                        Tail = new Expr.RecordAccess(
                            retained,
                            update.Name,
                            update.Arity,
                            field.Index
                        )
                    }
                };
            }

            return fields is null ? null : sequence with { Items = [.. prefix, update with { Fields = fields }] };

            static Expr.Call? ReadCall(Expr statement) => statement switch
            {
                Expr.Call call => call,
                Expr.Match { Pattern: Erlang.Pattern.Variable, Value: Expr.Call call } => call,
                _ => null
            };
        }

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
