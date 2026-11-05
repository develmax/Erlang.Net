// Derived from erts/preloaded/src/erts_internal.erl, OTP 29.1.1, Apache-2.0.
namespace Erlang;

internal static partial class ErtsInternalLibrary
{
    private static async ValueTask<Term> MapIterator(ProcessContext context, Term value)
    {
        if (value is MapTerm)
        {
            return await Internal(
                context,
                ErtsInternalFunctions.MapNext,
                Term.I(0),
                value,
                Term.A(ErtsInternalProtocol.Iterator)
            );
        }
        if (value is Cons candidate)
        {
            try
            {
                return await Internal(
                    context,
                    ErtsInternalFunctions.MapNext,
                    candidate.Head,
                    candidate.Tail,
                    Term.A(ErtsInternalProtocol.Iterator)
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && Is(error.Reason, ErlangErrorReasons.BadArgument))
            {
                return Nil.Value;
            }
        }
        Term iterator = value;
        while (iterator is TupleTerm { Items.Count: 3 } tuple)
        {
            iterator = tuple.Items[2];
            await context.ExecutionCheckpoint();
        }
        if (Is(iterator, ErtsInternalProtocol.None))
        {
            return value;
        }
        if (iterator is Cons tail)
        {
            try
            {
                await Internal(
                    context,
                    ErtsInternalFunctions.MapNext,
                    tail.Head,
                    tail.Tail,
                    Term.A(ErtsInternalProtocol.Iterator)
                );

                return value;
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && Is(error.Reason, ErlangErrorReasons.BadArgument))
            {
                return Nil.Value;
            }
        }

        return Nil.Value;
    }

    private static async ValueTask<Term> CheckProcessCode(
        ProcessContext context,
        Term pid,
        Term module,
        Term options
    )
    {
        using var caller = At(
            context,
            ErtsInternalFunctions.CheckProcessCode,
            3,
            ErtsInternalSource.CheckOptionsCall
        );
        Term mode = Term.A(ErtsInternalProtocol.Sync);
        Term remaining = options;
        while (remaining is Cons cell)
        {
            if (cell.Head is TupleTerm { Items.Count: 2 } option)
            {
                if (Is(option.Items[0], ErtsInternalProtocol.Async))
                {
                    mode = option;
                }
                else if (!(Is(option.Items[0], ErtsInternalProtocol.AllowGc)
                    && (Is(option.Items[1], ErtsInternalProtocol.True) || Is(option.Items[1], ErtsInternalProtocol.False))))
                {
                    throw BadOptions(context);
                }
            }
            else
            {
                throw BadOptions(context);
            }
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
        {
            throw BadOptions(context);
        }
        if (mode is TupleTerm asynchronous)
        {
            using var location = At(
                context,
                ErtsInternalFunctions.CheckProcessCode,
                3,
                ErtsInternalSource.CheckAsyncCall
            );
            await Internal(
                context,
                ErtsInternalFunctions.RequestSystemTask,
                pid,
                Term.A(ErtsInternalProtocol.Inherit),
                Term.Tuple(Term.A(ErtsInternalFunctions.CheckProcessCode), asynchronous.Items[1], module)
            );

            return Term.A(ErtsInternalProtocol.Async);
        }
        if (pid.Equals(context.Self))
        {
            return await Internal(context, ErtsInternalFunctions.CheckProcessCode, module);
        }
        Term reference = await Bif(context, ErtsInternalProtocol.MakeRef);
        using var syncLocation = At(
            context,
            ErtsInternalFunctions.CheckProcessCode,
            3,
            ErtsInternalSource.CheckSyncCall
        );
        await Internal(
            context,
            ErtsInternalFunctions.RequestSystemTask,
            pid,
            Term.A(ErtsInternalProtocol.Inherit),
            Term.Tuple(Term.A(ErtsInternalFunctions.CheckProcessCode), reference, module)
        );
        var response = (TupleTerm)await Receive(
            context,
            message => message is TupleTerm { Items.Count: 3 } tuple
            && Is(tuple.Items[0], ErtsInternalFunctions.CheckProcessCode) && tuple.Items[1].Equals(reference)
        );

        return response.Items[2];
    }

    private static ErlangException BadOptions(ProcessContext context)
    {
        using var frame = context.EnterGeneratedFunction(
            ErtsInternalContract.Module,
            ErtsInternalSource.CheckOptions,
            2,
            Location(ErtsInternalSource.BadOptions),
            new object()
        );

        return new ErlangException(ErtsInternalProtocol.BadOption)
            .WithCapturedStack(context.CaptureErrorStack(Term.I(2), null));
    }

    private static async ValueTask<Term> DynamicNode(ProcessContext context, IReadOnlyList<Term> args)
    {
        Term key = Term.Tuple(Term.A(ErtsInternalContract.Module), Term.A(ErtsInternalFunctions.DynamicNodeName));
        if (args.Count == 0)
        {
            Term value = await Call(
                context,
                ErtsInternalProtocol.PersistentTerm,
                ErtsInternalProtocol.Get,
                key,
                Term.A(ErtsInternalProtocol.False)
            );

            return Term.A(Is(value, ErtsInternalProtocol.False) ? ErtsInternalProtocol.False : ErtsInternalProtocol.True);
        }
        if (Is(args[0], ErtsInternalProtocol.True))
        {
            return await Call(
                context,
                ErtsInternalProtocol.PersistentTerm,
                ErtsInternalProtocol.Put,
                key,
                args[0]
            );
        }
        if (!Is(args[0], ErtsInternalProtocol.False))
        {
            throw Clause();
        }
        if (Is(await DynamicNode(context, []), ErtsInternalProtocol.True))
        {
            await Call(
                context,
                ErtsInternalProtocol.PersistentTerm,
                ErtsInternalProtocol.Erase,
                key
            );
        }

        return Term.A(ErtsInternalProtocol.Ok);
    }

    private static async ValueTask<Term> DistributedSpawn(ProcessContext context, Term value)
    {
        if (value is not TupleTerm { Items.Count: 3 } mfa)
        {
            throw ErrorAt(
                context,
                ErtsInternalFunctions.DistSpawnInit,
                1,
                ErtsInternalSource.DistributedSpawnMatch,
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value)
            );
        }
        Term args = await Receive(context, _ => true);
        if (Is(args, ErtsInternalFunctions.DistSpawnInit))
        {
            return await Bif(context, ErtsInternalProtocol.Exit, Term.A(ErtsInternalProtocol.ArgumentDecodeFailure));
        }
        await Receive(context, message => Is(message, ErtsInternalFunctions.DistSpawnInit));
        using var applyLocation = At(
            context,
            ErtsInternalFunctions.DistSpawnInit,
            1,
            ErtsInternalSource.DistributedSpawnApply
        );

        return await Bif(
            context,
            ErtsInternalProtocol.Apply,
            mfa.Items[0],
            mfa.Items[1],
            args
        );
    }

    private static async ValueTask<Term> Crasher(ProcessContext context, IReadOnlyList<Term> args)
    {
        bool empty = args[4] is Nil;
        Term values = empty ? Term.List(
            args[1],
            args[2],
            args[3],
            args[0]
        )
            : Term.List(
                args[1],
                args[2],
                args[3],
                args[4],
                args[0]
            );
        await Call(
            context,
            ErtsInternalProtocol.ErrorLogger,
            ErtsInternalProtocol.WarningMessage,
            Term.String(empty ? ErtsInternalProtocol.SpawnWarning : ErtsInternalProtocol.SpawnOptionsWarning),
            values
        );

        return await Bif(context, ErtsInternalProtocol.Exit, args[5]);
    }

    private static async ValueTask<Term> CleanerPriority(ProcessContext context, Term priority)
    {
        Term first = await Bif(context, ErtsInternalProtocol.MakeRef);
        await Bif(
            context,
            ErtsInternalProtocol.Send,
            Term.A(ErtsInternalProtocol.CodePurger),
            Term.Tuple(
                Term.A(ErtsInternalProtocol.ChangePriority),
                context.Self,
                first,
                priority
            )
        );
        Term second = await Bif(context, ErtsInternalProtocol.MakeRef);
        Term processes = await Bif(context, ErtsInternalProtocol.Processes);
        Term? collector = null;
        foreach (Term pid in Cons.Items(processes))
        {
            Term info = await Bif(
                context,
                ErtsInternalProtocol.ProcessInfo,
                pid,
                Term.A(ErtsInternalProtocol.InitialCall)
            );
            if (info.Equals(Term.Tuple(
                Term.A(ErtsInternalProtocol.InitialCall),
                Term.Tuple(Term.A(ErtsInternalProtocol.LiteralCollector), Term.A(ErtsInternalProtocol.Start), Term.I(0))
            )))
            {
                collector = pid;
                break;
            }
        }
        if (collector is null)
        {
            throw Clause();
        }
        await Bif(
            context,
            ErtsInternalProtocol.Send,
            collector,
            Term.Tuple(
                Term.A(ErtsInternalProtocol.ChangePriority),
                context.Self,
                second,
                priority
            )
        );
        Term oldPurger = await Reply(context, first);
        Term oldCollector = await Reply(context, second);

        return Term.List(
            Term.Tuple(Term.A(ErtsInternalProtocol.CodePurgerResult), oldPurger),
            Term.Tuple(Term.A(ErtsInternalProtocol.CollectorResult), oldCollector)
        );
    }

    private static async ValueTask<Term> Breakpoint(ProcessContext context, IReadOnlyList<Term> args)
    {
        Term reference = await Bif(context, ErtsInternalProtocol.MakeRef);
        FunctionTerm action = context.Runtime.Modules.CreateErtsInternalResumeAction(context, reference);
        using var location = At(
            context,
            ErtsInternalFunctions.Breakpoint,
            args.Count,
            ErtsInternalSource.NotifyBreakpoint
        );
        Term result = await Internal(
            context,
            ErtsInternalFunctions.NotifyBreakpointHit,
            Term.Tuple(args[0], args[1], args[2]),
            args[3],
            action
        );
        if (Is(result, ErtsInternalProtocol.Ok))
        {
            await Receive(context, message => message.Equals(reference));
        }

        return Term.A(ErtsInternalProtocol.Ok);
    }

    private static async ValueTask<Term> DistributionData(ProcessContext context, Term handle, Term data)
    {
        try
        {
            Term binary = await Bif(context, ErtsInternalProtocol.IolistToBinary, data);

            return await Bif(
                context,
                ErtsInternalFunctions.DistCtrlPutData,
                handle,
                binary
            );
        }
        catch (ErlangException error)
        {
            Term stack = context.CaptureErrorStack(Term.I(2), null);
            Term tail = stack is Cons top ? top.Tail : Nil.Value;
            Term origin = Term.Tuple(
                Term.A(ErlangModuleNames.Module),
                Term.A(ErtsInternalFunctions.DistCtrlPutData),
                Term.List(handle, data),
                Nil.Value
            );

            throw new ErlangException(error.Reason, error.ExceptionClass)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = new Cons(origin, tail)
            };
        }
    }

