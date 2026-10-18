namespace Erlang;

internal static class ProplistFamilyExpansion
{

    internal static async ValueTask<Term> Expand(
        ProcessContext context,
        Term expansions,
        Term properties,
        ProplistFamilyFrame? pending = null
    )
    {
        if (!ProplistFamilyLists.ListCell(properties))
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilyNames.Expand,
                [expansions, properties],
                ProplistFamilySource.Expand,
                pending
            );
        var rules = new List<(Term Property, Term Value)>();
        var generatorFrames = ProplistFamilyTrace.Push(
            context,
            ProplistFamilyNames.Expand,
            2,
            ProplistFamilySource.ExpansionGenerator,
            ProplistFamilyContinuations.ExpandBuildRules,
            pending
        );
        Term? previous = null;
        while (expansions is Cons cell)
        {
            await context.ReduceAsync();
            if (cell.Head is TupleTerm { Items.Count: 2 } rule)
            {
                Term property = ProplistFamilyLookup.Property(rule.Items[0]);
                if (previous is null || !previous.Equals(property))
                    rules.Add((property, rule.Items[1]));
                previous = property;
                generatorFrames = ProplistFamilyTrace.Push(
                    context,
                    ProplistFamilySource.ExpandComprehension,
                    1,
                    ProplistFamilySource.ExpansionGenerator,
                    ProplistFamilyContinuations.ExpandRuleResult,
                    generatorFrames
                );
            }

            expansions = cell.Tail;
        }

        if (expansions is not Nil)
            throw ProplistFamilyTrace.Generator(
                context,
                ProplistFamilySource.ExpandComprehension,
                1,
                expansions,
                ProplistFamilySource.ExpansionGenerator,
                generatorFrames
            );
        foreach (var rule in rules)
            properties = await One(
                context,
                rule.Property,
                rule.Value,
                properties,
                ProplistFamilyTrace.Push(
                    context,
                    ProplistFamilySource.ExpandZero,
                    2,
                    ProplistFamilySource.ExpandZeroReturn,
                    ProplistFamilyContinuations.ExpandApplyRule,
                    ProplistFamilyTrace.Push(
                        context,
                        ProplistFamilyNames.Expand,
                        2,
                        ProplistFamilySource.ExpansionReturn,
                        ProplistFamilyContinuations.ExpandFlattenArgument,
                        pending
                    )
                )
            );

