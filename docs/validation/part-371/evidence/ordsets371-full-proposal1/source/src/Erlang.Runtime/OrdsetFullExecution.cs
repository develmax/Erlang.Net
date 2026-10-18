namespace Erlang;

internal static class OrdsetFullExecution
{
    internal static async ValueTask<Term> Delegate(ProcessContext context, string function, Term set)
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, function);

        return await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            function == OrdsetFullNames.FromList ? ListModuleNames.UniqueSort : ListModuleNames.UniqueMerge,
            [set]
        );
    }

    internal static async ValueTask<Term> Binary(ProcessContext context, string function, Term first, Term second)
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, function);
        if (function == OrdsetFullNames.DeleteElement)
            return await Delete(context, first, second);
        var prefix = new List<Term>();
        var pending = new List<OrdsetFullFrame>();
        Term result;
        int line = function switch
        {
            OrdsetFullNames.Union => OrdsetFullNames.UnionLine,
            OrdsetFullNames.Intersection => OrdsetFullNames.IntersectionLine,
            OrdsetFullNames.Disjoint => OrdsetFullNames.DisjointLine,
            OrdsetFullNames.Subtract => OrdsetFullNames.SubtractLine,
            _ => OrdsetFullNames.SubsetLine
        };
        while (true)
        {
            await context.ReduceAsync();
            if (first is Cons one && second is Cons two)
            {
                int comparison = TermOrder.Compare(one.Head, two.Head);
                if (function == OrdsetFullNames.Subset && comparison < 0)
                    return CoreModules.Boolean(false);
                if (function == OrdsetFullNames.Disjoint && comparison == 0)
                    return CoreModules.Boolean(false);
                bool keep = function == OrdsetFullNames.Union
                    || function == OrdsetFullNames.Intersection && comparison == 0
                    || function == OrdsetFullNames.Subtract && comparison < 0;
                if (keep)
                {
                    prefix.Add(comparison > 0 ? two.Head : one.Head);
                    int callLine = function switch
                    {
                        OrdsetFullNames.Union when comparison < 0 => OrdsetFullNames.UnionLeftCall,
                        OrdsetFullNames.Union when comparison > 0 => OrdsetFullNames.UnionRightCall,
                        OrdsetFullNames.Union => OrdsetFullNames.UnionEqualCall,
                        OrdsetFullNames.Intersection => OrdsetFullNames.IntersectionEqualCall,
                        _ => OrdsetFullNames.SubtractLeftCall
                    };
                    OrdsetFullTrace.Push(pending, new(
                        OrdsetCoreNames.Module,
                        function,
                        2,
                        callLine
                    ));
                }
                if (comparison < 0)
                    first = one.Tail;
                else if (comparison > 0)
                {
                    if (function is OrdsetFullNames.Union or OrdsetFullNames.Intersection or OrdsetFullNames.Disjoint)
                        (first, second) = (two.Tail, first);
                    else
                        second = two.Tail;
                }
                else
                    (first, second) = (one.Tail, two.Tail);
                continue;
            }
            if (first is Nil || second is Nil)
            {
                result = function switch
                {
                    OrdsetFullNames.Union => first is Nil ? second : first,
                    OrdsetFullNames.Intersection => Nil.Value,
                    OrdsetFullNames.Disjoint => CoreModules.Boolean(true),
                    OrdsetFullNames.Subset => CoreModules.Boolean(first is Nil),
                    _ => first is Nil ? Nil.Value : first
                };
                break;
            }
            throw OrdsetFullTrace.Clause(
                context,
                OrdsetCoreNames.Module,
                function,
                [first, second],
                line,
                pending
            );
        }

        return Cons.From(prefix, result);
    }

    private static async ValueTask<Term> Delete(ProcessContext context, Term element, Term set)
    {
        var prefix = new List<Term>();
        var pending = new List<OrdsetFullFrame>();
        Term result;
        while (true)
        {
            await context.ReduceAsync();
            if (set is Nil)
            {
                result = Nil.Value;
                break;
            }
            if (set is not Cons cell)
                throw OrdsetFullTrace.Clause(
                    context,
                    OrdsetCoreNames.Module,
                    OrdsetFullNames.DeleteElement,
                    [element, set],
                    OrdsetFullNames.DeleteLine,
                    pending
                );
            int comparison = TermOrder.Compare(element, cell.Head);
            if (comparison <= 0)
            {
                result = comparison == 0 ? cell.Tail : set;
                break;
            }
            prefix.Add(cell.Head);
            OrdsetFullTrace.Push(pending, new(
                OrdsetCoreNames.Module,
                OrdsetFullNames.DeleteElement,
                2,
                OrdsetFullNames.DeleteLine
            ));
            set = cell.Tail;
        }

        return Cons.From(prefix, result);
    }

    internal static async ValueTask<Term> Intersection(ProcessContext context, Term sets)
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, OrdsetFullNames.Intersection);
        if (sets is Cons { Tail: Nil } singleton)
            return singleton.Head;
        if (sets is not Cons { Tail: Cons rest } first)
            throw OrdsetFullTrace.Clause(
                context,
                OrdsetCoreNames.Module,
                OrdsetFullNames.Intersection,
                [sets],
                OrdsetFullNames.IntersectionManyLine,
                []
            );
        Term result;
        using (OrdsetFullTrace.Enter(context, new(
            OrdsetCoreNames.Module,
            OrdsetFullNames.Intersection,
            1,
            OrdsetFullNames.IntersectionManyCall
        )))
            result = await Binary(
                context,
                OrdsetFullNames.Intersection,
                first.Head,
                rest.Head
            );
        Term remaining = rest.Tail;
        while (remaining is Cons cell)
        {
            await context.ReduceAsync();
            using (OrdsetFullTrace.Enter(context, new(
                OrdsetCoreNames.Module,
                OrdsetFullNames.IntersectionHelper,
                2,
                OrdsetFullNames.IntersectionHelperCall
            )))
                result = await Binary(
                    context,
                    OrdsetFullNames.Intersection,
                    result,
                    cell.Head
                );
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw OrdsetFullTrace.Clause(
                context,
                OrdsetCoreNames.Module,
                OrdsetFullNames.IntersectionHelper,
                [result, remaining],
                OrdsetFullNames.IntersectionHelperLine,
                []
            );

        return result;
    }
}
