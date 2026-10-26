namespace Erlang;

internal static class ProplistFamilyLookup
{

    internal static Term Property(Term value) => value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom && tuple.Items[1].Equals(ProplistFamilyNames.True) ? tuple.Items[0] : value;

    internal static Term Property(Term key, Term value) => key is Atom && value.Equals(ProplistFamilyNames.True) ? key : Term.Tuple(key, value);

    internal static async ValueTask<Term> Transform(
        ProcessContext context,
        Term properties,
        bool unfold,
        ProplistFamilyFrame? pending = null
    )
    {
        var result = new List<Term>();
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            result.Add(unfold && cell.Head is Atom ? Term.Tuple(cell.Head, ProplistFamilyNames.True) : unfold ? cell.Head : Property(cell.Head));
            pending = unfold ? ProplistFamilyTrace.Push(
                context,
                ProplistFamilyNames.Unfold,
                1,
                cell.Head is Atom ? ProplistFamilySource.UnfoldAtomReturn : ProplistFamilySource.UnfoldOtherReturn,
                cell.Head is Atom ? ProplistFamilyContinuations.UnfoldAtom : ProplistFamilyContinuations.UnfoldOther,
                pending
            ) : ProplistFamilyTrace.Push(
                context,
                ProplistFamilySource.CompactComprehension,
                1,
                ProplistFamilySource.Compact,
                ProplistFamilyContinuations.CompactResult,
                pending
            );
            properties = cell.Tail;
        }

        if (properties is not Nil)
            throw unfold ? ProplistFamilyTrace.Clause(
                context,
                ProplistFamilyNames.Unfold,
                [properties],
                ProplistFamilySource.Unfold,
                pending
            ) : ProplistFamilyTrace.Generator(
                context,
                ProplistFamilySource.CompactComprehension,
                1,
                properties,
                ProplistFamilySource.Compact,
                pending
            );