    private static async ValueTask<Term> EtsBinary(ProcessContext context, Term table)
    {
        try
        {
            try
            {
                await Internal(context, ErtsInternalFunctions.EtsSuperUser, Term.A(ErtsInternalProtocol.True));
                await Call(
                    context,
                    ErtsInternalProtocol.Ets,
                    ErtsInternalProtocol.SafeFixtable,
                    table,
                    Term.A(ErtsInternalProtocol.True)
                );
                Term key = await Internal(context, ErtsInternalFunctions.EtsRawFirst, table);
                Term accumulator = Nil.Value;
                while (!Is(key, ErtsInternalProtocol.EndOfTable))
                {
                    Term binaries = await Internal(
                        context,
                        ErtsInternalFunctions.EtsLookupBinaryInfo,
                        table,
                        key
                    );
                    accumulator = binaries switch
                    {
                        Nil => accumulator,
                        Cons { Tail: Nil } one => new Cons(one.Head, accumulator),
                        Cons => await Bif(
                            context,
                            ErtsInternalProtocol.Append,
                            binaries,
                            accumulator
                        ),
                        _ => throw new ErlangException(Term.Tuple(Term.A(ErtsInternalProtocol.CaseClause), binaries))
                    };
                    key = await Internal(
                        context,
                        ErtsInternalFunctions.EtsRawNext,
                        table,
                        key
                    );
                }

                return accumulator;
            }
            catch (ErlangException error)
            {
                Term stack = error.WithCapturedStack(context.CaptureErrorStack(Term.I(1), null)).StackTraceTerm;
                Term tail = Nil.Value;
                Term cursor = stack;
                while (cursor is Cons cell)
                {
                    if (cell.Head is TupleTerm tuple && tuple.Items.Count >= 2
                        && Is(tuple.Items[0], ErtsInternalContract.Module) && Is(tuple.Items[1], ErtsInternalFunctions.EtsInfoBinary))
                    {
                        tail = cell.Tail;
                        break;
                    }
                    cursor = cell.Tail;
                }
                Term origin = Term.Tuple(
                    Term.A(ErtsInternalProtocol.Ets),
                    Term.A(ErtsInternalProtocol.Info),
                    Term.List(table, Term.A(ErtsInternalProtocol.Binary)),
                    Nil.Value
                );

                throw new ErlangException(error.Reason, error.ExceptionClass)
                {
                    ExplicitStackTrace = true,
                    StackTraceTerm = new Cons(origin, tail)
                };
            }
        }
        finally
        {
            using var location = At(
                context,
                ErtsInternalFunctions.EtsInfoBinary,
                1,
                ErtsInternalSource.EtsUnfixCall
            );
            await Call(
                context,
                ErtsInternalProtocol.Ets,
                ErtsInternalProtocol.SafeFixtable,
                table,
                Term.A(ErtsInternalProtocol.False)
            );
            await Internal(context, ErtsInternalFunctions.EtsSuperUser, Term.A(ErtsInternalProtocol.False));
        }
    }
}
