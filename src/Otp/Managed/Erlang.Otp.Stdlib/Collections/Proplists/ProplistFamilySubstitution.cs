namespace Erlang;

internal static class ProplistFamilySubstitution
{

    internal static async ValueTask<Term> Apply(
        ProcessContext context,
        Term mappings,
        Term properties,
        bool negate,
        ProplistFamilyFrame? pending = null
    )
    {
        var result = new List<Term>();
        string comprehension = negate ? ProplistFamilySource.NegationComprehension : ProplistFamilySource.AliasComprehension;
        int line = negate ? ProplistFamilySource.Negations : ProplistFamilySource.Aliases;
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            pending = ProplistFamilyTrace.Push(
                context,
                comprehension,
                2,
                line,
                negate ? ProplistFamilyContinuations.NegationResult : ProplistFamilyContinuations.AliasResult,
                pending
            );
            result.Add(await One(
                context,
                mappings,
                cell.Head,
                negate,
                pending
            ));
            properties = cell.Tail;
        }

        if (properties is not Nil)
            throw ProplistFamilyTrace.Generator(
                context,
                comprehension,
                2,
                properties,
                line,
                pending
            );

        return Cons.From(result);
    }

    private static async ValueTask<Term> One(
        ProcessContext context,
        Term mappings,
        Term property,
        bool negate,
        ProplistFamilyFrame? pending
    )
    {
        while (mappings is Cons cell)
        {
            await context.ReduceAsync();
            if (cell.Head is not TupleTerm { Items.Count: 2 } mapping)
                throw ProplistFamilyTrace.Clause(
                    context,
                    negate ? ProplistFamilySource.NegationOne : ProplistFamilySource.AliasOne,
                    [mappings, property],
                    negate ? ProplistFamilySource.NegationLookup : ProplistFamilySource.AliasLookup,
                    pending
                );
            if (ProplistFamilyLists.Matches(property, mapping.Items[0]))
            {
                if (negate)
                {
                    bool truth = property is Atom || property is TupleTerm { Items.Count: 2 } pair && pair.Items[1].Equals(ProplistFamilyNames.True);

                    return ProplistFamilyLookup.Property(mapping.Items[1], truth ? ProplistFamilyNames.False : ProplistFamilyNames.True);
                }

                if (property is Atom)

                    return ProplistFamilyLookup.Property(mapping.Items[1], ProplistFamilyNames.True);
                var tuple = (TupleTerm)property;
                Term[] values = tuple.Items.ToArray();
                values[0] = mapping.Items[1];

                return ProplistFamilyLookup.Property(Term.Tuple(values));
            }

            mappings = cell.Tail;
        }

        if (mappings is not Nil)
            throw ProplistFamilyTrace.Clause(
                context,
                negate ? ProplistFamilySource.NegationOne : ProplistFamilySource.AliasOne,
                [mappings, property],
                negate ? ProplistFamilySource.NegationLookup : ProplistFamilySource.AliasLookup,
                pending
            );

        return property;
    }
}
