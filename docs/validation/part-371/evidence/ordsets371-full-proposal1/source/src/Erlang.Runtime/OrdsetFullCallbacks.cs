namespace Erlang;

internal static class OrdsetFullCallbacks
{
    internal static async ValueTask<Term> Fold(ProcessContext context, Term function, Term accumulator, Term set)
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, OrdsetFullNames.Fold);
        if (function is not FunctionTerm { Arity: 2 } callback)
            throw OrdsetFullTrace.Clause(
                context,
                ListModuleNames.Module,
                ListModuleNames.FoldLeft,
                [function, accumulator, set],
                OrdsetFullNames.ListsFoldLine,
                []
            );
        if (set is not (Cons or Nil))
            throw OrdsetFullTrace.Error(
                context,
                new(ListModuleNames.Module, ListModuleNames.FoldLeft, 3, OrdsetFullNames.ListsFoldCase),
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), set),
                []
            );
        bool first = true;
        while (set is Cons cell)
        {
            await context.ReduceAsync();
            accumulator = await OrdsetFullTrace.Invoke(
                context,
                callback,
                [cell.Head, accumulator],
                new(ListModuleNames.Module, first ? ListModuleNames.FoldLeft : OrdsetFullNames.FoldHelper, 3,
                    first ? OrdsetFullNames.ListsFoldCall : OrdsetFullNames.ListsFoldHelperCall),
                []
            );
            first = false;
            set = cell.Tail;
        }
        if (set is not Nil)
            throw OrdsetFullTrace.Clause(
                context,
                ListModuleNames.Module,
                OrdsetFullNames.FoldHelper,
                [function, accumulator, set],
                OrdsetFullNames.ListsFoldHelperLine,
                []
            );

        return accumulator;
    }

    internal static async ValueTask<Term> Transform(ProcessContext context, string operation, Term function, Term set)
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, operation);
        if (operation == OrdsetFullNames.Filter)
            return await Filter(context, function, set);
        Term transformed;
        using (OrdsetFullTrace.Enter(context, new(
            OrdsetCoreNames.Module,
            operation,
            2,
            operation == OrdsetFullNames.Map ? OrdsetFullNames.MapCall : OrdsetFullNames.FilterMapCall
        )))
            transformed = await TransformList(
                context,
                operation,
                function,
                set
            );

        return await OrdsetFullExecution.Delegate(context, OrdsetFullNames.FromList, transformed);
    }

    private static async ValueTask<Term> Filter(ProcessContext context, Term function, Term set)
    {
        if (function is not FunctionTerm { Arity: 1 } callback)
            throw OrdsetFullTrace.Clause(
                context,
                ListModuleNames.Module,
                ListModuleNames.Filter,
                [function, set],
                OrdsetFullNames.ListsFilterLine,
                []
            );
        var values = new List<Term>();
        var pending = new List<OrdsetFullFrame>();
        var frame = new OrdsetFullFrame(
            ListModuleNames.Module,
            OrdsetFullNames.FilterComprehension,
            2,
            OrdsetFullNames.ListsFilterCall
        );
        while (set is Cons cell)
        {
            await context.ReduceAsync();
            Term result = await OrdsetFullTrace.Invoke(
                context,
                callback,
                [cell.Head],
                frame,
                pending
            );
            if (result is Atom { Name: ErlangBooleanAtoms.True })
            {
                values.Add(cell.Head);
                OrdsetFullTrace.Push(pending, frame);
            }
            else if (result is not Atom { Name: ErlangBooleanAtoms.False })
                throw OrdsetFullTrace.Error(
                    context,
                    frame,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadFilter), result),
                    pending
                );
            set = cell.Tail;
        }
        if (set is not Nil)
            throw OrdsetFullTrace.Error(
                context,
                frame,
                Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), set),
                pending
            );

        return Cons.From(values);
    }

    private static async ValueTask<Term> TransformList(ProcessContext context, string operation, Term function, Term set)
    {
        bool map = operation == OrdsetFullNames.Map;
        string publicName = map ? ListModuleNames.Map : ListModuleNames.FilterMap;
        int publicLine = map ? OrdsetFullNames.ListsMapLine : OrdsetFullNames.ListsFilterMapLine;
        if (function is not FunctionTerm { Arity: 1 } callback)
            throw OrdsetFullTrace.Clause(
                context,
                ListModuleNames.Module,
                publicName,
                [function, set],
                publicLine,
                []
            );
        if (map && set is not (Cons or Nil))
            throw OrdsetFullTrace.Error(
                context,
                new(ListModuleNames.Module, ListModuleNames.Map, 2, OrdsetFullNames.ListsMapCase),
                Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), set),
                []
            );
        var values = new List<Term>();
        var pending = new List<OrdsetFullFrame>();
        bool first = true;
        while (set is Cons cell)
        {
            await context.ReduceAsync();
            string owner = map ? (first ? ListModuleNames.Map : OrdsetFullNames.MapHelper) : OrdsetFullNames.FilterMapHelper;
            int line = map ? (first ? OrdsetFullNames.ListsMapCall : OrdsetFullNames.ListsMapHelperCall) : OrdsetFullNames.ListsFilterMapCall;
            var frame = new OrdsetFullFrame(ListModuleNames.Module, owner, 2, line);
            Term result = await OrdsetFullTrace.Invoke(
                context,
                callback,
                [cell.Head],
                frame,
                pending
            );
            if (map)
            {
                values.Add(result);
                OrdsetFullTrace.Push(pending, frame);
            }
            else if (result is Atom { Name: ErlangBooleanAtoms.True })
            {
                values.Add(cell.Head);
                OrdsetFullTrace.Push(pending, frame with { Line = OrdsetFullNames.ListsFilterMapKeep });
            }
            else if (result is TupleTerm { Items.Count: 2 } pair && pair.Items[0] is Atom { Name: ErlangBooleanAtoms.True })
            {
                values.Add(pair.Items[1]);
                OrdsetFullTrace.Push(pending, frame with { Line = OrdsetFullNames.ListsFilterMapReplace });
            }
            else if (result is not Atom { Name: ErlangBooleanAtoms.False })
                throw OrdsetFullTrace.Error(
                    context,
                    frame,
                    Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), result),
                    pending
                );
            first = false;
            set = cell.Tail;
        }
        if (set is not Nil)
            throw OrdsetFullTrace.Clause(
                context,
                ListModuleNames.Module,
                map ? OrdsetFullNames.MapHelper : OrdsetFullNames.FilterMapHelper,
                [function, set],
                map ? OrdsetFullNames.ListsMapHelperLine : OrdsetFullNames.ListsFilterMapHelperLine,
                pending
            );

        return Cons.From(values);
    }
}
