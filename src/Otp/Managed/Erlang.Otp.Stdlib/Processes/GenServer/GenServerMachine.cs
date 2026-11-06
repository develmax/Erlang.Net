// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_server startup, callbacks, actions and receive loop ported to C#.
using static Erlang.GenServerModule;

namespace Erlang;

internal sealed partial class GenServerMachine(ProcessContext context)
{
    internal Term Parent { get; private set; } = A(GenServerProtocolAtoms.Undefined);
    private Term name = A(GenServerProtocolAtoms.Undefined);
    private Term module = A(GenServerProtocolAtoms.Undefined);
    private Term tag = A(GenServerProtocolAtoms.Undefined);
    private Term hibernateAfter = A(GenServerProtocolAtoms.Infinity);
    private Term state = A(GenServerProtocolAtoms.Undefined);
    private Term action = A(GenServerProtocolAtoms.Infinity);
    private Term timer = Nil.Value;
    private Term debug = Nil.Value;
    private Term message = Nil.Value;
    private Term from = A(GenServerProtocolAtoms.Undefined);
    private bool transferred;
    private bool terminating;

    private ValueTask<Term> Call(string m, string f, params Term[] a) => GenServerModule.Call(
        context,
        m,
        f,
        a
    );

    private string Module => ((Atom)module).Name;

    private async ValueTask<Term> Callback(string f, int line, params Term[] args)
    {
        using var caller = f == GenServerFunctionNames.Init
            ? GenServerSource.Scope(
                context,
                GenServerFunctionNames.InitIt,
                6,
                GenServerSourceLines.InitializeCallback
            )
            : f == GenServerFunctionNames.Terminate
                ? GenServerSource.Scope(
                    context,
                    GenServerFunctionNames.Terminate,
                    9,
                    GenServerSourceLines.TerminateCallback
                )
                : GenServerSource.Scope(
                    context,
                    f == GenServerFunctionNames.HandleContinue ? GenServerFunctionNames.Loop : GenServerFunctionNames.HandleMsg,
                    f == GenServerFunctionNames.HandleContinue ? 4 : debug is Nil ? 3 : 4,
                    f == GenServerFunctionNames.HandleContinue ? GenServerSourceLines.ContinueDispatch : f == GenServerFunctionNames.HandleCall ? (debug is Nil ? GenServerSourceLines.CallDispatch : GenServerSourceLines.DebugCallDispatch) : f == GenServerFunctionNames.HandleCast ? GenServerSourceLines.CastDispatch : GenServerSourceLines.InfoDispatch
                );
        using var scope = GenServerSource.Scope(
            context,
            f == GenServerFunctionNames.Init ? GenServerFunctionNames.InitIt : GenServerFunctionNames.Try + f,
            f == GenServerFunctionNames.Init ? 2 : f == GenServerFunctionNames.HandleCall ? 4 : 3,
            line
        );
        try
        {
            return await Call(Module, f, args);
        }
        catch (ErlangException e) when (e.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            return e.Reason;
        }
    }

