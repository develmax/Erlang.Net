namespace Erlang;

internal static class ProplistFamilyProjection
{

    internal static async ValueTask<Term> Normalize(
        ProcessContext context,
        Term properties,
        Term stages
    )
    {
        Term normalized = await ProplistFamilyExpansion.Stages(
            context,
            properties,
            stages,
            ProplistFamilyTrace.Push(
                context,
                ProplistFamilyNames.Normalize,
                2,
                ProplistFamilySource.NormalizeReturn,
                ProplistFamilyContinuations.NormalizeStages
            )
        );

        return await ProplistFamilyLookup.Transform(context, normalized, false);
    }

    internal static async ValueTask<Term> Split(
        ProcessContext context,
        Term properties,
        Term keys
    )
    {
        var outer = ProplistFamilyTrace.Push(
            context,
            ProplistFamilyNames.Split,
            2,
            ProplistFamilySource.SplitReturn,
            ProplistFamilyContinuations.SplitStore
        );
        var generator = outer;
        var ordered = new List<Term>();
        Term remainingKeys = keys;
        while (remainingKeys is Cons cell)
        {
            await context.ReduceAsync();
            ordered.Add(cell.Head);
            generator = ProplistFamilyTrace.Push(
                context,
                ProplistFamilySource.SplitComprehension,
                1,
                ProplistFamilySource.SplitReturn,
                ProplistFamilyContinuations.SplitKeyResult,
                generator
            );
            remainingKeys = cell.Tail;
        }

        if (remainingKeys is not Nil)
            throw ProplistFamilyTrace.Generator(
                context,
                ProplistFamilySource.SplitComprehension,
                1,
                remainingKeys,
                ProplistFamilySource.SplitReturn,
                generator
            );
        var buckets = new Dictionary<Term, List<Term>>();
        foreach (Term key in ordered)
            buckets[key] = [];
        var rest = new List<Term>();
        while (properties is Cons cell)
        {
            await context.ReduceAsync();
            if (ProplistFamilyLists.Key(cell.Head, out Term key) && buckets.TryGetValue(key, out var bucket))
                bucket.Add(cell.Head);
            else
                rest.Add(cell.Head);
            properties = cell.Tail;
        }

        if (properties is not Nil)
        {
            var store = new MapTerm(buckets.ToDictionary(pair => pair.Key, pair => (Term)Cons.From(pair.Value.AsEnumerable().Reverse())));
            throw ProplistFamilyTrace.Clause(
                context,
                ProplistFamilyNames.Split,
                [properties, store, Cons.From(rest.AsEnumerable().Reverse())],
                ProplistFamilySource.Split,
                outer
            );
        }

        return Term.Tuple(Cons.From(ordered.Select(key => Cons.From(buckets[key]))), Cons.From(rest));
    }

    internal static async ValueTask<Term> ToMap(ProcessContext context, Term properties)
    {
        FunctionTerm callback = ProplistFamilyToMapFunction.Create(context);
        var items = new List<Term>();
        Term reached = properties;
        while (reached is Cons cell)
        {
            await context.ReduceAsync();
            items.Add(cell.Head);
            reached = cell.Tail;
        }
        if (reached is not Nil)
        {
            using var tail = context.EnterSourceTailCall(
                ProplistFamilyNames.Module,
                ProplistFamilyNames.ToMap
            );
            throw ListFoldRight.Failure(
                context,
                callback,
                new MapTerm([]),
                reached,
                items.Count != 0
            );
        }
        var values = new Dictionary<Term, Term>();
        for (int index = items.Count - 1; index >= 0; index--)
        {
            await context.ReduceAsync();
            ProplistFamilyToMapFunction.FoldInto(items[index], values);
        }

        return new MapTerm(values);
    }

    internal static async ValueTask<Term> ToMap(
        ProcessContext context,
        Term properties,
        Term stages
    )
    {
        Term normalized = await ProplistFamilyExpansion.Stages(
            context,
            properties,
            stages,
            ProplistFamilyTrace.Push(
                context,
                ProplistFamilyNames.ToMap,
                2,
                ProplistFamilySource.ToMapStagesReturn,
                ProplistFamilyContinuations.ToMapStages
            )
        );

        return await ToMap(context, normalized);
    }

    internal static Term FromMap(ProcessContext context, Term map)
    {
        try
        {
            return MapOperations.ToList(map, context.Runtime);
        }
        catch (ErlangException error)
        {
            throw FromMapFailure(context, map, error);
        }
    }

    private static ErlangException FromMapFailure(ProcessContext context, Term map, ErlangException error)
    {
        Term location = Term.List(
            Term.Tuple(Term.A(ProplistFamilySource.File), Cons.From(ProplistFamilySource.MapsFileName.Select(value => Term.I(value)))),
            Term.Tuple(Term.A(ProplistFamilySource.Line), Term.I(ProplistFamilySource.MapsToList)),
            Term.Tuple(
                Term.A(RuntimeExceptionNames.ErrorInfo),
                new MapTerm(new Dictionary<Term, Term> { [Term.A(RuntimeExceptionNames.Module)] = Term.A(RuntimeExceptionNames.Formatter(MapModuleNames.Module)!) })
            )
        );
        Term frame = Term.Tuple(
            Term.A(MapModuleNames.Module),
            Term.A(MapModuleNames.ToList),
            Term.List(map),
            location
        );

        return ProplistFamilyTrace.Capture(
            context,
            error,
            frame
        );
    }
}