        return await Flatten(
            context,
            properties,
            pending
        );
    }

    private static async ValueTask<Term> Flatten(
        ProcessContext context,
        Term properties,
        ProplistFamilyFrame? pending
    )
    {
        var items = new List<Term>();
        var continuations = pending;
        ProplistFamilyFrame? invalidAppendContinuations = null;
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            items.Add(cell.Head);
            int line = ProplistFamilyLists.ListCell(cell.Head) ? ProplistFamilySource.FlattenListReturn : ProplistFamilySource.FlattenOtherReturn;
            continuations = ProplistFamilyTrace.Push(
                context,
                ProplistFamilySource.Flatten,
                1,
                line,
                ProplistFamilyLists.ListCell(cell.Head) ? ProplistFamilyContinuations.FlattenList : ProplistFamilyContinuations.FlattenOther,
                continuations
            );
            if (ProplistFamilyLists.ListCell(cell.Head) && !await ProplistFamilyLists.IsProper(context, cell.Head))
                invalidAppendContinuations = continuations;
            properties = cell.Tail;
        }

        if (properties is not Nil)
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilySource.Flatten,
                [properties],
                ProplistFamilySource.FlattenEntry,
                continuations
            );
        Term result = Nil.Value;
        for (int index = items.Count - 1; index >= 0; index--)
        {
            if (!ProplistFamilyLists.ListCell(items[index]))
            {
                result = new Cons(items[index], result);
                continue;
            }

            try
            {
                result = ProplistFamilyLists.Append(items[index], result);
            }
            catch (ErlangException)
            {
                throw ProplistFamilyTrace.Append(
                    context,
                    items[index],
                    result,
                    invalidAppendContinuations
                );
            }
        }

        return result;
    }

    private static async ValueTask<Term> One(
        ProcessContext context,
        Term expected,
        Term expansion,
        Term properties,
        ProplistFamilyFrame? pending
    )
    {
        if (!ProplistFamilyLists.Key(expected, out Term key))

            return properties;
        var prefix = new List<Term>();
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            if (ProplistFamilyLists.Matches(cell.Head, key))
            {
                Term minimal = ProplistFamilyLookup.Property(cell.Head);
                Term tail = minimal.Equals(expected) ? new Cons(
                    expansion,
                    await ProplistFamilyLookup.Delete(
                        context,
                        key,
                        cell.Tail,
                        ProplistFamilyTrace.Push(
                            context,
                            ProplistFamilySource.ExpandThree,
                            5,
                            ProplistFamilySource.ExpandThreeReturn,
                            ProplistFamilyContinuations.ExpandDelete,
                            pending
                        )
                    )
                ) : new Cons(minimal, cell.Tail);

                return Cons.From(prefix, tail);
            }

            prefix.Add(cell.Head);
            pending = ProplistFamilyTrace.Push(
                context,
                ProplistFamilySource.ExpandTwo,
                4,
                ProplistFamilySource.ExpandTwoReturn,
                ProplistFamilyContinuations.ExpandRetain,
                pending
            );
            properties = cell.Tail;
        }

        if (properties is not Nil)
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilySource.ExpandTwo,
                [key, expected, expansion, properties],
                ProplistFamilySource.ExpandTwoEntry,
                pending
            );

        return Cons.From(prefix);
    }

    internal static async ValueTask<Term> Stages(
        ProcessContext context,
        Term properties,
        Term stages,
        ProplistFamilyFrame? pending = null
    )
    {
        while (stages is Cons cell)
        {
            await context.ReduceAsync();
            if (cell.Head is not TupleTerm { Items.Count: 2 } stage || stage.Items[0] is not Atom operation)
                throw ProplistFamilyTrace.Clause(
                    context,
                    ProplistFamilySource.ApplyStages,
                    [properties, stages],
                    ProplistFamilySource.Stages,
                    pending
                );
            properties = operation.Name switch
            {
                ProplistFamilyNames.AliasStage => await ProplistFamilySubstitution.Apply(
                    context,
                    stage.Items[1],
                    properties,
                    false,
                    ProplistFamilyTrace.Push(
                        context,
                        ProplistFamilySource.ApplyStages,
                        2,
                        ProplistFamilySource.AliasStageReturn,
                        ProplistFamilyContinuations.StageAlias,
                        pending
                    )
                ),
                ProplistFamilyNames.NegationStage => await ProplistFamilySubstitution.Apply(
                    context,
                    stage.Items[1],
                    properties,
                    true,
                    ProplistFamilyTrace.Push(
                        context,
                        ProplistFamilySource.ApplyStages,
                        2,
                        ProplistFamilySource.NegationStageReturn,
                        ProplistFamilyContinuations.StageNegation,
                        pending
                    )
                ),
                ProplistFamilyNames.ExpansionStage => await Expand(
                    context,
                    stage.Items[1],
                    properties,
                    ProplistFamilyTrace.Push(
                        context,
                        ProplistFamilySource.ApplyStages,
                        2,
                        ProplistFamilySource.ExpansionStageReturn,
                        ProplistFamilyContinuations.StageExpand,
                        pending
                    )
                ),
                _ => throw ProplistFamilyTrace.Clause(
                    context,
                    ProplistFamilySource.ApplyStages,
                    [properties, stages],
                    ProplistFamilySource.Stages,
                    pending
                )
            };
            stages = cell.Tail;
        }

        if (stages is not Nil)
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilySource.ApplyStages,
                [properties, stages],
                ProplistFamilySource.Stages,
                pending
            );

        return properties;
    }
}