    internal static async ValueTask<Term> Initialize(ProcessContext c, Term[] a)
    {
        var m = new GenServerMachine(c)
        {
            Parent = Is(a[1], GenServerProtocolAtoms.Self) ? c.Self : a[1],
            module = a[3]
        };
        m.name = await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.Name, a[2]);
        m.tag = await m.Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.MakeRef);
        m.hibernateAfter = await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.HibernateAfter, a[5]);
        m.debug = await m.Call(
            GenServerDependencyNames.Gen,
            GenServerFunctionNames.DebugOptions,
            m.name,
            a[5]
        );
        Term result;
        try
        {
            result = await m.Callback(GenServerFunctionNames.Init, GenServerSourceLines.InitItLine2276, a[4]);
        }
        catch (ErlangException)
        {
            await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.UnregisterName, a[2]);
            throw;
        }
        if (result is TupleTerm { Items.Count: 2 or 3 } ok && Is(ok.Items[0], GenServerProtocolAtoms.Ok))
        {
            m.state = ok.Items[1];
            if (ok.Items.Count == 3 && !await m.Action(ok.Items[2]))
            {
                await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.UnregisterName, a[2]);
                throw GenServerSource.Exit(
                    c,
                    GenServerFunctionNames.InitIt,
                    6,
                    GenServerSourceLines.InitItLine2245,
                    Term.Tuple(A(GenServerProtocolAtoms.BadReturnValue), result)
                );
            }
            await m.Call(
                GenServerDependencyNames.ProcLib,
                GenServerFunctionNames.InitAck,
                a[0],
                Term.Tuple(A(GenServerProtocolAtoms.Ok), c.Self)
            );

            return await m.Run();
        }
        await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.UnregisterName, a[2]);
        if (Is(result, GenServerProtocolAtoms.Ignore) || result is TupleTerm { Items.Count: 2 } error && Is(error.Items[0], GenServerProtocolAtoms.Error))
            return await m.Call(
                GenServerDependencyNames.ProcLib,
                GenServerFunctionNames.InitFail,
                a[0],
                result,
                Term.Tuple(A(GenServerProtocolAtoms.Exit), A(GenServerProtocolAtoms.Normal))
            );
        if (result is TupleTerm { Items.Count: 2 } stop && Is(stop.Items[0], GenServerProtocolAtoms.Stop))
            throw GenServerSource.Exit(
                c,
                GenServerFunctionNames.InitIt,
                6,
                GenServerSourceLines.InitItLine2257,
                stop.Items[1]
            );
        throw GenServerSource.Exit(
            c,
            GenServerFunctionNames.InitIt,
            6,
            GenServerSourceLines.InitItLine2269,
            Term.Tuple(A(GenServerProtocolAtoms.BadReturnValue), result)
        );
    }

    internal static async ValueTask<Term> EnterLoop(ProcessContext c, Term[] a)
    {
        if (a[0] is not Atom || !List(a[1])) throw GenServerSource.Clause(
            c,
            GenServerFunctionNames.EnterLoop,
            a,
            a.Length == 3 ? GenServerSourceLines.EnterLoopLine2115 : a.Length == 4 ? GenServerSourceLines.EnterLoopLine2144 : GenServerSourceLines.EnterLoopLine2206
        );
        var m = new GenServerMachine(c) { module = a[0], state = a[2] };
        Term server = c.Self;
        Term initial = A(GenServerProtocolAtoms.Infinity);
        if (a.Length == 4)
        {
            if (a[3] is TupleTerm tuple && (tuple.Items.Count == 2 && (Is(tuple.Items[0], GenServerProtocolAtoms.Local) || Is(tuple.Items[0], GenServerProtocolAtoms.Global)) || tuple.Items.Count == 3 && Is(tuple.Items[0], GenServerProtocolAtoms.Via))) server = a[3];
            else initial = a[3];
        }
        else if (a.Length == 5)
        {
            server = a[3];
            initial = a[4];
        }
        using (GenServerSource.Scope(
            c,
            GenServerFunctionNames.EnterLoop,
            5,
            GenServerSourceLines.EnterLoopLine2208
        )) m.name = await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.GetProcName, server);
        using (GenServerSource.Scope(
            c,
            GenServerFunctionNames.EnterLoop,
            5,
            GenServerSourceLines.EnterLoopLine2209
        )) m.Parent = await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.GetParent);
        using (GenServerSource.Scope(
            c,
            GenServerFunctionNames.EnterLoop,
            5,
            GenServerSourceLines.EnterLoopLine2209
        )) m.hibernateAfter = await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.HibernateAfter, a[1]);
        m.tag = await m.Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.MakeRef);
        if (!await m.Action(initial))
        {
            await m.Call(GenServerDependencyNames.Gen, GenServerFunctionNames.UnregisterName, m.name);
            throw GenServerSource.Exit(
                c,
                GenServerFunctionNames.EnterLoop,
                5,
                GenServerSourceLines.EnterLoopLine2213,
                Term.Tuple(A(GenServerProtocolAtoms.BadAction), initial)
            );
        }
        m.debug = await m.Call(
            GenServerDependencyNames.Gen,
            GenServerFunctionNames.DebugOptions,
            m.name,
            a[1]
        );

        return await m.Run();
    }

    private async ValueTask<bool> Action(Term value)
    {
        if (value is TupleTerm { Items.Count: 2 } continuation && Is(continuation.Items[0], GenServerProtocolAtoms.Continue) || Is(value, GenServerProtocolAtoms.Hibernate) || Timeout(value))
        {
            action = value;
            timer = Nil.Value;

            return true;
        }
        if (value is not TupleTerm { Items.Count: 3 or 4 } t || !(Is(t.Items[0], GenServerProtocolAtoms.Timeout) || Is(t.Items[0], GenServerProtocolAtoms.Hibernate))) return false;
        bool absolute = false;
        if (t.Items.Count == 4)
        {
            Term options = List(t.Items[3]) ? t.Items[3] : Term.List(t.Items[3]);
            if (!ProperList(options)) return false;
            foreach (Term option in Cons.Items(options))
            {
                if (option is not TupleTerm { Items.Count: 2 } pair || !Is(pair.Items[0], GenServerProtocolAtoms.Abs) || !(Is(pair.Items[1], GenServerProtocolAtoms.True) || Is(pair.Items[1], GenServerProtocolAtoms.False))) return false;
                absolute = Is(pair.Items[1], GenServerProtocolAtoms.True);
            }
        }
        if (!Timeout(t.Items[1], absolute)) return false;
        Term hib = Is(t.Items[0], GenServerProtocolAtoms.Hibernate) ? A(GenServerProtocolAtoms.Hibernate) : A(GenServerProtocolAtoms.Infinity);
        if (!absolute && t.Items[1] is Integer { Value.IsZero: true })
        {
            if (Is(hib, GenServerProtocolAtoms.Hibernate)) await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.GarbageCollect);
            action = Term.Tuple(A(GenServerProtocolAtoms.TimeoutZero), t.Items[2]);
            timer = Nil.Value;
        }
        else if (Is(t.Items[1], GenServerProtocolAtoms.Infinity))
        {
            action = hib;
            timer = Nil.Value;
        }
        else
        {
            using var scope = GenServerSource.Scope(
                context,
                GenServerFunctionNames.HandleTimer,
                5,
                absolute ? GenServerSourceLines.HandleTimerLine2640 : GenServerSourceLines.HandleTimerLine2642
            );
            Term reference = await Call(
                GenServerDependencyNames.Erlang,
                GenServerFunctionNames.StartTimer,
                absolute ? [t.Items[1], context.Self, tag, Term.List(Term.Tuple(A(GenServerProtocolAtoms.Abs), A(GenServerProtocolAtoms.True)))] : [t.Items[1], context.Self, tag]
            );
            action = hib;
            timer = new Cons(reference, t.Items[2]);
        }

        return true;
    }

    private async ValueTask CancelTimer()
    {
        if (timer is Cons cell) await Call(
            GenServerDependencyNames.Erlang,
            GenServerFunctionNames.CancelTimer,
            cell.Head,
            Term.List(
                Term.Tuple(A(GenServerProtocolAtoms.Async), A(GenServerProtocolAtoms.True)),
                Term.Tuple(A(GenServerProtocolAtoms.Info), A(GenServerProtocolAtoms.False))
            )
        );
        timer = Nil.Value;
    }

    private async ValueTask<Term> Receive()
    {
        if (Is(action, GenServerProtocolAtoms.Hibernate))
        {
            Term? queued = await context.ReceiveAsync(t => t, TimeSpan.Zero);
            if (queued is not null)
            {
                await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.GarbageCollect);

                return queued;
            }
            await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.Hibernate);

            return (await context.ReceiveAsync(t => t))!;
        }
        Term duration = Is(action, GenServerProtocolAtoms.Infinity) ? hibernateAfter : action;
        TimeSpan? time = null;
        if (!Is(duration, GenServerProtocolAtoms.Infinity))
        {
            if (duration is not Integer n || n.Value < 0 || n.Value > uint.MaxValue) throw GenServerSource.Failure(
                context,
                GenServerFunctionNames.Loop,
                5,
                Is(action, GenServerProtocolAtoms.Infinity) ? GenServerSourceLines.LoopLine2321 : GenServerSourceLines.LoopLine2330,
                A(GenServerProtocolAtoms.TimeoutValue)
            );
            time = TimeSpan.FromMilliseconds((double)n.Value);
        }
        Term? message = await context.ReceiveAsync(t => t, time);
        if (message is not null) return message;
        if (!Is(action, GenServerProtocolAtoms.Infinity))
        {
            action = A(GenServerProtocolAtoms.Infinity);

            return A(GenServerProtocolAtoms.Timeout);
        }
        await Call(GenServerDependencyNames.Erlang, GenServerFunctionNames.Hibernate);
        action = A(GenServerProtocolAtoms.Hibernate);

        return (await context.ReceiveAsync(t => t))!;
    }

    internal async ValueTask<Term> Run()
    {
        while (true)
        {
            await context.ReduceAsync();
            from = A(GenServerProtocolAtoms.Undefined);
            Term result;
            bool call = false;
            if (action is TupleTerm { Items.Count: 2 } cont && Is(cont.Items[0], GenServerProtocolAtoms.Continue))
            {
                message = action;
                try
                {
                    result = await Callback(
                        GenServerFunctionNames.HandleContinue,
                        GenServerSourceLines.TryHandleContinueLine2424,
                        cont.Items[1],
                        state
                    );
                }
                catch (ErlangException error)
                {
                    return await Terminate(error, true);
                }
                await Debug(message);
            }
            else
            {
                if (action is TupleTerm { Items.Count: 2 } zero && Is(zero.Items[0], GenServerProtocolAtoms.TimeoutZero))
                {
                    message = zero.Items[1];
                    action = A(GenServerProtocolAtoms.Infinity);
                }
                else message = await Receive();
                if (message is TupleTerm { Items.Count: 3 } system && Is(system.Items[0], GenServerProtocolAtoms.System))
                {
                    // sys owns its successor loop, including termination. This frame must not terminate stale state.
                    transferred = true;

                    return await Call(
                        GenServerDependencyNames.Sys,
                        GenServerFunctionNames.HandleSystemMsg,
                        system.Items[2],
                        system.Items[1],
                        Parent,
                        A(Name),
                        debug,
                        Snapshot(),
                        Is(action, GenServerProtocolAtoms.Hibernate) ? A(GenServerProtocolAtoms.True) : A(GenServerProtocolAtoms.False)
                    );
                }
                if (message is TupleTerm { Items.Count: 3 } exit && Is(exit.Items[0], GenServerProtocolAtoms.ExitSignal) && exit.Items[1].Equals(Parent))
                    return await Terminate(new ErlangException(exit.Items[2], ErlangExceptionClasses.Exit));
                if (message is TupleTerm { Items.Count: 3 } tick && Is(tick.Items[0], GenServerProtocolAtoms.Timeout) && tick.Items[2].Equals(tag))
                {
                    if (timer is not Cons active || !tick.Items[1].Equals(active.Head)) continue;
                    message = active.Tail;
                    timer = Nil.Value;
                }
                else await CancelTimer();
                await Debug(Term.Tuple(A(GenServerProtocolAtoms.In), message));
                try
                {
                    if (message is TupleTerm { Items.Count: 3 } request && Is(request.Items[0], GenServerProtocolAtoms.GenCall))
                    {
                        call = true;
                        from = request.Items[1];
                        message = request.Items[2];
                        result = await Callback(
                            GenServerFunctionNames.HandleCall,
                            GenServerSourceLines.TryHandleCallLine2470,
                            message,
                            from,
                            state
                        );
                    }
                    else if (message is TupleTerm { Items.Count: 2 } cast && Is(cast.Items[0], GenServerProtocolAtoms.GenCast))
                        result = await Callback(
                            GenServerFunctionNames.HandleCast,
                            GenServerSourceLines.TryHandleCastLine2460,
                            cast.Items[1],
                            state
                        );
                    else
                    {
                        try
                        {
                            result = await Callback(
                                GenServerFunctionNames.HandleInfo,
                                GenServerSourceLines.TryHandleInfoLine2434,
                                message,
                                state
                            );
                        }
                        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && Is(error.Reason, GenServerProtocolAtoms.Undef) && !context.Runtime.Modules.Exports.Contains((Module, GenServerFunctionNames.HandleInfo, 2)))
                        {
                            await MissingInfo();
                            result = Term.Tuple(A(GenServerProtocolAtoms.Noreply), state);
                        }
                    }
                }
                catch (ErlangException error)
                {
                    return await Terminate(error, true);
                }
            }
            if (result is TupleTerm { Items.Count: > 0 } r)
            {
                if (call && r.Items.Count is 3 or 4 && Is(r.Items[0], GenServerProtocolAtoms.Reply))
                {
                    if (r.Items.Count == 4 && !await Action(r.Items[3])) return await BadReturn(result);
                    if (r.Items.Count == 3) await Action(A(GenServerProtocolAtoms.Infinity));
                    state = r.Items[2];
                    await Reply(r.Items[1]);
                    continue;
                }
                if (Is(r.Items[0], GenServerProtocolAtoms.Noreply) && r.Items.Count is 2 or 3)
                {
                    if (r.Items.Count == 3 && !await Action(r.Items[2])) return await BadReturn(result);
                    if (r.Items.Count == 2) await Action(A(GenServerProtocolAtoms.Infinity));
                    state = r.Items[1];
                    await Debug(Term.Tuple(A(GenServerProtocolAtoms.Noreply), state));
                    continue;
                }
                if (Is(r.Items[0], GenServerProtocolAtoms.Stop) && (r.Items.Count == 3 || call && r.Items.Count == 4))
                {
                    state = r.Items[^1];
                    try
                    {
                        return await Terminate(new ErlangException(r.Items[1], ErlangExceptionClasses.Exit));
                    }
                    finally
                    {
                        if (r.Items.Count == 4) await Reply(r.Items[2]);
                    }
                }
            }

            return await BadReturn(result);
        }
    }

    private async ValueTask Reply(Term value)
    {
        await Call(
            GenServerDependencyNames.Gen,
            GenServerFunctionNames.Reply,
            from,
            value
        );
        await Debug(Term.Tuple(
            A(GenServerProtocolAtoms.Out),
            value,
            from,
            state
        ));
    }

    private ValueTask<Term> BadReturn(Term value) => Terminate(new ErlangException(Term.Tuple(A(GenServerProtocolAtoms.BadReturnValue), value), ErlangExceptionClasses.Exit));
}
