using System.Numerics;

namespace Erlang;

public static class CoreModules
{
    public static void Register(ModuleRegistry r)
    {
        r.InstallPreloaded(RegisterCode);
    }

    private static void RegisterCode(ModuleRegistry r)
    {
        void Add(
            string module,
            string name,
            int arity,
            Func<ProcessContext, IReadOnlyList<Term>, Term> f
        ) => r.Register(
            module,
            name,
            arity,
            (c, a) => ValueTask.FromResult(f(c, a))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Self,
            0,
            (c, a) => c.Self
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Length,
            1,
            (c, a) => Term.I(Cons.Items(a[0]).LongCount())
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Head,
            1,
            (c, a) => a[0] is Cons l ? l.Head : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Tail,
            1,
            (c, a) => a[0] is Cons l ? l.Tail : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Element,
            2,
            (c, a) => a[0] is Integer i && a[1] is TupleTerm t && i.Value > 0 && i.Value <= t.Items.Count ? t.Items[(int)i.Value - 1] : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.TupleSize,
            1,
            (c, a) => a[0] is TupleTerm t ? Term.I(t.Items.Count) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsAtom,
            1,
            (c, a) => Boolean(a[0] is Atom)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsInteger,
            1,
            (c, a) => Boolean(a[0] is Integer)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsFloat,
            1,
            (c, a) => Boolean(a[0] is FloatTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsNumber,
            1,
            (c, a) => Boolean(a[0] is Integer or FloatTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsTuple,
            1,
            (c, a) => Boolean(a[0] is TupleTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsBinary,
            1,
            (c, a) => Boolean(a[0] is BitString b && b.IsBinary)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsBitString,
            1,
            (c, a) => Boolean(a[0] is BitString)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.BitSize,
            1,
            (c, a) => a[0] is BitString b ? Term.I(b.BitLength) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.ByteSize,
            1,
            (c, a) => a[0] is BitString b ? Term.I((b.BitLength + 7L) / 8) : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsList,
            1,
            (c, a) => Boolean(a[0] is Cons or Nil)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsPid,
            1,
            (c, a) => Boolean(a[0] is Pid)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsMap,
            1,
            (c, a) => Boolean(a[0] is MapTerm)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MapSize,
            1,
            (c, a) => a[0] is MapTerm m ? Term.I(m.Entries.Count) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MapGet,
            2,
            (c, a) => a[1] is MapTerm m ? m.Get(a[0]) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.IsMapKey,
            2,
            (c, a) => a[1] is MapTerm m ? Boolean(m.TryGet(a[0], out _)) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[1]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MakeReference,
            0,
            (c, a) => c.Runtime.MakeReference()
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Register,
            2,
            (c, a) =>
 {
     c.Runtime.Register(AtomName(a[0]), PidValue(a[1]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.WhereIs,
            1,
            (c, a) => c.Runtime.WhereIs(AtomName(a[0]))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Unregister,
            1,
            (c, a) =>
 {
     c.Runtime.Unregister(AtomName(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Link,
            1,
            (c, a) =>
 {
     c.Runtime.Link(c, PidValue(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Unlink,
            1,
            (c, a) =>
 {
     c.Runtime.Unlink(c, PidValue(a[0]));

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Monitor,
            2,
            (c, a) =>
            {
                string kind = AtomName(a[0]);

                return c.Runtime.MonitorSource(
                    c,
                    kind,
                    a[1],
                    ProcessMonitorOptions.ForKind(kind)
                );
            }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Demonitor,
            1,
            (c, a) =>
 {
     if (a[0] is not ReferenceTerm reference)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return Boolean(c.Runtime.Demonitor(c, reference));
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.ProcessFlag,
            2,
            (c, a) =>
 {
     if (AtomName(a[0]) != ProcessFlagNames.TrapExit)
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     bool previous = c.TrapExits;
     c.TrapExits = Bool(a[1]);

     return Boolean(previous);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Exit,
            1,
            (c, a) => throw new ErlangException(a[0], ErlangExceptionClasses.Exit)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Exit,
            2,
            (c, a) =>
 {
     c.Runtime.SendExitSignal(
         c,
         a[0],
         a[1],
         true,
         false
     );

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Error,
            1,
            (c, a) => throw new ErlangException(a[0])
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Throw,
            1,
            (c, a) => throw new ErlangException(a[0], ErlangExceptionClasses.Throw)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Put,
            2,
            (c, a) =>
 {
     var previous = c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined));
     c.Dictionary[a[0]] = a[1];

     return previous;
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Get,
            1,
            (c, a) => c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined))
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Erase,
            1,
            (c, a) =>
 {
     var previous = c.Dictionary.GetValueOrDefault(a[0], Term.A(ProcessDictionaryAtoms.Undefined));
     c.Dictionary.Remove(a[0]);

     return previous;
 }
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            1,
            (c, a) => ListOperations.Reverse(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            2,
            (c, a) => ListOperations.Reverse(a[0], a[1])
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
        Add(
            ListModuleNames.Module,
            ListModuleNames.Member,
            2,
            (c, a) => Boolean(Cons.Items(a[1]).Any(x => x.Equals(a[0])))
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.Sum,
            1,
            (c, a) => ListBasicOperations.Sum(a[0])
        );
        Add(
            ListModuleNames.Module,
            ListModuleNames.KeyFind,
            3,
            (c, a) => ListOperations.KeyFind(a[0], a[1], a[2])
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
            (c, a) => ListOperations.Last(a[0])
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
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.Spawn,
            1,
            (c, a) =>
 {
     return ProcessSpawnRegistrations.StartCallback(c, a[0]);
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.SpawnLink,
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);

     return c.Runtime.Spawn(child => f.Invoke(child, []), c, true).Pid;
 }
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.SpawnMonitor,
            1,
            (c, a) =>
 {
     if (a[0] is not FunctionTerm { Arity: 0 } f)
         throw new ErlangException(ErlangErrorReasons.BadArgument);
     var spawned = c.Runtime.SpawnMonitor(c, child => f.Invoke(child, []));

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
            (c, a) => MapIteration.Next(a[0])
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
            (c, a) => Cons.From(MapOperations.RequireMap(a[0]).Entries.Select(entry => entry.Key))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.Values,
            1,
            (c, a) => Cons.From(MapOperations.RequireMap(a[0]).Entries.Select(entry => entry.Value))
        );
        Add(
            MapModuleNames.Module,
            MapModuleNames.ToList,
            1,
            (c, a) => MapOperations.ToList(a[0])
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
            (c, a) => Boolean(MapOperations.IsIteratorValid(a[0]))
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
        ErlangBasicRegistrations.Register(r);
        ErlangDataRegistrations.Register(r);
        ErlangAtomRegistrations.Register(r);
        ErlangFloatRegistrations.Register(r);
        ErlangClockRegistrations.Register(r);
        ErlangCalendarRegistrations.Register(r);
        ErlangTimerRegistrations.Register(r);
        ErlangMd5Registrations.Register(r);
        ErlangRaiseRegistrations.Register(r);
        ErlangProcessQueryRegistrations.Register(r);
        ProcessInfoRegistrations.Register(r);
        ProcessIdentifierTextRegistrations.Register(r);
        PacketDecodeRegistrations.Register(r);
        ModuleLoadingRegistrations.Register(r);
        ProcessTableRegistrations.Register(r);
        ReferenceTextRegistrations.Register(r);
        PreloadedModuleRegistrations.Register(r);
        ModuleInformationRegistrations.Register(r);
        ModuleCodeLifecycleRegistrations.Register(r);
        CodePurgeRegistrations.Register(r);
        StandardStreamRegistrations.Register(r);
        ProcessSuspensionRegistrations.Register(r);
        ProcessCodeRegistrations.Register(r);
        ErlangFunctionRegistrations.Register(r);
        ErlangFunctionInfoRegistrations.Register(r);
        ErlangEtfRegistrations.Register(r);
        ErlangHashRegistrations.Register(r);
        ProcessAliasRegistrations.Register(r);
        ProcessMonitorRegistrations.Register(r);
        ProcessSpawnRegistrations.Register(r);
        ProcessExitRegistrations.Register(r);
        RecordPredicateRegistrations.Register(r);
        DynamicTraceRegistrations.Register(r);
        ErlangErrorRegistrations.Register(r);
        ListModuleRegistrations.Register(r);
        foreach (string operation in new[] { TimerModuleNames.SendAfter, TimerModuleNames.StartTimer })
            r.Register(
                ErlangModuleNames.Module,
                operation,
                3,
                async (context, arguments) =>
            {
                var reference = (ReferenceTerm)await context.Runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangModuleNames.MakeReference);

                return context.Runtime.Timers.Start(
                    reference,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    operation == TimerModuleNames.StartTimer
                );
            }
            );
        Add(
            ErlangModuleNames.Module,
            TimerModuleNames.ReadTimer,
            1,
            (context, arguments) => context.Runtime.Timers.Access(arguments[0], false)
        );
        Add(
            ErlangModuleNames.Module,
            TimerModuleNames.CancelTimer,
            1,
            (context, arguments) => context.Runtime.Timers.Access(arguments[0], true)
        );
        IoFormatRequests.Register(r);
    }


    public static string AtomName(Term t) => t is Atom a ? a.Name : throw new ErlangException(ErlangErrorReasons.BadArgument);

    public static Pid PidValue(Term t) => t is Pid p ? p : throw new ErlangException(ErlangErrorReasons.BadArgument);

    public static Term Boolean(bool value) => Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False);

    public static bool Bool(Term t) => t is Atom a && a.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False ? a.Name == ErlangBooleanAtoms.True : throw new ErlangException(ErlangErrorReasons.BadArgument);

    public static Term Arithmetic(string op, Term a, Term b)
    {
        if (a is Integer x && b is Integer y)
        {
            if (op == ErlangOperators.Divide && y.Value == 0 || op is ErlangOperators.IntegerDivide or ErlangOperators.Remainder && y.Value == 0)
                throw new ErlangException(ErlangErrorReasons.BadArithmetic);
            if (op == ErlangOperators.Divide)
            {
                if (!x.TryToDouble(out double numerator) || !y.TryToDouble(out double denominator))
                    throw new ErlangException(ErlangErrorReasons.BadArithmetic);

                return new FloatTerm(numerator / denominator);
            }

            return op switch
            {
                ErlangOperators.Plus => new Integer(x.Value + y.Value),
                ErlangOperators.Minus => new Integer(x.Value - y.Value),
                ErlangOperators.Multiply => new Integer(x.Value * y.Value),
                ErlangOperators.IntegerDivide => new Integer(x.Value / y.Value),
                ErlangOperators.Remainder => new Integer(x.Value % y.Value),
                ErlangOperators.BitwiseAnd => new Integer(x.Value & y.Value),
                ErlangOperators.BitwiseOr => new Integer(x.Value | y.Value),
                ErlangOperators.BitwiseXor => new Integer(x.Value ^ y.Value),
                ErlangOperators.ShiftLeft => Shift(x.Value, y.Value),
                ErlangOperators.ShiftRight => Shift(x.Value, -y.Value),
                _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
            };
        }
        double left = a switch
        {
            Integer i when i.TryToDouble(out double converted) => converted,
            FloatTerm f => f.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        };
        double right = b switch
        {
            Integer i when i.TryToDouble(out double converted) => converted,
            FloatTerm f => f.Value,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        };

        return new FloatTerm(op switch
        {
            ErlangOperators.Plus => left + right,
            ErlangOperators.Minus => left - right,
            ErlangOperators.Multiply => left * right,
            ErlangOperators.Divide => left / right,
            _ => throw new ErlangException(ErlangErrorReasons.BadArithmetic)
        });
    }

    private static Term Shift(BigInteger value, BigInteger count)
    {
        if (value.IsZero || count.IsZero)
            return new Integer(value);
        if (count.Sign < 0)
        {
            var magnitude = -count;
            if (magnitude >= value.GetBitLength())
                return Term.I(value.Sign < 0 ? -1 : 0);

            return new Integer(value >> (int)magnitude);
        }
        if (count > IntegerShiftLimits.MaximumResultBits || count + value.GetBitLength() > IntegerShiftLimits.MaximumResultBits)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return new Integer(value << (int)count);
    }
}
