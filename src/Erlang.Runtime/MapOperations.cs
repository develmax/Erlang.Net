namespace Erlang;

public static class MapOperations
{
    public static MapTerm Intersect(Term left, Term right)
    {
        var first = RequireMap(left);
        var second = RequireMap(right);
        var smaller = first.Entries.Count <= second.Entries.Count ? first : second;
        var entries = new List<KeyValuePair<Term, Term>>();
        foreach (var entry in smaller.Entries)
            if (first.TryGet(entry.Key, out _) && second.TryGet(entry.Key, out var value))
                entries.Add(new(entry.Key, value!));

        return new MapTerm(entries);
    }

    public static async ValueTask<Term> Combine(
        ProcessContext context,
        Term function,
        Term left,
        Term right,
        bool intersection
    )
    {
        if (function is not FunctionTerm { Arity: 3 } callback)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var first = RequireMap(left);
        var second = RequireMap(right);
        bool traverseFirst = intersection ? first.Entries.Count <= second.Entries.Count : first.Entries.Count < second.Entries.Count;
        var smaller = traverseFirst ? first : second;
        var larger = traverseFirst ? second : first;
        var output = intersection ? new Dictionary<Term, Term>() : larger.Entries.ToDictionary(entry => entry.Key, entry => entry.Value);
        foreach (var entry in smaller.Entries)
        {
            await context.ReduceAsync();
            if (larger.TryGet(entry.Key, out var other))
                output[entry.Key] = await callback.Invoke(context, new[] { entry.Key, traverseFirst ? entry.Value : other!, traverseFirst ? other! : entry.Value });
            else if (!intersection)
                output[entry.Key] = entry.Value;
        }

        return new MapTerm(output);
    }

    public static async ValueTask<Term> GroupsFromList(
        ProcessContext context,
        Term function,
        Term list,
        Term? valueFunction = null
    )
    {
        if (function is not FunctionTerm { Arity: 1 } keyCallback || valueFunction is not null && valueFunction is not FunctionTerm { Arity: 1 })
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var items = Cons.Items(list).ToArray();
        var groups = new Dictionary<Term, Term>();
        for (int i = items.Length - 1; i >= 0; i--)
        {
            await context.ReduceAsync();
            Term item = items[i];
            Term key = await keyCallback.Invoke(context, new[] { item });
            Term value = valueFunction is FunctionTerm valueCallback ? await valueCallback.Invoke(context, new[] { item }) : item;
            groups.TryGetValue(key, out var existing);
            groups[key] = new Cons(value, existing ?? Nil.Value);
        }

        return new MapTerm(groups);
    }

    public static Term ToList(Term source) => ToList(source, null);

    public static Term ToList(Term source, ProcessRuntime? runtime)
    {
        try
        {
            return Cons.From(MapIteration.Entries(source, runtime).Select(entry => Term.Tuple(entry.Key, entry.Value)));
        }
        catch (ErlangException)
        {
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), source));
        }
    }

    public static MapTerm RequireMap(Term source) => source as MapTerm ?? throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), source));

    public static bool IsIteratorValid(Term iterator) => IsIteratorValid(iterator, null);

    public static bool IsIteratorValid(Term iterator, ProcessRuntime? runtime)
    {
        try
        {
            while (iterator is TupleTerm { Items.Count: 3 } pair)
                iterator = MapIteration.Next(pair.Items[2], runtime);
            MapIteration.Next(iterator, runtime);

            return true;
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return false;
        }
    }

    public static MapTerm Put(
        Term key,
        Term value,
        Term source,
        bool existing = false
    )
    {
        var map = RequireMap(source);
        if (existing && !map.TryGet(key, out _))
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

        return new MapTerm(map.Entries.Append(new(key, value)));
    }

    public static MapTerm Remove(Term key, Term source) => new(RequireMap(source).Entries.Where(entry => !entry.Key.Equals(key)));

    public static Term Take(Term key, Term source)
    {
        var map = RequireMap(source);

        return map.TryGet(key, out var value) ? Term.Tuple(value!, Remove(key, map)) : Term.A(MapResultAtoms.Error);
    }

    public static MapTerm FromList(Term source)
    {
        var entries = new List<KeyValuePair<Term, Term>>();
        while (source is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } pair)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            entries.Add(new(pair.Items[0], pair.Items[1]));
            source = cell.Tail;
        }
        if (source is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new MapTerm(entries);
    }

    public static MapTerm SelectKeys(Term keys, Term source, bool keep)
    {
        var map = RequireMap(source);
        if (keys is not (Cons or Nil))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var selected = new HashSet<Term>();
        while (keys is Cons cell)
        {
            selected.Add(cell.Head);
            keys = cell.Tail;
        }
        if (keys is not Nil)
            throw new ErlangException(ErlangErrorReasons.FunctionClause);

        return new MapTerm(map.Entries.Where(entry => selected.Contains(entry.Key) == keep));
    }

    public static async ValueTask<Term> UpdateWith(
        ProcessContext context,
        Term key,
        Term function,
        Term source,
        Term? initial = null
    )
    {
        var map = RequireMap(source);
        if (function is not FunctionTerm { Arity: 1 } callback)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (!map.TryGet(key, out var value))
        {
            if (initial is null)
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

            return Put(key, initial, map);
        }
        var updated = await callback.Invoke(context, new[] { value! });

        return Put(key, updated, map);
    }

    public static async ValueTask<Term> Transform(
        ProcessContext context,
        string operation,
        Term function,
        Term source,
        Term? accumulator = null
    )
    {
        int arity = operation == MapModuleNames.Fold ? 3 : 2;
        if (function is not FunctionTerm callback || callback.Arity != arity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term iterator = source is MapTerm map ? MapIteration.Iterator(map) : source;
        var entries = new List<KeyValuePair<Term, Term>>();
        while (true)
        {
            Term next;
            try
            {
                next = MapIteration.Next(iterator, context.Runtime);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), source));
            }
            if (next is Atom { Name: MapIteratorAtoms.None })
                break;
            if (next is not TupleTerm { Items.Count: 3 } pair)
                throw new ErlangException(ErlangErrorReasons.FunctionClause);
            await context.ReduceAsync();
            Term value = await callback.Invoke(context, arity == 3 ? new[] { pair.Items[0], pair.Items[1], accumulator! } : new[] { pair.Items[0], pair.Items[1] });
            if (operation == MapModuleNames.Fold)
                accumulator = value;
            else if (operation == MapModuleNames.Map)
                entries.Add(new(pair.Items[0], value));
            else if (operation != MapModuleNames.ForEach)
            {
                if (value is Atom { Name: ErlangBooleanAtoms.True })
                    entries.Add(new(pair.Items[0], pair.Items[1]));
                else if (value is Atom { Name: ErlangBooleanAtoms.False })
                {
                }
                else if (operation == MapModuleNames.FilterMap && value is TupleTerm { Items.Count: 2 } updated && updated.Items[0] is Atom { Name: ErlangBooleanAtoms.True })
                    entries.Add(new(pair.Items[0], updated.Items[1]));
                else
                    throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), value));
            }
            iterator = pair.Items[2];
        }

        return operation == MapModuleNames.Fold ? accumulator! : operation == MapModuleNames.ForEach ? Term.A(MapResultAtoms.Ok) : new MapTerm(entries);
    }
}
