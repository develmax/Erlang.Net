// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2025. All Rights Reserved.
// Modified: OTP debug structures, callbacks and bounded log adapted to .NET.
namespace Erlang;

public static partial class SysModule
{
    private static Term DebugData(Term key, Term debug, Term fallback)
    {
        foreach (Term option in Cons.Items(debug))
        {
            if (option is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].NumericEquals(key))
            {
                return tuple.Items.Count == 2 && tuple.Items[0].Equals(key) ? tuple.Items[1] : fallback;
            }
        }

        return fallback;
    }

    private static Term Install(Term key, Term data, Term debug)
    {
        foreach (Term option in Cons.Items(debug))
        {
            if (option is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].NumericEquals(key))
            {
                return debug;
            }
        }

        return new Cons(Term.Tuple(key, data), debug);
    }

    private static Term Remove(Term key, Term debug)
    {
        var prefix = new List<Term>();
        Term tail = debug;
        while (tail is Cons cons)
        {
            if (cons.Head is TupleTerm tuple && tuple.Items.Count > 0 && tuple.Items[0].NumericEquals(key))
            {
                return Cons.From(prefix, cons.Tail);
            }
            prefix.Add(cons.Head);
            tail = cons.Tail;
        }
        if (tail is not Nil)
        {
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return debug;
    }

    private static Term NewLog() => Term.List(Term.I(SysDebugLimits.DefaultLogCapacity));

    private static Term LogPut(ProcessContext context, Term item, Term log)
    {
        if (log is not Cons cons)
        {
            throw SysSource.Failure(
                context,
                Term.Tuple(A(SysProtocolNames.CaseClause), log),
                SysSourceNames.NlogPut,
                2,
                SysSourceLines.LogPutCase
            );
        }
        if (cons.Head is Nil or Cons)
        {
            if (cons.Tail is Cons forward)
            {
                return new Cons(new Cons(item, cons.Head), forward.Tail);
            }
            Term rotated = Cons.From(Cons.Items(cons.Head).Reverse(), Term.List(item));

            return new Cons(Nil.Value, ((Cons)rotated).Tail);
        }

        return cons.Head.Equals(Term.I(1)) ? Term.List(new Cons(item, cons.Tail))
            : new Cons(CoreModules.Arithmetic(ErlangOperators.Minus, cons.Head, Term.I(1)), new Cons(item, cons.Tail));
    }

    private static Term LogItems(ProcessContext context, Term log)
    {
        if (log is not Cons cons)
        {
            throw SysSource.Error(
                context,
                SysSourceNames.NlogGet,
                [log],
                SysSourceLines.LogGetClause,
                ErlangErrorReasons.FunctionClause
            );
        }
        if (cons.Head is Nil)
        {
            return cons.Tail;
        }

        return cons.Head is Cons
            ? Cons.From(Cons.Items(cons.Tail).Concat(Cons.Items(cons.Head).Reverse()))
            : Cons.From(Cons.Items(cons.Tail).Reverse());
    }

    private static Term ResizeLog(ProcessContext context, Term count, Term log)
    {
        Term result = Term.List(count);
        foreach (Term item in Cons.Items(LogItems(context, log)))
        {
            result = LogPut(context, item, result);
        }

        return result;
    }

    private static Term GetLog(ProcessContext context, Term debug)
        => Cons.From(Cons.Items(LogItems(context, DebugData(A(SysApiNames.Log), debug, NewLog())))
            .OfType<TupleTerm>().Where(tuple => tuple.Items.Count == 3).Select(tuple => tuple.Items[0]));

    private static async ValueTask<Term> PrintLog(ProcessContext context, Term debug)
    {
        foreach (Term item in Cons.Items(LogItems(context, DebugData(A(SysApiNames.Log), debug, NewLog()))))
        {
            await PrintEvent(context, A(SysProtocolNames.StandardIo), item);
        }

        return A(SysProtocolNames.Ok);
    }

    private static async ValueTask PrintEvent(ProcessContext context, Term device, Term item)
    {
        if (item is not TupleTerm { Items: [var @event, var extra, var formatter] })
        {
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        }
        if (formatter is not FunctionTerm)
        {
            throw SysSource.Failure(
                context,
                Term.Tuple(A(SysProtocolNames.Badfun), formatter),
                SysSourceNames.PrintEvent,
                2,
                SysSourceLines.PrintEventCallback
            );
        }
        await Apply(
            context,
            formatter,
            device,
            @event,
            extra
        );
    }

    private static async ValueTask<Term> HandleDebug(
        ProcessContext context,
        Term debug,
        Term formatter,
        Term extra,
        Term @event
    )
    {
        var result = new List<Term>();
        Term remaining = debug;
        while (remaining is Cons current)
        {
            Term option = current.Head;
            if (option is not TupleTerm { Items: [var key, var data] })
            {
                throw SysSource.Error(
                    context,
                    SysApiNames.HandleDebug,
                    [remaining, formatter, extra, @event],
                    SysSourceLines.HandleDebugClause,
                    ErlangErrorReasons.FunctionClause
                );
            }
            remaining = current.Tail;
            switch (key)
            {
                case Atom { Name: SysApiNames.Trace } when Is(data, SysProtocolNames.True):
                    using (SysSource.Scope(
                        context,
                        SysApiNames.HandleDebug,
                        4,
                        SysSourceLines.HandleDebugTrace
                    ))
                    {
                        await PrintEvent(context, A(SysProtocolNames.StandardIo), Term.Tuple(@event, extra, formatter));
                    }
                    result.Add(option);
                    break;
                case Atom { Name: SysApiNames.Log }:
                    result.Add(Term.Tuple(key, LogPut(context, Term.Tuple(@event, extra, formatter), data)));
                    break;
                case Atom { Name: SysApiNames.LogToFile }:
                    using (SysSource.Scope(
                        context,
                        SysApiNames.HandleDebug,
                        4,
                        SysSourceLines.HandleDebugFile
                    ))
                    {
                        await PrintEvent(context, data, Term.Tuple(@event, extra, formatter));
                    }
                    result.Add(option);
                    break;
                case Atom { Name: SysApiNames.Statistics }:
                    result.Add(Term.Tuple(key, Stat(@event, data)));
                    break;
                default:
                    bool identified = data is TupleTerm { Items.Count: 2 };
                    Term function = identified ? ((TupleTerm)data).Items[0] : key;
                    Term state = identified ? ((TupleTerm)data).Items[1] : data;
                    Term next;
                    try
                    {
                        next = await Apply(
                            context,
                            function,
                            state,
                            @event,
                            extra
                        );
                    }
                    catch (ErlangException error)
                    {
                        if (error.ExceptionClass != ErlangExceptionClasses.Throw)
                        {
                            continue;
                        }
                        next = error.Reason;
                    }
                    if (!Is(next, SysProtocolNames.Done))
                    {
                        result.Add(Term.Tuple(key, identified ? Term.Tuple(function, next) : next));
                    }
                    break;
            }
        }
        if (remaining is not Nil)
        {
            throw SysSource.Error(
                context,
                SysApiNames.HandleDebug,
                [remaining, formatter, extra, @event],
                SysSourceLines.HandleDebugClause,
                ErlangErrorReasons.FunctionClause
            );
        }

        return Cons.From(result);
    }

    private static Term Stat(Term @event, Term data)
    {
        if (@event is TupleTerm evt && evt.Items.Count > 0 &&
            (Is(evt.Items[0], SysProtocolNames.In) && evt.Items.Count is 2 or 3 || Is(evt.Items[0], SysProtocolNames.Out) && evt.Items.Count is 3 or 4))
        {
            if (data is TupleTerm { Items: [var time, var reductions, var input, var output] })
            {
                bool incoming = Is(evt.Items[0], SysProtocolNames.In);

                return Term.Tuple(
                    time,
                    reductions,
                    incoming ? CoreModules.Arithmetic(ErlangOperators.Plus, input, Term.I(1)) : input,
                    incoming ? output : CoreModules.Arithmetic(ErlangOperators.Plus, output, Term.I(1))
                );
            }
        }

        return data;
    }

    private static async ValueTask<Term> InitStat(ProcessContext context)
        => Term.Tuple(
            await Call(context, SysDependencyNames.Erlang, SysDependencyFunctions.Localtime),
            await Call(
                context,
                SysDependencyNames.Erlang,
                SysDependencyFunctions.ProcessInfo,
                context.Self,
                A(SysProtocolNames.Reductions)
            ),
            Term.I(0),
            Term.I(0)
        );

    private static async ValueTask<Term> GetStat(ProcessContext context, Term data)
    {
        if (data is not TupleTerm { Items: [var time, TupleTerm { Items: [Atom { Name: SysProtocolNames.Reductions }, var before] }, var input, var output] })
        {
            return A(SysProtocolNames.NoStatistics);
        }
        var current = (TupleTerm)await Call(
            context,
            SysDependencyNames.Erlang,
            SysDependencyFunctions.ProcessInfo,
            context.Self,
            A(SysProtocolNames.Reductions)
        );

        return Term.List(
            Term.Tuple(A(SysProtocolNames.StartTime), time),
            Term.Tuple(A(SysProtocolNames.CurrentTime), await Call(context, SysDependencyNames.Erlang, SysDependencyFunctions.Localtime)),
            Term.Tuple(A(SysProtocolNames.Reductions), CoreModules.Arithmetic(ErlangOperators.Minus, current.Items[1], before)),
            Term.Tuple(A(SysProtocolNames.MessagesIn), input),
            Term.Tuple(A(SysProtocolNames.MessagesOut), output)
        );
    }

    private static async ValueTask<Term> CloseLogFile(ProcessContext context, Term debug)
    {
        Term descriptor = DebugData(A(SysApiNames.LogToFile), debug, Nil.Value);
        if (descriptor is Nil)
        {
            return debug;
        }
        Term result = await Call(
            context,
            SysDependencyNames.File,
            SysDependencyFunctions.Close,
            descriptor
        );
        if (!Is(result, SysProtocolNames.Ok))
        {
            throw new ErlangException(Term.Tuple(A(SysProtocolNames.Badmatch), result));
        }

        return Remove(A(SysApiNames.LogToFile), debug);
    }

    private static ValueTask<Term> OpenLogFile(ProcessContext context, Term file)
        => Call(
            context,
            SysDependencyNames.File,
            SysDependencyFunctions.Open,
            file,
            Term.List(A(SysProtocolNames.Write), Term.Tuple(A(SysProtocolNames.Encoding), A(SysProtocolNames.Utf8)))
        );

    private static async ValueTask<Term> DebugOptions(ProcessContext context, Term options)
    {
        Term debug = Nil.Value;
        Term remaining = options;
        while (remaining is Cons current)
        {
            Term option = current.Head;
            remaining = current.Tail;
            if (Is(option, SysApiNames.Trace))
            {
                debug = Install(option, A(SysProtocolNames.True), debug);
            }
            else if (Is(option, SysApiNames.Log))
            {
                debug = Install(option, NewLog(), debug);
            }
            else if (Is(option, SysApiNames.Statistics))
            {
                debug = Install(option, await InitStat(context), debug);
            }
            else if (option is TupleTerm { Items: [Atom { Name: SysApiNames.Log }, Integer { Value.Sign: > 0 } count] })
            {
                debug = Install(A(SysApiNames.Log), Term.List(count), debug);
            }
            else if (option is TupleTerm { Items: [Atom { Name: SysApiNames.LogToFile }, var file] })
            {
                Term result = await OpenLogFile(context, file);
                if (result is TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, var descriptor] })
                {
                    debug = Install(A(SysApiNames.LogToFile), descriptor, debug);
                }
            }
            else if (option is TupleTerm { Items: [Atom { Name: SysApiNames.Install }, TupleTerm specification] })
            {
                if (specification.Items.Count == 2)
                {
                    debug = Install(specification.Items[0], specification.Items[1], debug);
                }
                else if (specification.Items.Count == 3)
                {
                    debug = Install(specification.Items[0], Term.Tuple(specification.Items[1], specification.Items[2]), debug);
                }
            }
        }
        if (remaining is not Nil)
        {
            throw SysSource.Error(
                context,
                SysApiNames.DebugOptions,
                [remaining, debug],
                SysSourceLines.DebugOptionsClause,
                ErlangErrorReasons.FunctionClause
            );
        }

        return debug;
    }

    private static async ValueTask<(Term Reply, Term Debug)> DebugCommand(ProcessContext context, Term what, Term debug)
    {
        if (Is(what, SysApiNames.NoDebug))
        {
            await CloseLogFile(context, debug);

            return (A(SysProtocolNames.Ok), Nil.Value);
        }
        if (what is not TupleTerm { Items: [var key, var value] })
        {
            return (A(SysProtocolNames.UnknownDebug), debug);
        }
        if (Is(key, SysApiNames.Trace) && Is(value, SysProtocolNames.True))
        {
            return (A(SysProtocolNames.Ok), Install(key, value, debug));
        }
        if ((Is(key, SysApiNames.Trace) || Is(key, SysApiNames.Log) || Is(key, SysApiNames.Statistics)) && Is(value, SysProtocolNames.False))
        {
            return (A(SysProtocolNames.Ok), Remove(key, debug));
        }
        if (Is(key, SysApiNames.Log))
        {
            if (Is(value, SysProtocolNames.True))
            {
                Term old = DebugData(key, debug, NewLog());
                Term next = old is Cons ? ResizeLog(context, Term.I(SysDebugLimits.DefaultLogCapacity), old) : Term.List(old);

                return (A(SysProtocolNames.Ok), Install(key, next, debug));
            }
            if (value is TupleTerm { Items: [Atom { Name: SysProtocolNames.True }, Integer { Value.Sign: > 0 } count] })
            {
                Term old = DebugData(key, debug, Term.List(count));

                return (A(SysProtocolNames.Ok), Install(key, ResizeLog(context, count, old), debug));
            }
            if (Is(value, SysDebugFlags.Get))
            {
                return (Term.Tuple(A(SysProtocolNames.Ok), GetLog(context, debug)), debug);
            }
            if (Is(value, SysProtocolNames.Print))
            {
                await PrintLog(context, debug);

                return (A(SysProtocolNames.Ok), debug);
            }
        }
        if (Is(key, SysApiNames.LogToFile))
        {
            debug = await CloseLogFile(context, debug);
            if (Is(value, SysProtocolNames.False))
            {
                return (A(SysProtocolNames.Ok), debug);
            }
            Term opened = await OpenLogFile(context, value);

            return opened is TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, var descriptor] }
                ? (A(SysProtocolNames.Ok), Install(key, descriptor, debug)) : (Term.Tuple(A(SysProtocolNames.Error), A(SysProtocolNames.OpenFile)), debug);
        }
        if (Is(key, SysApiNames.Statistics))
        {
            if (Is(value, SysProtocolNames.True))
            {
                return (A(SysProtocolNames.Ok), Install(key, await InitStat(context), debug));
            }
            if (Is(value, SysDebugFlags.Get))
            {
                return (Term.Tuple(A(SysProtocolNames.Ok), await GetStat(context, DebugData(key, debug, Nil.Value))), debug);
            }
        }
        if (Is(key, SysApiNames.Install) && value is TupleTerm specification)
        {
            if (specification.Items.Count == 2)
            {
                return (A(SysProtocolNames.Ok), Install(specification.Items[0], specification.Items[1], debug));
            }
            if (specification.Items.Count == 3)
            {
                return (A(SysProtocolNames.Ok), Install(specification.Items[0], Term.Tuple(specification.Items[1], specification.Items[2]), debug));
            }
        }
        if (Is(key, SysApiNames.Remove))
        {
            return (A(SysProtocolNames.Ok), Remove(value, debug));
        }

        return (A(SysProtocolNames.UnknownDebug), debug);
    }
}
