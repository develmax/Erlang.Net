// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_server ported to C#.
namespace Erlang;

public static class GenServerModule
{
    public const string Name = GenServerLibraryNames.Module;
    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (GenServerFunctionNames.Start,3), (GenServerFunctionNames.Start,4), (GenServerFunctionNames.StartLink,3), (GenServerFunctionNames.StartLink,4), (GenServerFunctionNames.StartMonitor,3), (GenServerFunctionNames.StartMonitor,4),
        (GenServerFunctionNames.Stop,1), (GenServerFunctionNames.Stop,3), (GenServerFunctionNames.Call,2), (GenServerFunctionNames.Call,3), (GenServerFunctionNames.SendRequest,2), (GenServerFunctionNames.SendRequest,4),
        (GenServerFunctionNames.WaitResponse,2), (GenServerFunctionNames.WaitResponse,3), (GenServerFunctionNames.ReceiveResponse,2), (GenServerFunctionNames.ReceiveResponse,3),
        (GenServerFunctionNames.CheckResponse,2), (GenServerFunctionNames.CheckResponse,3), (GenServerFunctionNames.ReqidsNew,0), (GenServerFunctionNames.ReqidsSize,1), (GenServerFunctionNames.ReqidsAdd,3), (GenServerFunctionNames.ReqidsToList,1),
        (GenServerFunctionNames.Cast,2), (GenServerFunctionNames.Reply,2), (GenServerFunctionNames.Abcast,2), (GenServerFunctionNames.Abcast,3), (GenServerFunctionNames.MultiCall,2), (GenServerFunctionNames.MultiCall,3), (GenServerFunctionNames.MultiCall,4),
        (GenServerFunctionNames.EnterLoop,3), (GenServerFunctionNames.EnterLoop,4), (GenServerFunctionNames.EnterLoop,5), (GenServerFunctionNames.SystemContinue,3), (GenServerFunctionNames.SystemTerminate,4),
        (GenServerFunctionNames.SystemCodeChange,4), (GenServerFunctionNames.SystemGetState,1), (GenServerFunctionNames.SystemReplaceState,2), (GenServerFunctionNames.FormatStatus,2),
        (GenServerFunctionNames.FormatLog,1), (GenServerFunctionNames.FormatLog,2), (GenServerFunctionNames.InitIt,6), (GenServerFunctionNames.BehaviourInfo,1)
    });

    public static void Register(ModuleRegistry registry)
        => registry.InstallPreloaded(preloaded => preloaded.InstallSourceWrappers(target =>
        {
            foreach (var export in Exports)
            {
                string function = export.Name;
                target.Register(
                    Name,
                    function,
                    export.Arity,
                    (context, arguments) => Invoke(context, function, arguments.ToArray())
                );
            }
        }));

    internal static ValueTask<Term> Call(
        ProcessContext c,
        string m,
        string f,
        params Term[] a
    )
        => c.Runtime.Modules.Call(
            c,
            m,
            f,
            a
        );

    internal static bool Is(Term t, string value) => t is Atom atom && atom.Name == value;

    internal static bool List(Term t) => t is Cons or Nil;

    internal static bool ProperList(Term t)
    {
        while (t is Cons cell) t = cell.Tail;

        return t is Nil;
    }

    internal static Atom A(string value) => Term.A(value);

    internal static bool Timeout(Term t, bool absolute = false) => Is(t, GenServerProtocolAtoms.Infinity) || t is Integer n && (absolute || n.Value >= 0);

    internal static MapTerm Map(params (string Key, Term Value)[] entries)
        => new(entries.Select(e => new KeyValuePair<Term, Term>(A(e.Key), e.Value)));

    internal static FunctionTerm External(string module, string function, int arity)
        => new(new ExternalFunctionIdentity(A(module), A(function), arity), (c, a) => Call(
            (ProcessContext)c,
            module,
            function,
            a.ToArray()
        ));

    private static async ValueTask<Term> Invoke(ProcessContext c, string f, Term[] a)
    {
        using var tail = c.EnterSourceTailCall(Name, f);
        switch (f)
        {
            case GenServerFunctionNames.Start or GenServerFunctionNames.StartLink or GenServerFunctionNames.StartMonitor:
                int offset = a.Length - 3;
                if (a[offset] is not Atom || !List(a[^1]) || offset == 1 && a[0] is not TupleTerm)
                    throw GenServerSource.Error(
                        c,
                        f,
                        a,
                        GenServerSource.ApiLine(f, a.Length, true)
                    );

                return await Call(
                    c,
                    GenServerDependencyNames.Gen,
                    GenServerFunctionNames.Start,
                    new Term[] { A(Name), A(f == GenServerFunctionNames.Start ? GenServerProtocolAtoms.Nolink : f == GenServerFunctionNames.StartLink ? GenServerProtocolAtoms.Link : GenServerProtocolAtoms.Monitor) }.Concat(a).ToArray()
                );
            case GenServerFunctionNames.Stop:
                return await Call(
                    c,
                    GenServerDependencyNames.Gen,
                    f,
                    a.Length == 1 ? [a[0], A(GenServerProtocolAtoms.Normal), A(GenServerProtocolAtoms.Infinity)] : a
                );
            case GenServerFunctionNames.Call:
                Term callResult;
                using (GenServerSource.Scope(
                    c,
                    f,
                    a.Length,
                    GenServerSource.ApiLine(f, a.Length)
                ))
                {
                    try
                    {
                        callResult = await Call(
                            c,
                            GenServerDependencyNames.Gen,
                            f,
                            new Term[] { a[0], A(GenServerProtocolAtoms.GenCall), a[1] }.Concat(a.Skip(2)).ToArray()
                        );
                    }
                    catch (ErlangException error)
                    {
                        callResult = error.ExceptionClass == ErlangExceptionClasses.Throw
                            ? error.Reason
                            : Term.Tuple(
                                A(GenServerProtocolAtoms.ExitSignal),
                                error.ExceptionClass == ErlangExceptionClasses.Error ? Term.Tuple(error.Reason, error.StackTraceTerm) : error.Reason
                            );
                    }
                }
                if (callResult is TupleTerm { Items.Count: 2 } callPair)
                {
                    if (Is(callPair.Items[0], GenServerProtocolAtoms.Ok))
                        return callPair.Items[1];
                    if (Is(callPair.Items[0], GenServerProtocolAtoms.ExitSignal))
                        throw GenServerSource.Exit(
                            c,
                            f,
                            a.Length,
                            GenServerSource.ApiLine(f, a.Length, true),
                            Term.Tuple(callPair.Items[1], Term.Tuple(A(Name), A(f), Term.List(a)))
                        );
                }
                throw GenServerSource.Failure(
                    c,
                    Term.Tuple(A(GenServerProtocolAtoms.CaseClause), callResult),
                    f,
                    a.Length,
                    GenServerSource.ApiLine(f, a.Length)
                );
            case GenServerFunctionNames.SendRequest:
                return await Delegate(
                    c,
                    f,
                    a,
                    new Term[] { a[0], A(GenServerProtocolAtoms.GenCall), a[1] }.Concat(a.Skip(2)).ToArray()
                );
            case GenServerFunctionNames.WaitResponse or GenServerFunctionNames.ReceiveResponse or GenServerFunctionNames.CheckResponse or GenServerFunctionNames.ReqidsSize or GenServerFunctionNames.ReqidsAdd or GenServerFunctionNames.ReqidsToList:
                return await Delegate(
                    c,
                    f,
                    a,
                    a
                );
            case GenServerFunctionNames.ReqidsNew or GenServerFunctionNames.Reply:
                return await Call(
                    c,
                    GenServerDependencyNames.Gen,
                    f,
                    a
                );
            case GenServerFunctionNames.Cast:
                return await Cast(c, a[0], a[1]);
            case GenServerFunctionNames.Abcast:
                if (a.Length == 2)
                {
                    if (a[0] is not Atom) throw GenServerSource.Clause(
                        c,
                        f,
                        a,
                        GenServerSourceLines.AbcastLine1870
                    );

                    return await Invoke(
                        c,
                        f,
                        [new Cons(
                            await Call(c,GenServerDependencyNames.Erlang,GenServerFunctionNames.Node),
                            await Call(c,GenServerDependencyNames.Erlang,GenServerFunctionNames.Nodes)
                        ),a[0],a[1]]
                    );
                }
                if (!List(a[0]) || a[1] is not Atom) throw GenServerSource.Clause(
                    c,
                    f,
                    a,
                    GenServerSourceLines.AbcastLine1893
                );
                Term nodes = a[0];
                while (nodes is Cons cell)
                {
                    if (cell.Head is not Atom) throw GenServerSource.Clause(
                        c,
                        GenServerFunctionNames.DoAbcast,
                        [nodes, a[1], Term.Tuple(A(GenServerProtocolAtoms.GenCast), a[2])],
                        GenServerSourceLines.DoAbcastLine1896
                    );
                    await Send(c, Term.Tuple(a[1], cell.Head), Term.Tuple(A(GenServerProtocolAtoms.GenCast), a[2]));
                    nodes = cell.Tail;
                }

                if (nodes is not Nil) throw GenServerSource.Clause(
                    c,
                    GenServerFunctionNames.DoAbcast,
                    [nodes, a[1], Term.Tuple(A(GenServerProtocolAtoms.GenCast), a[2])],
                    GenServerSourceLines.DoAbcastLine1896
                );

                return A(GenServerProtocolAtoms.Abcast);
            case GenServerFunctionNames.MultiCall:
                return await MultiCall(c, a);
            case GenServerFunctionNames.InitIt: return await GenServerMachine.Initialize(c, a);
            case GenServerFunctionNames.EnterLoop: return await GenServerMachine.EnterLoop(c, a);
            case GenServerFunctionNames.SystemContinue:
                var resumed = GenServerMachine.StateList(
                    c,
                    f,
                    a,
                    a[2],
                    GenServerSourceLines.SystemContinueLine2656
                );
                if (resumed[0] is not TupleTerm { Items.Count: GenServerRecordLayout.ServerData.Arity } resumeData || !Is(resumeData.Items[0], GenServerProtocolAtoms.ServerData))
                    throw GenServerSource.Clause(
                        c,
                        f,
                        a,
                        GenServerSourceLines.SystemContinueLine2656
                    );
                var machine = GenServerMachine.Restore(c, a[2], a[1]);
                if (!machine.Parent.Equals(a[0])) throw GenServerSource.Clause(
                    c,
                    f,
                    a,
                    GenServerSourceLines.SystemContinueLine2656
                );

                return await machine.Run();
            case GenServerFunctionNames.SystemTerminate:
                var terminating = GenServerMachine.StateList(
                    c,
                    f,
                    a,
                    a[3],
                    GenServerSourceLines.SystemTerminateLine2663
                );
                if (terminating[0] is not TupleTerm { Items.Count: GenServerRecordLayout.ServerData.Arity } terminateData || !Is(terminateData.Items[0], GenServerProtocolAtoms.ServerData))
                {
                    using var terminateScope = GenServerSource.Scope(
                        c,
                        GenServerFunctionNames.Terminate,
                        9,
                        GenServerSourceLines.TerminateCallback
                    );
                    throw GenServerSource.Clause(
                        c,
                        GenServerFunctionNames.TryTerminate,
                        [terminating[0], terminating[1], a[0]],
                        GenServerSourceLines.TryTerminate
                    );
                }

                return await GenServerMachine.Restore(c, a[3], a[2]).Terminate(new ErlangException(a[0], ErlangExceptionClasses.Exit));
            case GenServerFunctionNames.SystemCodeChange: return await GenServerMachine.CodeChange(c, a);
            case GenServerFunctionNames.SystemGetState:
                var state = GenServerMachine.StateList(
                    c,
                    f,
                    a,
                    a[0],
                    GenServerSourceLines.SystemGetStateLine2674
                );

                return Term.Tuple(A(GenServerProtocolAtoms.Ok), state[1]);
            case GenServerFunctionNames.SystemReplaceState:
                var snapshot = GenServerMachine.StateList(
                    c,
                    f,
                    a,
                    a[1],
                    GenServerSourceLines.SystemReplaceStateLine2678
                );
                using (GenServerSource.Scope(
                    c,
                    f,
                    2,
                    GenServerSourceLines.SystemReplaceStateLine2679
                ))
                {
                    if (a[0] is not FunctionTerm transform) throw GenServerSource.Failure(
                        c,
                        Term.Tuple(A(GenServerProtocolAtoms.Badfun), a[0]),
                        f,
                        2,
                        GenServerSourceLines.SystemReplaceStateLine2679
                    );
                    if (transform.Arity != 1)
                        throw GenServerSource.Failure(
                            c,
                            Term.Tuple(A(GenServerProtocolAtoms.BadArity), Term.Tuple(transform, Term.List(snapshot[1]))),
                            f,
                            2,
                            GenServerSourceLines.SystemReplaceStateLine2679
                        );
                    Term value = await transform.Invoke(c, [snapshot[1]]);
                    snapshot[1] = value;

                    return Term.Tuple(A(GenServerProtocolAtoms.Ok), value, Term.List(snapshot));
                }
            case GenServerFunctionNames.FormatStatus: return await GenServerMachine.FormatStatus(c, a);
            case GenServerFunctionNames.FormatLog: return await GenServerLog.Format(c, a);
            case GenServerFunctionNames.BehaviourInfo:
                (string Name, int Arity)[] all = [(GenServerFunctionNames.Init, 1), (GenServerFunctionNames.HandleCall, 3), (GenServerFunctionNames.HandleCast, 2), (GenServerFunctionNames.HandleInfo, 2), (GenServerFunctionNames.HandleContinue, 2), (GenServerFunctionNames.Terminate, 2), (GenServerFunctionNames.CodeChange, 3), (GenServerFunctionNames.FormatStatus, 2), (GenServerFunctionNames.FormatStatus, 1)];
                (string Name, int Arity)[] optional = [(GenServerFunctionNames.HandleInfo, 2), (GenServerFunctionNames.HandleContinue, 2), (GenServerFunctionNames.Terminate, 2), (GenServerFunctionNames.CodeChange, 3), (GenServerFunctionNames.FormatStatus, 1), (GenServerFunctionNames.FormatStatus, 2)];
                var selected = Is(a[0], GenServerProtocolAtoms.Callbacks) ? all : Is(a[0], GenServerProtocolAtoms.OptionalCallbacks) ? optional : null;
                if (selected is null) throw GenServerSource.Clause(
                    c,
                    GenServerFunctionNames.InlinedBehaviourInfo1,
                    a,
                    0
                );

                return Cons.From(selected.Select(x => Term.Tuple(A(x.Name), Term.I(x.Arity))));
            default: throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    private static async ValueTask<Term> Delegate(
        ProcessContext c,
        string f,
        Term[] original,
        Term[] args
    )
    {
        try
        {
            using var scope = GenServerSource.Scope(
                c,
                f,
                original.Length,
                GenServerSource.ApiLine(f, original.Length)
            );

            return await Call(
                c,
                GenServerDependencyNames.Gen,
                f,
                args
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && Is(error.Reason, GenServerProtocolAtoms.Badarg))
        {
            throw GenServerSource.Error(
                c,
                f,
                original,
                GenServerSource.ApiLine(f, original.Length, true)
            );
        }
    }

    internal static async ValueTask<Term> Send(ProcessContext c, Term destination, Term message)
    {
        try
        {
            await Call(
                c,
                GenServerDependencyNames.Erlang,
                GenServerFunctionNames.Send,
                destination,
                message
            );
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error) { }

        return A(GenServerProtocolAtoms.Ok);
    }

    private static async ValueTask<Term> Cast(ProcessContext c, Term destination, Term request)
    {
        Term msg = Term.Tuple(A(GenServerProtocolAtoms.GenCast), request);
        if (destination is TupleTerm t)
        {
            if (t.Items.Count == 2 && Is(t.Items[0], GenServerProtocolAtoms.Global) || t.Items.Count == 3 && Is(t.Items[0], GenServerProtocolAtoms.Via))
            {
                try
                {
                    if (t.Items.Count == 2) await Call(
                        c,
                        GenServerDependencyNames.Global,
                        GenServerFunctionNames.Send,
                        t.Items[1],
                        msg
                    );
                    else await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Apply,
                        t.Items[1],
                        A(GenServerProtocolAtoms.Send),
                        Term.List(t.Items[2], msg)
                    );
                }
                catch (ErlangException) { }

                return A(GenServerProtocolAtoms.Ok);
            }
            if (t.Items.Count == 2 && t.Items[0] is Atom && t.Items[1] is Atom) return await Send(c, destination, msg);
        }
        if (destination is Atom or Pid) return await Send(c, destination, msg);
        throw GenServerSource.Clause(
            c,
            GenServerFunctionNames.Cast,
            [destination, request],
            GenServerSourceLines.CastLine1809
        );
    }

    private static async ValueTask<Term> MultiCall(ProcessContext c, Term[] a)
    {
        if (a.Length == 2)
        {
            if (a[0] is not Atom) throw GenServerSource.Clause(
                c,
                GenServerFunctionNames.MultiCall,
                a,
                GenServerSourceLines.MultiCallLine1928
            );

            return await MultiCall(
                c,
                [new Cons(
                    await Call(c,GenServerDependencyNames.Erlang,GenServerFunctionNames.Node),
                    await Call(c,GenServerDependencyNames.Erlang,GenServerFunctionNames.Nodes)
                ),a[0],a[1],A(GenServerProtocolAtoms.Infinity)]
            );
        }
        if (a.Length == 3)
        {
            if (!List(a[0]) || a[1] is not Atom) throw GenServerSource.Clause(
                c,
                GenServerFunctionNames.MultiCall,
                a,
                GenServerSourceLines.MultiCallLine1943
            );

            return await MultiCall(c, [a[0], a[1], a[2], A(GenServerProtocolAtoms.Infinity)]);
        }
        if (!List(a[0]) || a[1] is not Atom || !Timeout(a[3])) throw GenServerSource.Clause(
            c,
            GenServerFunctionNames.MultiCall,
            a,
            GenServerSourceLines.MultiCallLine2005
        );
        Term alias = await Call(c, GenServerDependencyNames.Erlang, GenServerFunctionNames.Alias);
        Term timer = A(GenServerProtocolAtoms.Undefined);
        var requests = new List<(Term Node, Term Monitor)>();
        var replies = new List<Term>();
        var bad = new List<Term>();
        try
        {
            if (!Is(a[3], GenServerProtocolAtoms.Infinity)) timer = await Call(
                c,
                GenServerDependencyNames.Erlang,
                GenServerFunctionNames.StartTimer,
                a[3],
                c.Self,
                alias
            );
            using var sendScope = GenServerSource.Scope(
                c,
                GenServerFunctionNames.MultiCall,
                4,
                GenServerSourceLines.MultiSend
            );
            Term pending = a[0];
            while (pending is not Nil)
            {
                Term node = pending is Cons cell ? cell.Head : pending;
                if (node is not Atom || pending is not Cons)
                {
                    await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Unalias,
                        alias
                    );
                    await CancelMultiTimer(c, timer, alias);
                    await Collect(true);
                    throw GenServerSource.Failure(
                        c,
                        GenServerFunctionNames.McSend,
                        6,
                        GenServerSourceLines.McSendLine2050,
                        A(GenServerProtocolAtoms.Badarg)
                    );
                }
                Term destination = Term.Tuple(a[1], node);
                Term monitor;
                try
                {
                    monitor = await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Monitor,
                        A(GenServerProtocolAtoms.Process),
                        destination,
                        Term.List(Term.Tuple(A(GenServerProtocolAtoms.Tag), alias))
                    );
                }
                catch (ErlangException e) when (e.ExceptionClass == ErlangExceptionClasses.Error && Is(e.Reason, GenServerProtocolAtoms.Badarg))
                {
                    monitor = await Call(c, GenServerDependencyNames.Erlang, GenServerFunctionNames.MakeRef);
                    await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Send,
                        alias,
                        Term.Tuple(
                            alias,
                            monitor,
                            A(GenServerProtocolAtoms.Process),
                            destination,
                            A(GenServerProtocolAtoms.Noconnection)
                        )
                    );
                }
                Term tag = new Cons(new Cons(A(GenServerProtocolAtoms.Alias), alias), monitor);
                try
                {
                    await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Send,
                        destination,
                        Term.Tuple(A(GenServerProtocolAtoms.GenCall), Term.Tuple(c.Self, tag), a[2]),
                        Term.List(A(GenServerProtocolAtoms.Noconnect))
                    );
                }
                catch (ErlangException) { }
                requests.Insert(0, (node, monitor));
                pending = ((Cons)pending).Tail;
            }
            sendScope.Dispose();
            if (!await Collect(false))
                await CancelMultiTimer(c, timer, alias);

            return Term.Tuple(Cons.From(replies), Cons.From(bad));
        }
        finally
        {
            await Call(
                c,
                GenServerDependencyNames.Erlang,
                GenServerFunctionNames.Unalias,
                alias
            );
        }

        async ValueTask<bool> Collect(bool timedOut)
        {
            foreach (var (node, monitor) in requests)
            {
                if (timedOut) await Call(
                    c,
                    GenServerDependencyNames.Erlang,
                    GenServerFunctionNames.Demonitor,
                    monitor
                );
                Term tag = new Cons(new Cons(A(GenServerProtocolAtoms.Alias), alias), monitor);
                Term? received = await c.ReceiveAsync(
                    t => t is TupleTerm pair && (pair.Items.Count == 2 && pair.Items[0].Equals(tag)
                    || pair.Items.Count == 5 && pair.Items[0].Equals(alias) && pair.Items[1].Equals(monitor)
                    || !timedOut && pair.Items.Count == 3 && Is(pair.Items[0], GenServerProtocolAtoms.Timeout) && pair.Items[1].Equals(timer) && pair.Items[2].Equals(alias)) ? t : null,
                    timedOut ? TimeSpan.Zero : null
                );
                if (received is TupleTerm { Items.Count: 3 })
                {
                    timedOut = true;
                    await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Unalias,
                        alias
                    );
                    await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Demonitor,
                        monitor
                    );
                    received = await c.ReceiveAsync(
                        t => t is TupleTerm pair && (pair.Items.Count == 2 && pair.Items[0].Equals(tag)
                        || pair.Items.Count == 5 && pair.Items[0].Equals(alias) && pair.Items[1].Equals(monitor)) ? t : null,
                        TimeSpan.Zero
                    );
                }
                if (received is TupleTerm { Items.Count: 2 } reply)
                {
                    if (!timedOut) await Call(
                        c,
                        GenServerDependencyNames.Erlang,
                        GenServerFunctionNames.Demonitor,
                        monitor,
                        Term.List(A(GenServerProtocolAtoms.Flush))
                    );
                    replies.Insert(0, Term.Tuple(node, reply.Items[1]));
                }
                else bad.Insert(0, node);
            }

            return timedOut;
        }
    }

    private static async ValueTask CancelMultiTimer(ProcessContext c, Term timer, Term alias)
    {
        if (Is(timer, GenServerProtocolAtoms.Undefined)) return;
        if (Is(await Call(
            c,
            GenServerDependencyNames.Erlang,
            GenServerFunctionNames.CancelTimer,
            timer
        ), GenServerProtocolAtoms.False))
            await c.ReceiveAsync(t => t is TupleTerm { Items.Count: 3 } msg && Is(msg.Items[0], GenServerProtocolAtoms.Timeout) && msg.Items[1].Equals(timer) && msg.Items[2].Equals(alias) ? t : null);
    }
}
