using static Erlang.CoreModules;

namespace Erlang;

public sealed class ManagedStdlibRegistration456 : IRuntimeManagedLibrary456
{
    public void Register(ModuleRegistry r, RuntimeManagedLibraryStage456 stage)
    {
        switch (stage)
        {
            case RuntimeManagedLibraryStage456.Collections: RegisterCollections(r); break;
            case RuntimeManagedLibraryStage456.Modules: RegisterModules(r); break;
            case RuntimeManagedLibraryStage456.BinaryWrappers: BinaryWrappersModule.Register(r); break;
            default: throw new ArgumentOutOfRangeException(nameof(stage));
        }
    }

    private static void RegisterCollections(ModuleRegistry r)
    {
        void Add(
            string module,
            string name,
            int arity,
            Func<ProcessContext, IReadOnlyList<Term>, Term> body
        )
            => r.Register(
                module,
                name,
                arity,
                (context, arguments) => ValueTask.FromResult(body(context, arguments))
            );

        void AddVmSpawn641(string name, Func<ProcessContext, IReadOnlyList<Term>, Term> body)
            => r.RegisterCanonicalRuntimeRead535(
                ErlangModuleNames.Module,
                name,
                1,
                (context, arguments) => ValueTask.FromResult(body(context, arguments))
            );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            1,
            (c, a) => ListOperations.Reverse(a[0])
        );
        r.Register(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            2,
            (c, a) => ListNativeTraversal.Reverse(c, a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Append,
            2,
            (c, a) => Cons.From(Cons.Items(a[0]), a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Append,
            1,
            (c, a) => ListOperations.Append(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Duplicate,
            2,
            (c, a) => ListOperations.Duplicate(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Flatten,
            1,
            (c, a) => ListOperations.Flatten(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Flatten,
            2,
            (c, a) => ListOperations.Flatten(a[0], a[1])
        );
        r.Register(
            ListModuleNames.Module,
            ListModuleNames.Member,
            2,
            (c, a) => ListNativeTraversal.Member(c, a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sum,
            1,
            (c, a) => ListBasicOperations.Sum(a[0])
        );
        r.RegisterCanonicalRead507(
            ListModuleNames.Module,
            ListModuleNames.KeyFind,
            3,
            (c, a) => ValueTask.FromResult(ListOperations.KeyFind(a[0], a[1], a[2]))
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.KeyMember,
            3,
            (c, a) => ListOperations.KeyMember(a[0], a[1], a[2])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.KeySearch,
            3,
            (c, a) => ListOperations.KeySearch(a[0], a[1], a[2])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Nth,
            2,
            (c, a) => ListOperations.Nth(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.NthTail,
            2,
            (c, a) => ListOperations.NthTail(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Last,
            1,
            (c, a) => ListLastExecution.Read(c, a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Split,
            2,
            (c, a) => ListOperations.Split(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sequence,
            2,
            (c, a) => ListOperations.Sequence(a[0], a[1])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sequence,
            3,
            (c, a) => ListOperations.Sequence(a[0], a[1], a[2])
        );
        r.Register(
            ListModuleNames.Module,
            ListModuleNames.Map,
            2,
            async (c, a) =>
 {
     return await ListFunctions.Run(
         c,
         ListModuleNames.Map,
         a[0],
         a[1]
     );
 }
        );
        AddVmSpawn641(
            ErlangModuleNames.Spawn,
            (c, a) =>
 {
     return ProcessSpawnRegistrations.StartCallback(c, a[0]);
 }
        );
        AddVmSpawn641(
            ErlangModuleNames.SpawnLink,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return c.Runtime.SpawnTraced(
         RuntimeVmSpawnInput641.Callback641(f, Array.Empty<Term>()),
         c,
         true,
         SequenceSpawnDescription.Callback(f, SequenceSpawnDescription.DefaultOptions(ErlangModuleNames.SpawnLink))
     ).Pid;
 }
        );
        AddVmSpawn641(
            ErlangModuleNames.SpawnMonitor,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     var spawned = c.Runtime.SpawnMonitorTraced(
         c,
         RuntimeVmSpawnInput641.Callback641(f, Array.Empty<Term>()),
         false,
         SequenceSpawnDescription.Callback(f, SequenceSpawnDescription.DefaultOptions(ErlangModuleNames.SpawnMonitor))
     );

     return Term.Tuple(spawned.Process.Pid, spawned.Reference);
 }
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Get,
            2,
            (c, a) => a[1] is MapTerm m ? m.Get(a[0]) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Size,
            1,
            (c, a) => a[0] is MapTerm m ? Term.I(m.Entries.Count) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0]))
        );
        r.Register(
            ListModuleNames.Module,
            ListModuleNames.Sort,
            2,
            (c, a) => ComparatorSort.Sort(a[0], a[1], c)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Iterator,
            1,
            (c, a) => MapIteration.Iterator(a[0])
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.Iterator,
            2,
            (c, a) => MapIteration.Iterator(a[0], a[1], c)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Next,
            1,
            (c, a) => MapIteration.Next(a[0], c.Runtime)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Get,
            3,
            (c, a) => MapOperations.RequireMap(a[1]).TryGet(a[0], out var value) ? value! : a[2]
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.New,
            0,
            (c, a) => new MapTerm([])
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Find,
            2,
            (c, a) => MapOperations.RequireMap(a[1]).TryGet(a[0], out var value) ? Term.Tuple(Term.A(MapResultAtoms.Ok), value!) : Term.A(MapResultAtoms.Error)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.IsKey,
            2,
            (c, a) => Boolean(MapOperations.RequireMap(a[1]).TryGet(a[0], out _))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Put,
            3,
            (c, a) => MapOperations.Put(a[0], a[1], a[2])
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Update,
            3,
            (c, a) => MapOperations.Put(
                a[0],
                a[1],
                a[2],
                true
            )
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Remove,
            2,
            (c, a) => MapOperations.Remove(a[0], a[1])
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Take,
            2,
            (c, a) => MapOperations.Take(a[0], a[1])
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.FromList,
            1,
            (c, a) => MapOperations.FromList(a[0])
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.FromKeys,
            2,
            (c, a) => new MapTerm(Cons.Items(a[0]).Select(key => new KeyValuePair<Term, Term>(key, a[1])))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Merge,
            2,
            (c, a) => new MapTerm(MapOperations.RequireMap(a[0]).Entries.Concat(MapOperations.RequireMap(a[1]).Entries))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Keys,
            1,
            (c, a) => Cons.From(MapIteration.Entries(MapOperations.RequireMap(a[0]), c.Runtime).Select(entry => entry.Key))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Values,
            1,
            (c, a) => Cons.From(MapIteration.Entries(MapOperations.RequireMap(a[0]), c.Runtime).Select(entry => entry.Value))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.ToList,
            1,
            (c, a) => MapOperations.ToList(a[0], c.Runtime)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.With,
            2,
            (c, a) => MapOperations.SelectKeys(a[0], a[1], true)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Without,
            2,
            (c, a) => MapOperations.SelectKeys(a[0], a[1], false)
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.IsIteratorValid,
            1,
            (c, a) => Boolean(MapOperations.IsIteratorValid(a[0], c.Runtime))
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.UpdateWith,
            3,
            (c, a) => MapOperations.UpdateWith(
                c,
                a[0],
                a[1],
                a[2]
            )
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.UpdateWith,
            4,
            (c, a) => MapOperations.UpdateWith(
                c,
                a[0],
                a[1],
                a[3],
                a[2]
            )
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Intersect,
            2,
            (c, a) => MapOperations.Intersect(a[0], a[1])
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.IntersectWith,
            3,
            (c, a) => MapOperations.Combine(
                c,
                a[0],
                a[1],
                a[2],
                true
            )
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.MergeWith,
            3,
            (c, a) => MapOperations.Combine(
                c,
                a[0],
                a[1],
                a[2],
                false
            )
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.GroupsFromList,
            2,
            (c, a) => MapOperations.GroupsFromList(c, a[0], a[1])
        );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.GroupsFromList,
            3,
            (c, a) => MapOperations.GroupsFromList(
                c,
                a[0],
                a[2],
                a[1]
            )
        );
        foreach (string operation in new[] { MapModuleNames.Filter, MapModuleNames.FilterMap, MapModuleNames.Map, MapModuleNames.ForEach })
            r.Register(
                MapModuleNames.Module,
                operation,
                2,
                (c, a) => MapOperations.Transform(
                    c,
                    operation,
                    a[0],
                    a[1]
                )
            );
        r.Register(
            MapModuleNames.Module,
            MapModuleNames.Fold,
            3,
            (c, a) => MapOperations.Transform(
                c,
                MapModuleNames.Fold,
                a[0],
                a[2],
                a[1]
            )
        );
        foreach (string operation in new[] { ListModuleNames.All, ListModuleNames.Any, ListModuleNames.Partition, ListModuleNames.FilterMap, ListModuleNames.ForEach, ListModuleNames.TakeWhile, ListModuleNames.DropWhile })
            r.Register(
                ListModuleNames.Module,
                operation,
                2,
                (c, a) => ListFunctions.Run(
                    c,
                    operation,
                    a[0],
                    a[1]
                )
            );
        foreach (string operation in new[] { ListModuleNames.FoldLeft, ListModuleNames.FoldRight, ListModuleNames.MapFoldLeft, ListModuleNames.MapFoldRight })
            r.Register(
                ListModuleNames.Module,
                operation,
                3,
                (c, a) => ListFunctions.Run(
                    c,
                    operation,
                    a[0],
                    a[2],
                    a[1]
                )
            );
    }

    private static void RegisterModules(ModuleRegistry r)
    {
        ListModuleRegistrations.Register(r);
        ProplistModuleRegistrations.Register(r);
        ProplistFamilyRegistrations.Register(r);
        QueueCoreModule.Register(r);
        OrdsetCoreModule.Register(r);
    }
}
