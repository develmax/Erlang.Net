// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 2016-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_statem receive loop, startup and records ported to C#.
using static Erlang.GenStatemModule;

namespace Erlang;

internal sealed partial class GenStatemMachine(ProcessContext context)
{
    private Term callbackMode = Term.A(GenStatemProtocolAtoms.StateFunctions);
    private bool stateEnter;
    internal Term Parent { get; set; } = Term.A(GenStatemProtocolAtoms.Undefined);
    private List<Term> modules = [];
    private Term name = Term.A(GenStatemProtocolAtoms.Undefined);
    private Term hibernateAfter = Term.A(GenStatemProtocolAtoms.Infinity);
    internal Term StateData { get; set; } = Term.Tuple(Term.A(GenStatemProtocolAtoms.Undefined), Term.A(GenStatemProtocolAtoms.Undefined));
    private readonly List<Term> postponed = [];
    private readonly Dictionary<Term, Term> timers = [];
    private readonly Queue<Term> zeroQueue = new();
    private readonly LinkedList<Term> events = new();
    private bool hibernate;
    private Term debug = Nil.Value;
    private bool terminating;
    private Term? currentEvent;
    private string Module => ((Atom)modules[0]).Name;
    private Term State => ((TupleTerm)StateData).Items[GenStatemRecordLayout.StateData.State];
    private Term Data => ((TupleTerm)StateData).Items[GenStatemRecordLayout.StateData.Data];

    internal static bool Is(Term value, string atom) => value is Atom a && a.Name == atom;

    internal static bool IsList(Term value)
    {
        while (value is Cons cell)
        {
            value = cell.Tail;
        }

        return value is Nil;
    }

    internal static IEnumerable<Term> Listify(Term value) => IsList(value) ? Cons.Items(value) : [value];

    private ValueTask<Term> Call(string module, string function, params Term[] args)
        => GenStatemModule.Call(
            context,
            module,
            function,
            args
        );

    private async ValueTask<Term> Callback(string function, params Term[] args)
    {
        try
        {
            return await Call(Module, function, args);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            return error.Reason;
        }
    }

