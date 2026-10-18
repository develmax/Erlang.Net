namespace Erlang;

public static class ListFunctions
{
    public static async ValueTask<Term> Run(
        ProcessContext context,
        string operation,
        Term function,
        Term list,
        Term? accumulator = null
    )
    {
        if (operation is ListModuleNames.FoldLeft or ListModuleNames.Filter or ListModuleNames.Map or ListModuleNames.FilterMap)
            return await ListCallbackSourceExecution.Run(
                context,
                operation,
                function,
                list,
                accumulator
            );
        if (operation == ListModuleNames.FoldRight)
            return await ListFoldRight.Run(
                context,
                function,
                accumulator!,
                list
            );
        bool fold = operation is ListModuleNames.FoldLeft or ListModuleNames.FoldRight or ListModuleNames.MapFoldLeft or ListModuleNames.MapFoldRight;
        if (function is not FunctionTerm callback || callback.Arity != (fold ? 2 : 1))
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        bool right = operation is ListModuleNames.FoldRight or ListModuleNames.MapFoldRight;
        if (right)
        {
            var stack = new Stack<Term>();
            Term tail = list;
            while (tail is Cons cell)
            {
                stack.Push(cell.Head);
                tail = cell.Tail;
            }
            if (tail is not Nil)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            Term output = Nil.Value;
            while (stack.TryPop(out var item))
            {
                await context.ReduceAsync();
                var value = await callback.Invoke(context, new[] { item, accumulator! });
                if (operation == ListModuleNames.MapFoldRight)
                {
                    if (value is not TupleTerm { Items.Count: 2 } pair)
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value));
                    output = new Cons(pair.Items[0], output);
                    accumulator = pair.Items[1];
                }
                else
                    accumulator = value;
            }

            return operation == ListModuleNames.MapFoldRight ? Term.Tuple(output, accumulator!) : accumulator!;
        }
        if (operation is ListModuleNames.All or ListModuleNames.Any or ListModuleNames.FoldLeft or ListModuleNames.Map && list is not (Cons or Nil))
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), list));
        var accepted = new List<Term>();
        var rejected = new List<Term>();
        while (list is Cons cell)
        {
            await context.ReduceAsync();
            Term value = await callback.Invoke(context, fold ? new[] { cell.Head, accumulator! } : new[] { cell.Head });
            if (operation == ListModuleNames.FoldLeft)
                accumulator = value;
            else if (operation == ListModuleNames.MapFoldLeft)
            {
                if (value is not TupleTerm { Items.Count: 2 } pair)
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value));
                accepted.Add(pair.Items[0]);
                accumulator = pair.Items[1];
            }
            else if (operation == ListModuleNames.Map)
                accepted.Add(value);
            else if (operation != ListModuleNames.ForEach)
            {
                if (operation == ListModuleNames.FilterMap && value is TupleTerm { Items.Count: 2 } pair && pair.Items[0] is Atom { Name: ErlangBooleanAtoms.True })
                    accepted.Add(pair.Items[1]);
                else
                {
                    bool yes = value is Atom { Name: ErlangBooleanAtoms.True };
                    if (!yes && value is not Atom { Name: ErlangBooleanAtoms.False })
                        throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), value));
                    if (operation == ListModuleNames.All && !yes || operation == ListModuleNames.Any && yes)
                        return value;
                    if (operation == ListModuleNames.DropWhile && !yes)
                        return list;
                    if (operation == ListModuleNames.TakeWhile && !yes)
                        return Cons.From(accepted);
                    if (yes)
                        accepted.Add(cell.Head);
                    else
                        rejected.Add(cell.Head);
                }
            }
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return operation switch
        {
            ListModuleNames.All => Term.A(ErlangBooleanAtoms.True),
            ListModuleNames.Any => Term.A(ErlangBooleanAtoms.False),
            ListModuleNames.FoldLeft => accumulator!,
            ListModuleNames.MapFoldLeft => Term.Tuple(Cons.From(accepted), accumulator!),
            ListModuleNames.ForEach => Term.A(ListResultAtoms.Ok),
            ListModuleNames.Partition => Term.Tuple(Cons.From(accepted), Cons.From(rejected)),
            ListModuleNames.DropWhile => Nil.Value,
            _ => Cons.From(accepted)
        };
    }
}
