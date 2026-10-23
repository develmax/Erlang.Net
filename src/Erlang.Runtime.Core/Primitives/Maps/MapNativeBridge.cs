namespace Erlang;

internal static class MapNativeBridge
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            MapNativeBridgeNames.Module,
            MapNativeBridgeNames.Compare,
            2,
            (_, arguments) => ValueTask.FromResult<Term>(Term.I(Math.Sign(TermOrder.Compare(arguments[0], arguments[1], true))))
        );
        registry.Register(
            MapNativeBridgeNames.Module,
            MapNativeBridgeNames.Next,
            3,
            (context, arguments) => ValueTask.FromResult(Next(context, arguments))
        );
        registry.RegisterIntrinsic(
            MapNativeBridgeNames.Module,
            MapNativeBridgeNames.ComprehensionIterator,
            1,
            (context, arguments) => ValueTask.FromResult(ComprehensionIterator(context, arguments[0]))
        );
        registry.RegisterIntrinsic(
            MapNativeBridgeNames.Module,
            MapNativeBridgeNames.ComprehensionRefill,
            1,
            (context, arguments) => ValueTask.FromResult(arguments[0] is Cons cursor
                ? Next(context, [cursor.Head, cursor.Tail, Term.A(MapNativeBridgeNames.Iterator)])
                : throw new ErlangException(ErlangErrorReasons.FunctionClause))
        );
    }

    internal static bool IsCompilerHelper(string module, string function, int arity)
        => module == MapNativeBridgeNames.Module && arity == 1
            && function is MapNativeBridgeNames.ComprehensionIterator or MapNativeBridgeNames.ComprehensionRefill;

    private static Term ComprehensionIterator(ProcessContext context, Term source)
    {
        if (source is MapTerm map)
            return Next(context, [Term.I(0), map, Term.A(MapNativeBridgeNames.Iterator)]);
        Term remaining = source;
        while (remaining is TupleTerm { Items.Count: MapIteratorLayout.TupleArity } entry)
            remaining = entry.Items[MapIteratorLayout.ContinuationIndex];
        if (remaining is Atom { Name: MapIteratorAtoms.None })
            return source;
        if (remaining is not Cons cursor)
            return Nil.Value;
        try
        {
            Term next = Next(context, [cursor.Head, cursor.Tail, Term.A(MapNativeBridgeNames.Iterator)]);

            return ReferenceEquals(source, remaining) ? next : source;
        }
        catch (ErlangException error) when (error.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
        {
            return Nil.Value;
        }
    }

    private static Term Next(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[1] is not MapTerm map)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term path = arguments[0];
        if (arguments[2] is Atom { Name: MapNativeBridgeNames.Iterator })
        {
            if (path is not (Integer or Nil or Cons))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            if (path is Integer integer && map.Entries.Count > MapIteratorLayout.FlatMapMaximumEntries)
            {
                using var index = context.Runtime.AcquireHamtIndex429(map);

                return HamtIteratorCursor.Read(
                    map,
                    integer.Value,
                    null!,
                    index.Index.Root
                );
            }

            return MapIteration.Next(new Cons(path, map), context.Runtime);
        }
        if (path is not Integer offset || arguments[2] is not (Nil or Cons))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        using var identities = context.Runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
        using var owned = map.Entries.Count > MapIteratorLayout.FlatMapMaximumEntries
            ? context.Runtime.AcquireHamtIndex429(map) : null;

        return HamtCursorView.ReadIntegerListCursor(
            map,
            offset.Value,
            arguments[2],
            identities.Hash,
            ownedRoot429: owned?.Index.Root
        );
    }
}
