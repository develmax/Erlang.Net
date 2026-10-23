using System.Numerics;

namespace Erlang;

public static class CoreModules
{
    public static void Register(ModuleRegistry r)
    {
        r.InstallPreloaded(RegisterCode);
        r.SealCanonicalMatchSpecGuards419();
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
        r.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Length,
            1,
            (context, arguments) => ErlangListLength.ReadAsync(context, arguments[0])
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
        r.SealElementSourceContract();
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.TupleSize,
            1,
            (c, a) => a[0] is TupleTerm t
                ? Term.I(t.Items.Count)
                : throw ErlangSizeErrorFrames.Invalid(
                    c,
                    ErlangModuleNames.TupleSize,
                    a,
                    Term.A(ErlangErrorReasons.BadArgument)
                )
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
            (c, a) => a[0] is BitString b
                ? Term.I(b.BitLength)
                : throw ErlangSizeErrorFrames.Invalid(
                    c,
                    ErlangModuleNames.BitSize,
                    a,
                    Term.A(ErlangErrorReasons.BadArgument)
                )
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.ByteSize,
            1,
            (c, a) => a[0] is BitString b
                ? Term.I((b.BitLength + 7L) / 8)
                : throw ErlangSizeErrorFrames.Invalid(
                    c,
                    ErlangModuleNames.ByteSize,
                    a,
                    Term.A(ErlangErrorReasons.BadArgument)
                )
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
            (c, a) => Boolean(a[0] is MapTerm or HamtBitmapTerm429)
        );
        Add(
            ErlangModuleNames.Module,
            ErlangModuleNames.MapSize,
            1,
            (c, a) => a[0] is MapTerm m
                ? Term.I(m.Entries.Count)
                : throw ErlangSizeErrorFrames.Invalid(
                    c,
                    ErlangModuleNames.MapSize,
                    a,
                    Term.Tuple(Term.A(ErlangErrorReasons.BadMap), a[0])
                )
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
     if (a[1] is PortTerm port)
         c.Runtime.RegisterPort(AtomName(a[0]), port);
     else
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
        r.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Link,
            1,
            async (c, a) =>
 {
     if (a[0] is PortTerm port)
         c.Runtime.Ports.Link(c, port);
     else
         await c.Runtime.LinkGuestAsync(c, PidValue(a[0])).ConfigureAwait(false);

     return Term.A(ErlangBooleanAtoms.True);
 }
        );
        r.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Unlink,
            1,
            async (c, a) =>
 {
     if (a[0] is PortTerm port)
         c.Runtime.Ports.Unlink(c, port);
     else
         await c.Runtime.UnlinkGuestAsync(c, PidValue(a[0])).ConfigureAwait(false);

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
        r.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.ProcessFlag,
            2,
            async (c, a) =>
 {
     if (RuntimeNodeMonitorRegistrations.TryChangeSelf(c, a, out Term nodeMonitorResult))
         return nodeMonitorResult;
     if (a[0] is Atom { Name: ProcessSavedCallNames.Flag })
         return ProcessSavedCallRegistrations.ChangeSelf(c, a);
     if (a[0] is Atom { Name: ProcessInfoLogicalNames.ErrorHandler })
         return c.Runtime.ChangeErrorHandler(c, a[1]);
     if (a[0] is Atom { Name: ProcessSchedulerNames.Binding })
         return await c.Runtime.ChangeSchedulerBinding(c, a[1]);
     if (a[0] is Atom { Name: ProcessMessageQueueDataNames411.Flag })
         return ProcessMessageQueueData411.ChangeSelf(c, a[1]);
     if (a[0] is Atom { Name: ProcessFullSweepNames.Flag })
         return ProcessFullSweepAfter.SetSelf(c, a[1]);
     if (a[0] is Atom { Name: ProcessPriorityNames.Flag })
         return c.Runtime.ChangeSchedulingPriority(c, a[1]);
     if (a[0] is Atom { Name: ProcessAsyncDistributionNames.Flag })
         return ProcessAsyncDistributionRegistration.ChangeSelf(c, a[1]);
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
        r.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Exit,
            2,
            async (c, a) =>
 {
     await c.Runtime.SendGuestExitSignalAsync(
         c,
         a[0],
         a[1],
         true,
         false
     ).ConfigureAwait(false);

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
            (c, a) => ErlangInfallibleBifValues.Get(c, a[0])
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
        RuntimeManagedLibraries456.Register(r, RuntimeManagedLibraryStage456.Collections);
        ErlangBasicRegistrations.Register(r);
        ErlangDataRegistrations.Register(r);
        BinaryModule.Register(r);
        ErlangAtomRegistrations.Register(r);
        ErlangFloatRegistrations.Register(r);
        ErlangClockRegistrations.Register(r);
        ErlangCalendarRegistrations.Register(r);
        ErlangTimerRegistrations.Register(r);
        ErlangMd5Registrations.Register(r);
        ErlangRaiseRegistrations.Register(r);
        ErlangProcessQueryRegistrations.Register(r);
        ProcessInfoRegistrations.Register(r);
        ProcessDisplayRegistrations.Register(r);
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
        TermDisplayRegistrations.Register(r);
        RuntimeNodeQueryRegistrations.Register(r);
        NodeSpawnRegistrations.Register(r);
        ErlangCookieRegistrations.Register(r);
        RuntimeNodeControlRegistrations.Register(r);
        RuntimeSetNodeRegistrations.Register(r);
        DistributionRegistrations.Register(r);
        ProcessSavedCallRegistrations.Register(r);
        PortRegistrations.Register(r);
        ProcessCodeRegistrations.Register(r);
        ProcessGarbageCollectionRegistrations.Register(r);
        GarbageCollectionResultRegistrations.Register(r);
        ErlangFunctionRegistrations.Register(r);
        ErlangFunctionInfoRegistrations.Register(r);
        ErlangEtfRegistrations.Register(r);
        ErlangHashRegistrations.Register(r);
        ProcessAliasRegistrations.Register(r);
        ProcessMonitorRegistrations.Register(r);
        ProcessSpawnRegistrations.Register(r);
        ProcessSpawnOptionRegistrations.Register(r);
        ProcessSpawnRequestRegistrations.Register(r);
        ProcessHibernateRegistrations.Register(r);
        ProcessDelayRegistrations.Register(r);
        CpuTopologyFormatting.Register(r);
        CpuTopologyStateRegistrations.Register(r);
        ProcessExitRegistrations.Register(r);
        RecordPredicateRegistrations.Register(r);
        MapNativeBridge.Register(r);
        DistributionPendingNativeBridge.Register(r);
        CollectionMathBridge.Register(r);
        StringFloatNative.Register(r);
        NativeRecordRegistrations.Register(r);
        DynamicTraceRegistrations.Register(r);
        SequenceTraceRegistrations.Register(r);
        SystemMonitorRegistrations.Register(r);
        SystemProfileRegistrations.Register(r);
        ProcessTraceRegistrations.Register(r);
        MatchSpecRegistrations.Register(r);
        RuntimeStatisticsRegistrations.Register(r);
        ErlangErrorRegistrations.Register(r);
        r.SealCanonicalNoReturnContracts();
        ErlangLoadNifRegistrations.Register(r);
        RuntimeManagedLibraries456.Register(r, RuntimeManagedLibraryStage456.Modules);
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
        HaltRegistrations.Register(r);
        BeamLoaderRegistrations.Register(r);
        AllocatorQueryRegistrations.Register(r);
        ClrMemoryRegistrations.Register(r);
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
