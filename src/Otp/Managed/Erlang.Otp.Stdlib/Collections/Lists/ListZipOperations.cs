namespace Erlang;

internal static class ListZipOperations
{
    public static async ValueTask<Term> Zip(
        ProcessContext context,
        IReadOnlyList<Term> inputs,
        Term? mode = null,
        Term? function = null
    )
    {
        mode ??= Term.A(ListZipModes.Fail);
        var lists = inputs.ToArray();
        var output = new List<Term>();
        bool comprehension = false;
        while (true)
        {
            bool allCells = lists.All(list => list is Cons);
            bool allEmpty = lists.All(list => list is Nil);
            bool trim = mode is Atom { Name: ListZipModes.Trim };
            TupleTerm? defaults = mode is TupleTerm { Items.Count: 2 } option && option.Items[0] is Atom { Name: ListZipModes.Pad } && option.Items[1] is TupleTerm values && values.Items.Count == lists.Length ? values : null;
            bool validMode = trim || defaults is not null || mode is Atom { Name: ListZipModes.Fail };
            if (!allCells)
            {
                if (comprehension && lists.Any(list => list is not (Cons or Nil)))
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), lists.First(list => list is not (Cons or Nil))));
                if (allEmpty && validMode || trim && lists.All(list => list is Cons or Nil))
                {
                    if (function is not null && (function is not FunctionTerm callback || callback.Arity != lists.Length))
                        throw new ErlangException(ErlangErrorReasons.FunctionClause);

                    return Cons.From(output);
                }
                if (defaults is null || lists.Any(list => list is not (Cons or Nil)))
                    throw new ErlangException(ErlangErrorReasons.FunctionClause);
                comprehension = lists.Count(list => list is Cons) == 1;
            }
            var arguments = new Term[lists.Length];
            for (int i = 0; i < lists.Length; i++)
                if (lists[i] is Cons cell)
                {
                    arguments[i] = cell.Head;
                    lists[i] = cell.Tail;
                }
                else
                    arguments[i] = defaults!.Items[i];
            await context.ReduceAsync();
            output.Add(function is null ? new TupleTerm(arguments) : await ListCallback.Invoke(context, function, arguments));
        }
    }

    public static Term Unzip(Term list, int arity)
    {
        var outputs = Enumerable.Range(0, arity).Select(_ => new List<Term>()).ToArray();
        foreach (Term item in ListBasicOperations.Items(list))
        {
            if (item is not TupleTerm tuple || tuple.Items.Count != arity)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            for (int i = 0; i < arity; i++)
                outputs[i].Add(tuple.Items[i]);
        }

        return new TupleTerm(outputs.Select(items => Cons.From(items)).ToArray());
    }
}
