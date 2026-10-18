namespace Erlang;

internal static class ListSortOperations
{
    public static Term Unique(ProcessContext context, Term list)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.UniqueSort);
        if (list is not Nil && list is not Cons { Tail: Nil or Cons })
            throw ListUniqueMergeTrace.Clause(
                context,
                ListModuleNames.UniqueSort,
                [list],
                ListUniqueMergeNames.UniqueSortLine,
                []
            );

        return Sort(list, true);
    }

    public static Term Sort(Term list, bool unique = false, Term? position = null)
    {
        int index = position is null ? 0 : ListKeyOperations.Position(position);
        if (list is Nil || list is Cons { Tail: Nil })
            return list;
        if (position is not null && list is not Cons { Tail: Cons })
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), list));
        var items = new List<(Term Value, Term Key)>();
        foreach (Term item in ListBasicOperations.Items(list))
            items.Add((item, index == 0 ? item : ListKeyOperations.Element(item, index)));
        var comparer = Comparer<Term>.Create((first, second) => TermOrder.Compare(first, second));
        var ordered = items.OrderBy(item => item.Key, comparer);
        var output = new List<Term>();
        Term? lastKey = null;
        foreach (var item in ordered)
        {
            Term key = item.Key;
            if (!unique || lastKey is null || !lastKey.NumericEquals(key))
                output.Add(item.Value);
            lastKey = key;
        }

        return Cons.From(output);
    }
}
