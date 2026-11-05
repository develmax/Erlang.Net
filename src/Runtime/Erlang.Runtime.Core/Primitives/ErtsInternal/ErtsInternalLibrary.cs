// Derived from erts/preloaded/src/erts_internal.erl, OTP 29.1.1, Apache-2.0.
// Copyright Ericsson AB 2012-2025. Retained original source contains the notices.
namespace Erlang;

internal static partial class ErtsInternalLibrary
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.InstallPreloaded(target =>
        {
            foreach (var export in ErtsInternalContract.LibraryExports)
            {
                string function = export.Function;
                target.RegisterReviewedManagedLibrary789(
                    ErtsInternalContract.Module,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        });
    }

    internal static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> args)
    {
        using var tail = context.EnterSourceTailCall(ErtsInternalContract.Module, function);
        using var frame = context.EnterGeneratedFunction(
            ErtsInternalContract.Module,
            function,
            args.Count,
            Location(function == ErtsInternalFunctions.DynamicNodeName && args.Count == 1
                ? ErtsInternalSource.DynamicSetter : ErtsInternalContract.Line(function)),
            new object()
        );
        try
        {
            return await Execute(context, function, args);
        }
        catch (ErlangException error)
        {
            throw error.WithCapturedStack(context.CaptureErrorStack(
                error.Reason is Atom { Name: ErlangErrorReasons.FunctionClause } ? Cons.From(args) : Term.I(args.Count),
                null
            ));
        }
    }

    private static Term Location(int line) => Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(ErtsInternalContract.SourceFile)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );

    private static IDisposable At(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            ErtsInternalContract.Module,
            function,
            arity,
            Location(line),
            new object(),
            tail: true
        );

    private static ErlangException ErrorAt(
        ProcessContext context,
        string function,
        int arity,
        int line,
        Term reason
    )
    {
        using var location = At(
            context,
            function,
            arity,
            line
        );

        return new ErlangException(reason).WithCapturedStack(context.CaptureErrorStack(Term.I(arity), null));
    }

    private static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] args
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            args
        );

    private static ValueTask<Term> Internal(ProcessContext context, string function, params Term[] args)
        => Call(
            context,
            ErtsInternalContract.Module,
            function,
            args
        );

    private static ValueTask<Term> Bif(ProcessContext context, string function, params Term[] args)
        => Call(
            context,
            ErlangModuleNames.Module,
            function,
            args
        );

    private static bool Is(Term term, string name) => term is Atom atom && atom.Name == name;

    private static ErlangException Clause() => new(ErlangErrorReasons.FunctionClause);

    private static async ValueTask<Term> Receive(ProcessContext context, Func<Term, bool> predicate)
        => (await context.ReceiveAsync(message => predicate(message) ? message : null))!;

    private static bool Tagged(Term term, Term reference, int size)
        => term is TupleTerm tuple && tuple.Items.Count == size && tuple.Items[0].Equals(reference);

    private static async ValueTask<Term> Reply(ProcessContext context, Term reference)
        => ((TupleTerm)await Receive(context, message => Tagged(message, reference, 2))).Items[1];

    private static async ValueTask<Term> Execute(ProcessContext context, string function, IReadOnlyList<Term> args)
    {
        Term a = args.Count == 0 ? Nil.Value : args[0];
        switch (function)
        {
            case ErtsInternalFunctions.AwaitPortSendResult:
                return Is(await Reply(context, a), ErtsInternalProtocol.False) ? args[1] : args[2];
            case ErtsInternalFunctions.AwaitResult:
                if (a is not ReferenceTerm)
                {
                    throw Clause();
                }

                return await Reply(context, a);
            case ErtsInternalFunctions.GatherIoBytes:
                return await GatherIo(context, a, args[1]);
            case ErtsInternalFunctions.GatherSystemCheckResult:
                if (a is not ReferenceTerm)
                {
                    throw Clause();
                }
                await GatherSignals(context, a, await Schedulers(context));

                return Term.A(ErtsInternalProtocol.Ok);
            case ErtsInternalFunctions.WaitReleaseLiteralAreaSwitch:
                await Receive(context, message => message.Equals(a));

                return await Call(context, ErtsInternalProtocol.LiteralCollector, ErtsInternalProtocol.ReleaseAreaSwitch);
            case ErtsInternalFunctions.FlushMonitorMessages:
                return await Flush(
                    context,
                    a,
                    args[1],
                    args[2]
                );
            case ErtsInternalFunctions.FlushMonitorMessagesRefopt:
                Term reference = await Bif(context, ErtsInternalProtocol.MakeRef);
                await Flush(
                    context,
                    reference,
                    Term.A(ErtsInternalProtocol.True),
                    Term.A(ErtsInternalProtocol.Ok)
                );

                return await Flush(
                    context,
                    reference,
                    Term.A(ErtsInternalProtocol.True),
                    Term.A(ErtsInternalProtocol.Ok)
                );
            case ErtsInternalFunctions.McIterator:
                return await MapIterator(context, a);
            case ErtsInternalFunctions.McRefill:
                if (a is not Cons refill)
                {
                    throw Clause();
                }

                using (At(
                    context,
                    function,
                    args.Count,
                    ErtsInternalSource.RefillCall
                ))
                {
                    return await Internal(
                        context,
                        ErtsInternalFunctions.MapNext,
                        refill.Head,
                        refill.Tail,
                        Term.A(ErtsInternalProtocol.Iterator)
                    );
                }
            case ErtsInternalFunctions.CheckProcessCode:
                return await CheckProcessCode(
                    context,
                    a,
                    args[1],
                    args[2]
                );
            case ErtsInternalFunctions.IsProcessAlive:
                Term aliveReference = await Bif(context, ErtsInternalProtocol.MakeRef);
                await Internal(
                    context,
                    function,
                    a,
                    aliveReference
                );

                return await Reply(context, aliveReference);
            case ErtsInternalFunctions.AwaitMicrostateAccountingModifications:
                await Microstates(context, a, args[2]);

                return args[1];
            case ErtsInternalFunctions.GatherMicrostateAccountingResult:
                return await Microstates(context, a, args[1]);
            case ErtsInternalFunctions.AwaitSchedWallTimeModifications:
                await GatherSignals(context, a, await Schedulers(context));

                return args[1];
            case ErtsInternalFunctions.GatherSchedWallTimeResult:
                if (a is not ReferenceTerm)
                {
                    throw Clause();
                }

                return await WallTime(context, a, await Schedulers(context));
            case ErtsInternalFunctions.SystemFlagSchedulerWallTime:
                return await Call(
                    context,
                    ErtsInternalProtocol.KernelRefc,
                    ErtsInternalFunctions.SchedulerWallTime,
                    a
                );
            case ErtsInternalFunctions.DistCtrlPutData:
                return await DistributionData(context, a, args[1]);
            case ErtsInternalFunctions.DynamicNodeName:
                return await DynamicNode(context, args);
            case ErtsInternalFunctions.TermToString:
                return await Internal(
                    context,
                    function,
                    a,
                    Term.A(ErtsInternalProtocol.Undefined)
                );
            case ErtsInternalFunctions.SpawnInit:
                {
                    if (a is not TupleTerm { Items.Count: 3 } mfa)
                    {
                        throw Clause();
                    }
                    using var spawnLocation = At(
                        context,
                        function,
                        args.Count,
                        ErtsInternalSource.SpawnApply
                    );

                    return await Bif(
                        context,
                        ErtsInternalProtocol.Apply,
                        mfa.Items[0],
                        mfa.Items[1],
                        mfa.Items[2]
                    );
                }
            case ErtsInternalFunctions.DistSpawnInit:
                return await DistributedSpawn(context, a);
            case ErtsInternalFunctions.Crasher:
                return await Crasher(context, args);
            case ErtsInternalFunctions.GetInternalStateBlocked:
                {
                    using var blockLocation = At(
                        context,
                        function,
                        args.Count,
                        ErtsInternalSource.InternalStateBlock
                    );
                    await Bif(
                        context,
                        ErtsInternalProtocol.SystemFlag,
                        Term.A(ErtsInternalProtocol.MultiScheduling),
                        Term.A(ErtsInternalProtocol.Block)
                    );
                    try
                    {
                        using var readLocation = At(
                            context,
                            function,
                            args.Count,
                            ErtsInternalSource.InternalStateRead
                        );

                        return await Call(
                            context,
                            ErtsInternalProtocol.Debug,
                            ErtsInternalProtocol.GetInternalState,
                            Term.Tuple(a, Term.A(ErtsInternalProtocol.Blocked))
                        );
                    }
                    finally
                    {
                        using var unblockLocation = At(
                            context,
                            function,
                            args.Count,
                            ErtsInternalSource.InternalStateUnblock
                        );
                        await Bif(
                            context,
                            ErtsInternalProtocol.SystemFlag,
                            Term.A(ErtsInternalProtocol.MultiScheduling),
                            Term.A(ErtsInternalProtocol.Unblock)
                        );
                    }
                }
            case ErtsInternalFunctions.EtsInfoBinary:
                return await EtsBinary(context, a);
            case ErtsInternalFunctions.SetCodeAndLiteralCleanerPrio:
                return await CleanerPriority(context, a);
            case ErtsInternalFunctions.Breakpoint:
                return await Breakpoint(context, args);
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static ValueTask<Term> Schedulers(ProcessContext context)
        => Bif(context, ErtsInternalProtocol.SystemInfo, Term.A(ErtsInternalProtocol.Schedulers));

    private static async ValueTask GatherSignals(ProcessContext context, Term reference, Term count)
    {
        while (!count.Equals(Term.I(0)))
        {
            await Receive(context, message => message.Equals(reference));
            count = await Bif(
                context,
                ErtsInternalProtocol.Subtract,
                count,
                Term.I(1)
            );
        }
    }

    private static async ValueTask<Term> GatherIo(ProcessContext context, Term reference, Term count)
    {
        if (reference is not ReferenceTerm || count is not Integer integer || integer.Value <= 0)
        {
            throw Clause();
        }
        Term input = Term.I(0);
        Term output = Term.I(0);
        while (!count.Equals(Term.I(0)))
        {
            var message = (TupleTerm)await Receive(context, message => Tagged(message, reference, 4));
            input = await Bif(
                context,
                ErtsInternalProtocol.Add,
                input,
                message.Items[2]
            );
            output = await Bif(
                context,
                ErtsInternalProtocol.Add,
                output,
                message.Items[3]
            );
            count = await Bif(
                context,
                ErtsInternalProtocol.Subtract,
                count,
                Term.I(1)
            );
        }

        return Term.Tuple(Term.Tuple(Term.A(ErtsInternalProtocol.Input), input), Term.Tuple(Term.A(ErtsInternalProtocol.Output), output));
    }

    private static async ValueTask<Term> Flush(
        ProcessContext context,
        Term reference,
        Term multi,
        Term result
    )
    {
        if (reference is not ReferenceTerm)
        {
            throw Clause();
        }
        while (await context.ReceiveAsync(message =>
            message is TupleTerm { Items.Count: 5 } tuple && tuple.Items[1].Equals(reference) ? message : null,
            TimeSpan.Zero) is not null)
        {
            if (Is(multi, ErtsInternalProtocol.False))
            {
                break;
            }
        }

        return result;
    }

    private static async ValueTask<Term> Microstates(ProcessContext context, Term reference, Term count)
    {
        var results = new List<Term>();
        while (!count.Equals(Term.I(0)))
        {
            Term message = await Receive(context, message => message.Equals(reference) || Tagged(message, reference, 2));
            if (!message.Equals(reference))
            {
                results.Add(((TupleTerm)message).Items[1]);
            }
            count = await Bif(
                context,
                ErtsInternalProtocol.Subtract,
                count,
                Term.I(1)
            );
        }

        return Cons.From(results);
    }

    private static async ValueTask<Term> WallTime(ProcessContext context, Term reference, Term count)
    {
        Term accumulator = Nil.Value;
        while (!count.Equals(Term.I(0)))
        {
            Term value = await Reply(context, reference);
            if (!Is(accumulator, ErtsInternalProtocol.Undefined))
            {
                accumulator = Is(value, ErtsInternalProtocol.Undefined) ? value
                    : value is Cons or Nil ? await Bif(
                        context,
                        ErtsInternalProtocol.Append,
                        accumulator,
                        value
                    )
                    : new Cons(value, accumulator);
            }
            count = await Bif(
                context,
                ErtsInternalProtocol.Subtract,
                count,
                Term.I(1)
            );
        }

        return accumulator;
    }
}