        return Cons.From(result);
    }

    internal static async ValueTask<Term> Find(
        ProcessContext context,
        Term key,
        Term properties,
        string mode
    )
    {
        var result = new List<Term>();
        ProplistFamilyFrame? invalidAppendContinuations = null;
        ProplistFamilyFrame? pending = null;
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            Term property = cell.Head;
            if (ProplistFamilyLists.Matches(property, key))
            {
                if (mode == ProplistFamilyNames.IsDefined)

                    return ProplistFamilyNames.True;
                if (mode == ProplistFamilyNames.GetBool)

                    return property is Atom || property is TupleTerm { Items.Count: 2 } boolean && boolean.Items[1].Equals(ProplistFamilyNames.True) ? ProplistFamilyNames.True : ProplistFamilyNames.False;
                Term entry = property is Atom ? Term.Tuple(key, ProplistFamilyNames.True) : property;
                if (mode == ProplistFamilyNames.Lookup)

                    return entry;
                if (mode == ProplistFamilyNames.LookupAll)
                {
                    result.Add(entry);
                    pending = ProplistFamilyTrace.Push(
                        context,
                        mode,
                        2,
                        property is Atom ? ProplistFamilySource.LookupAllAtomReturn : ProplistFamilySource.LookupAllTupleReturn,
                        property is Atom ? ProplistFamilyContinuations.LookupAllAtom : ProplistFamilyContinuations.LookupAllTuple,
                        pending
                    );
                }
                else if (property is Atom || property is TupleTerm { Items.Count: 2 })
                {
                    Term value = property is Atom ? ProplistFamilyNames.True : ((TupleTerm)property).Items[1];
                    result.Add(value);
                    int line = mode == ProplistFamilyNames.GetAllValues ? property is Atom ? ProplistFamilySource.GetAllValuesAtomReturn : ProplistFamilySource.GetAllValuesTupleReturn : property is Atom ? ProplistFamilySource.AppendAtomReturn : ProplistFamilyLists.ListCell(value) ? ProplistFamilySource.AppendListReturn : ProplistFamilySource.AppendOtherReturn;
                    object continuationIdentity = mode == ProplistFamilyNames.GetAllValues
                        ? property is Atom ? ProplistFamilyContinuations.AllValuesAtom : ProplistFamilyContinuations.AllValuesTuple
                        : property is Atom ? ProplistFamilyContinuations.AppendAtom
                            : ProplistFamilyLists.ListCell(value) ? ProplistFamilyContinuations.AppendList : ProplistFamilyContinuations.AppendOther;
                    pending = ProplistFamilyTrace.Push(
                        context,
                        mode,
                        2,
                        line,
                        continuationIdentity,
                        pending
                    );
                    if (mode == ProplistFamilyNames.AppendValues && ProplistFamilyLists.ListCell(value)
                        && !await ProplistFamilyLists.IsProper(context, value))
                        invalidAppendContinuations = pending;
                }
            }

            properties = cell.Tail;
        }

        if (properties is not Nil)
        {
            int line = mode switch
            {
                ProplistFamilyNames.Lookup => ProplistFamilySource.Lookup,
                ProplistFamilyNames.LookupAll => ProplistFamilySource.LookupAll,
                ProplistFamilyNames.IsDefined => ProplistFamilySource.IsDefined,
                ProplistFamilyNames.GetAllValues => ProplistFamilySource.GetAllValues,
                ProplistFamilyNames.AppendValues => ProplistFamilySource.AppendValues,
                _ => ProplistFamilySource.GetBool
            };
            throw ProplistFamilyTrace.Clause(
                context,
                mode,
                [key, properties],
                line,
                pending
            );
        }

        if (mode == ProplistFamilyNames.Lookup)

            return ProplistFamilyNames.None;
        if (mode is ProplistFamilyNames.GetBool or ProplistFamilyNames.IsDefined)

            return ProplistFamilyNames.False;
        if (mode != ProplistFamilyNames.AppendValues)

            return Cons.From(result);
        Term appended = Nil.Value;
        for (int index = result.Count - 1; index >= 0; index--)
        {
            if (!ProplistFamilyLists.ListCell(result[index]))
            {
                appended = new Cons(result[index], appended);
                continue;
            }

            try
            {
                appended = ProplistFamilyLists.Append(result[index], appended);
            }
            catch (ErlangException)
            {
                throw ProplistFamilyTrace.Append(
                    context,
                    result[index],
                    appended,
                    invalidAppendContinuations
                );
            }
        }

        return appended;
    }

    internal static async ValueTask<Term> Keys(ProcessContext context, Term properties)
    {
        var seen = new HashSet<Term>();
        var keys = new List<Term>();
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            if (ProplistFamilyLists.Key(cell.Head, out Term key) && seen.Add(key))
                keys.Add(key);
            properties = cell.Tail;
        }

        if (properties is not Nil)
        {
            var set = new MapTerm(keys.ToDictionary(key => key, _ => (Term)Nil.Value));
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilyNames.GetKeys,
                [properties, set],
                ProplistFamilySource.Keys,
                ProplistFamilyTrace.Push(
                    context,
                    ProplistFamilyNames.GetKeys,
                    1,
                    ProplistFamilySource.KeysReturn,
                    ProplistFamilyContinuations.KeysSet
                )
            );
        }

        return Cons.From(keys);
    }

    internal static async ValueTask<Term> Delete(
        ProcessContext context,
        Term key,
        Term properties,
        ProplistFamilyFrame? pending = null
    )
    {
        var result = new List<Term>();
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            if (!ProplistFamilyLists.Matches(cell.Head, key))
            {
                result.Add(cell.Head);
                pending = ProplistFamilyTrace.Push(
                    context,
                    ProplistFamilyNames.Delete,
                    2,
                    ProplistFamilySource.DeleteReturn,
                    ProplistFamilyContinuations.DeleteRetain,
                    pending
                );
            }

            properties = cell.Tail;
        }

        if (properties is not Nil)
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilyNames.Delete,
                [key, properties],
                ProplistFamilySource.Delete,
                pending
            );

        return Cons.From(result);
    }
}