    internal static async ValueTask<Term> Initialize(ProcessContext context, Term[] a)
    {
        var machine = new GenStatemMachine(context)
        {
            Parent = Is(a[1], GenStatemProtocolAtoms.Self) ? context.Self : a[1],
            modules = [a[3]]
        };
        machine.name = await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.GetProcName, a[2]);
        machine.debug = await machine.Call(
            GenStatemDependencyNames.Gen,
            GenStatemFunctionNames.DebugOptions,
            machine.name,
            a[5]
        );
        machine.hibernateAfter = await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.HibernateAfter, a[5]);
        Term result;
        try
        {
            result = await machine.Callback(GenStatemFunctionNames.Init, a[4]);
        }
        catch (ErlangException error)
        {
            await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.UnregisterName, a[2]);
            await machine.ErrorInfo(error);

            return await machine.Call(
                GenStatemDependencyNames.ProcLib,
                GenStatemFunctionNames.InitFail,
                a[0],
                Term.Tuple(Term.A(GenStatemProtocolAtoms.Error), error.Reason),
                Term.Tuple(Term.A(error.ExceptionClass), error.Reason, error.StackTraceTerm)
            );
        }
        if (result is TupleTerm { Items.Count: 3 or 4 } ok && Is(ok.Items[0], GenStatemProtocolAtoms.Ok))
        {
            machine.StateData = Term.Tuple(ok.Items[1], ok.Items[2]);
            await machine.Call(
                GenStatemDependencyNames.ProcLib,
                GenStatemFunctionNames.InitAck,
                a[0],
                Term.Tuple(Term.A(GenStatemProtocolAtoms.Ok), context.Self)
            );

            return await machine.Enter(ok.Items.Count == 4 ? ok.Items[3] : Nil.Value);
        }
        await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.UnregisterName, a[2]);
        if (Is(result, GenStatemProtocolAtoms.Ignore) || result is TupleTerm { Items.Count: 2 } failure && Is(failure.Items[0], GenStatemProtocolAtoms.Error))
        {
            return await machine.Call(
                GenStatemDependencyNames.ProcLib,
                GenStatemFunctionNames.InitFail,
                a[0],
                result,
                Term.Tuple(Term.A(GenStatemProtocolAtoms.Exit), Term.A(GenStatemProtocolAtoms.Normal))
            );
        }
        if (result is TupleTerm { Items.Count: 2 } stop && Is(stop.Items[0], GenStatemProtocolAtoms.Stop))
        {
            throw new ErlangException(stop.Items[1], ErlangExceptionClasses.Exit);
        }
        var bad = new ErlangException(Term.Tuple(Term.A(GenStatemProtocolAtoms.BadReturnFromInit), result), ErlangExceptionClasses.Exit);
        await machine.ErrorInfo(bad);
        throw bad;
    }

    internal static async ValueTask<Term> EnterLoop(ProcessContext context, Term[] a)
    {
        if (a[0] is not Atom)
        {
            throw GenStatemSource.Failure(
                context,
                Term.Tuple(Term.A(GenStatemProtocolAtoms.Atom), a[0]),
                GenStatemFunctionNames.EnterLoop,
                GenStatemSourceArities.EnterLoop,
                GenStatemSourceLines.EnterLoopModuleGuard
            );
        }
        Term server = context.Self;
        Term actions = Nil.Value;
        if (a.Length == 5)
        {
            if (IsList(a[4]))
            {
                actions = a[4];
            }
            else
            {
                server = a[4];
            }
        }
        if (a.Length == 6)
        {
            server = a[4];
            actions = a[5];
        }
        var machine = new GenStatemMachine(context)
        {
            modules = [a[0]],
            StateData = Term.Tuple(a[2], a[3])
        };
        using (GenStatemSource.Scope(
            context,
            GenStatemFunctionNames.EnterLoop,
            GenStatemSourceArities.EnterLoop,
            GenStatemSourceLines.EnterLoopParent
        ))
        {
            machine.Parent = await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.GetParent);
        }
        using (GenStatemSource.Scope(
            context,
            GenStatemFunctionNames.EnterLoop,
            GenStatemSourceArities.EnterLoop,
            GenStatemSourceLines.EnterLoopProcessName
        ))
        {
            machine.name = await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.GetProcName, server);
        }
        using (GenStatemSource.Scope(
            context,
            GenStatemFunctionNames.EnterLoop,
            GenStatemSourceArities.EnterLoop,
            GenStatemSourceLines.EnterLoopDebugOptions
        ))
        {
            machine.debug = await machine.Call(
                GenStatemDependencyNames.Gen,
                GenStatemFunctionNames.DebugOptions,
                machine.name,
                a[1]
            );
        }
        using (GenStatemSource.Scope(
            context,
            GenStatemFunctionNames.EnterLoop,
            GenStatemSourceArities.EnterLoop,
            GenStatemSourceLines.EnterLoopHibernateAfter
        ))
        {
            machine.hibernateAfter = await machine.Call(GenStatemDependencyNames.Gen, GenStatemFunctionNames.HibernateAfter, a[1]);
        }

        return await machine.Enter(actions);
    }

    private async ValueTask<Term> Enter(Term actions)
    {
        try
        {
            using (GenStatemSource.Scope(
                context,
                GenStatemFunctionNames.Enter,
                GenStatemSourceArities.Enter,
                GenStatemSourceLines.EnterCallbackMode
            ))
            {
                await SetCallbackMode();
            }
            await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.Enter), modules[0], State));
            var transition = new GenStatemTransition(State, Data);
            Term initialEvent = Term.Tuple(Term.A(GenStatemProtocolAtoms.Internal), Term.A(GenStatemProtocolAtoms.InitState));
            await Actions(
                transition,
                Cons.From(Listify(actions).Append(Term.Tuple(Term.A(GenStatemProtocolAtoms.Postpone), Term.A(GenStatemProtocolAtoms.False)))),
                true
            );
            await Complete(transition, initialEvent, true);
        }
        catch (ErlangException error) when (!terminating)
        {
            return await Terminate(error);
        }

        return await Run();
    }

    private async ValueTask SetCallbackMode()
    {
        Term result = await Callback(GenStatemFunctionNames.CallbackMode);
        Term? selected = null;
        bool enter = false;
        foreach (Term value in Listify(result))
        {
            if (value is Atom { Name: GenStatemProtocolAtoms.StateFunctions or GenStatemProtocolAtoms.HandleEventFunction })
            {
                selected = value;
            }
            else if (Is(value, GenStatemProtocolAtoms.StateEnter))
            {
                enter = true;
            }
            else
            {
                throw GenStatemSource.Failure(
                    context,
                    Term.Tuple(Term.A(GenStatemProtocolAtoms.BadReturnFromCallbackMode), result),
                    GenStatemFunctionNames.CallbackModeResult,
                    GenStatemSourceArities.CallbackModeResult,
                    GenStatemSourceLines.InvalidCallbackModeEntry
                );
            }
        }
        if (selected is null)
        {
            throw GenStatemSource.Failure(
                context,
                Term.Tuple(Term.A(GenStatemProtocolAtoms.BadReturnFromCallbackMode), result),
                GenStatemFunctionNames.CallbackModeResult,
                GenStatemSourceArities.CallbackModeResult,
                GenStatemSourceLines.MissingCallbackMode
            );
        }
        callbackMode = selected;
        stateEnter = enter;
    }

    internal async ValueTask<Term> Run()
    {
        try
        {
            while (true)
            {
                await context.ReduceAsync();
                Term? next = null;
                if (events.First is { } first)
                {
                    next = first.Value;
                    events.RemoveFirst();
                }
                while (next is null && zeroQueue.TryDequeue(out Term? zero))
                {
                    var pair = (TupleTerm)zero;
                    Term type = pair.Items[0];
                    Term reference = pair.Items[1];
                    if (timers.TryGetValue(type, out Term? active) && active is TupleTerm entry && entry.Items[0].Equals(reference))
                    {
                        timers.Remove(type);
                        next = Term.Tuple(type, entry.Items[1]);
                        await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.In), next, State));
                    }
                    else
                    {
                        timers.Remove(reference);
                    }
                }
                if (next is null)
                {
                    Term? message;
                    if (hibernate)
                    {
                        message = await context.ReceiveAsync(t => t, TimeSpan.Zero);
                        if (message is not null)
                        {
                            await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.GarbageCollect);
                        }
                        else
                        {
                            await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.Hibernate);
                            message = await context.ReceiveAsync(t => t);
                        }
                    }
                    else
                    {
                        // Use the runtime's Erlang timeout validation, including large timeouts.
                        message = Is(hibernateAfter, GenStatemProtocolAtoms.Infinity)
                            ? await context.ReceiveAsync(t => t)
                            : await ReceiveAfter();
                    }
                    next = await Message(message!);
                    if (next is null)
                    {
                        continue;
                    }
                    hibernate = false;
                    await Debug(Term.Tuple(Term.A(GenStatemProtocolAtoms.In), next, State));
                }
                else if (hibernate)
                {
                    await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.GarbageCollect);
                }
                var current = (TupleTerm)next;
                currentEvent = next;
                var transition = new GenStatemTransition(State, Data) { Hibernate = hibernate };
                Term result = await StateCallback(transition, current.Items[0], current.Items[1]);
                bool enter = await Result(transition, result, true);
                await Complete(transition, next, enter);
                currentEvent = null;
            }
        }
        catch (ErlangException error) when (!terminating)
        {
            return await Terminate(error);
        }
    }

    private async ValueTask<Term?> ReceiveAfter()
    {
        if (hibernateAfter is not Integer time || time.Value < 0 || time.Value > GenStatemTimeoutLimits.MaximumHibernateAfterMilliseconds)
        {
            throw new ErlangException(GenStatemProtocolAtoms.TimeoutValue);
        }
        Term? result = await context.ReceiveAsync(t => t, TimeSpan.FromMilliseconds((double)time.Value));
        if (result is not null)
        {
            return result;
        }
        await Call(GenStatemDependencyNames.Erlang, GenStatemFunctionNames.Hibernate);

        return await context.ReceiveAsync(t => t);
    }

    private async ValueTask<Term?> Message(Term message)
    {
        if (message is TupleTerm tuple)
        {
            if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.GenCall))
            {
                return Term.Tuple(Term.Tuple(Term.A(GenStatemProtocolAtoms.Call), tuple.Items[1]), tuple.Items[2]);
            }
            if (tuple.Items.Count == 2 && Is(tuple.Items[0], GenStatemProtocolAtoms.GenCast))
            {
                return Term.Tuple(Term.A(GenStatemProtocolAtoms.Cast), tuple.Items[1]);
            }
            if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.Timeout) && tuple.Items[1] is ReferenceTerm)
            {
                if (timers.TryGetValue(tuple.Items[2], out Term? timer) && timer is TupleTerm active && active.Items[0].Equals(tuple.Items[1]))
                {
                    timers.Remove(tuple.Items[2]);

                    return Term.Tuple(tuple.Items[2], active.Items[1]);
                }
                if (timers.Remove(tuple.Items[1]))
                {
                    return null;
                }
            }
            if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.System))
            {
                // Unwind this machine before sys starts its successor. The driver
                // catches only our control transfer; Erlang errors keep their stacks.
                terminating = true;
                throw new GenStatemSystemTransfer(
                    context,
                    [
                    tuple.Items[2],
                    tuple.Items[1],
                    Parent,
                    Term.A(Name),
                    debug,
                    Snapshot(),
                    Boolean(hibernate)
                ]
                );
            }
            if (tuple.Items.Count == 3 && Is(tuple.Items[0], GenStatemProtocolAtoms.EXIT) && tuple.Items[1].Equals(Parent))
            {
                return await Terminate(new ErlangException(tuple.Items[2], ErlangExceptionClasses.Exit));
            }
        }

        return Term.Tuple(Term.A(GenStatemProtocolAtoms.Info), message);
    }

    private async ValueTask<Term> StateCallback(GenStatemTransition transition, Term type, Term content)
    {
        if (!Is(callbackMode, GenStatemProtocolAtoms.StateFunctions))
        {
            using var source = GenStatemSource.Scope(
                context,
                GenStatemFunctionNames.LoopStateCallback,
                GenStatemSourceArities.StateCallback,
                GenStatemSourceLines.HandleEventCallback
            );

            return await Callback(
                GenStatemFunctionNames.HandleEvent,
                type,
                content,
                transition.State,
                transition.Data
            );
        }
        try
        {
            using var source = GenStatemSource.Scope(
                context,
                GenStatemFunctionNames.LoopStateCallback,
                GenStatemSourceArities.StateCallback,
                GenStatemSourceLines.StateFunctionsCallback
            );

            return await Call(
                GenStatemDependencyNames.Erlang,
                GenStatemFunctionNames.Apply,
                modules[0],
                transition.State,
                Term.List(type, content, transition.Data)
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Throw)
        {
            return error.Reason;
        }
    }

    internal static Term Boolean(bool value) => Term.A(value ? GenStatemProtocolAtoms.True : GenStatemProtocolAtoms.False);

    private async ValueTask Debug(Term entry)
    {
        if (debug is Nil)
        {
            return;
        }
        var printer = new FunctionTerm(3, (execution, args) => GenStatemLog.PrintEvent((ProcessContext)execution, args));
        debug = await Call(
            GenStatemDependencyNames.Sys,
            GenStatemFunctionNames.HandleDebug,
            debug,
            printer,
            name,
            entry
        );
    }
}
