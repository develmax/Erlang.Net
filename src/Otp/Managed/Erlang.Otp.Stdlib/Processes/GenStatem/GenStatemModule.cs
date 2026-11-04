// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 2016-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_statem library dispatch adapted to the .NET runtime.
namespace Erlang;

public static class GenStatemModule
{
    public const string Name = GenStatemLibraryNames.Module;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (GenStatemFunctionNames.Start, 3), (GenStatemFunctionNames.Start, 4), (GenStatemFunctionNames.StartLink, 3), (GenStatemFunctionNames.StartLink, 4),
        (GenStatemFunctionNames.StartMonitor, 3), (GenStatemFunctionNames.StartMonitor, 4), (GenStatemFunctionNames.Stop, 1), (GenStatemFunctionNames.Stop, 3),
        (GenStatemFunctionNames.Cast, 2), (GenStatemFunctionNames.Call, 2), (GenStatemFunctionNames.Call, 3), (GenStatemFunctionNames.SendRequest, 2), (GenStatemFunctionNames.SendRequest, 4),
        (GenStatemFunctionNames.WaitResponse, 1), (GenStatemFunctionNames.WaitResponse, 2), (GenStatemFunctionNames.WaitResponse, 3),
        (GenStatemFunctionNames.ReceiveResponse, 1), (GenStatemFunctionNames.ReceiveResponse, 2), (GenStatemFunctionNames.ReceiveResponse, 3),
        (GenStatemFunctionNames.CheckResponse, 2), (GenStatemFunctionNames.CheckResponse, 3), (GenStatemFunctionNames.ReqidsNew, 0), (GenStatemFunctionNames.ReqidsSize, 1),
        (GenStatemFunctionNames.ReqidsAdd, 3), (GenStatemFunctionNames.ReqidsToList, 1), (GenStatemFunctionNames.EnterLoop, 4), (GenStatemFunctionNames.EnterLoop, 5), (GenStatemFunctionNames.EnterLoop, 6),
        (GenStatemFunctionNames.Reply, 1), (GenStatemFunctionNames.Reply, 2), (GenStatemFunctionNames.InitIt, 6), (GenStatemFunctionNames.SystemContinue, 3), (GenStatemFunctionNames.SystemTerminate, 4),
        (GenStatemFunctionNames.SystemCodeChange, 4), (GenStatemFunctionNames.SystemGetState, 1), (GenStatemFunctionNames.SystemReplaceState, 2),
        (GenStatemFunctionNames.FormatStatus, 2), (GenStatemFunctionNames.FormatLog, 1), (GenStatemFunctionNames.FormatLog, 2), (GenStatemFunctionNames.BehaviourInfo, 1)
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
                    (context, arguments) => Invoke(context, function, arguments)
                );
            }
        }));

    public static async ValueTask<Term> Invoke(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        using var tail = context.EnterSourceTailCall(Name, function);
        if (!Exports.Contains((function, arguments.Count)))
        {
            throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
        try
        {
            if (!GenStatemExecution.IsActive(context) && function is GenStatemFunctionNames.InitIt or GenStatemFunctionNames.EnterLoop or GenStatemFunctionNames.SystemContinue)
            {
                ValueTask<Term> execution = GenStatemExecution.Run(context, function, arguments);
                arguments = Array.Empty<Term>();

                return await execution;
            }

            return await InvokeCore(context, function, arguments);
        }
        catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
            && GenStatemMachine.Is(error.Reason, ErlangErrorReasons.BadArgument)
            && function is not (GenStatemFunctionNames.Start or GenStatemFunctionNames.StartLink or GenStatemFunctionNames.StartMonitor or GenStatemFunctionNames.Call)
            && GenStatemSource.ErrorLine(function, arguments.Count) != 0)
        {
            throw GenStatemSource.Error(
                context,
                function,
                arguments,
                GenStatemSource.ErrorLine(function, arguments.Count)
            );
        }
    }

    internal static async ValueTask<Term> InvokeCore(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Term[] a = arguments.ToArray();
        switch (function)
        {
            case GenStatemFunctionNames.Start or GenStatemFunctionNames.StartLink or GenStatemFunctionNames.StartMonitor when a.Length is 3 or 4:
                {
                    int offset = a.Length - 3;
                    if (a[offset] is not Atom || !GenStatemMachine.IsList(a[^1]) || offset == 1 && a[0] is not TupleTerm)
                    {
                        throw GenStatemSource.Error(
                            context,
                            function,
                            a,
                            GenStatemSource.ErrorLine(function, a.Length)
                        );
                    }
                    Term link = Term.A(function == GenStatemFunctionNames.Start ? GenStatemProtocolAtoms.Nolink : function == GenStatemFunctionNames.StartLink ? GenStatemProtocolAtoms.Link : GenStatemProtocolAtoms.Monitor);

                    return await Call(
                        context,
                        GenStatemDependencyNames.Gen,
                        GenStatemFunctionNames.Start,
                        new Term[] { Term.A(Name), link }.Concat(a).ToArray()
                    );
                }
            case GenStatemFunctionNames.Stop when a.Length is 1 or 3:
                return await Call(
                    context,
                    GenStatemDependencyNames.Gen,
                    function,
                    a.Length == 1 ? [a[0], Term.A(GenStatemProtocolAtoms.Normal), Term.A(GenStatemProtocolAtoms.Infinity)] : a
                );
            case GenStatemFunctionNames.Cast when a.Length == 2:
                return await Cast(context, a[0], a[1]);
            case GenStatemFunctionNames.Call when a.Length is 2 or 3:
                {
                    Term timeout = a.Length == 2 ? Term.A(GenStatemProtocolAtoms.Infinity) : a[2];
                    Term time = timeout;
                    if (timeout is TupleTerm { Items.Count: 2 } tagged)
                    {
                        if (tagged.Items[0] is not Atom { Name: GenStatemProtocolAtoms.DirtyTimeout or GenStatemProtocolAtoms.CleanTimeout })
                        {
                            throw GenStatemSource.Error(
                                context,
                                function,
                                [a[0], a[1], timeout],
                                GenStatemSourceLines.CallTimeoutError
                            );
                        }
                        time = tagged.Items[1];
                    }
                    try
                    {
                        using var scope = GenStatemSource.Scope(
                            context,
                            GenStatemFunctionNames.Call,
                            3,
                            GenStatemSourceLines.CallDelegation
                        );
                        var result = (TupleTerm)await Call(
                            context,
                            GenStatemDependencyNames.Gen,
                            GenStatemFunctionNames.Call,
                            a[0],
                            Term.A(GenStatemProtocolAtoms.GenCall),
                            a[1],
                            time
                        );

                        return result.Items[1];
                    }
                    catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit)
                    {
                        throw new ErlangException(Term.Tuple(error.Reason, Term.Tuple(Term.A(Name), Term.A(function), Term.List(a[0], a[1], timeout))), error.ExceptionClass)
                        {
                            StackTraceTerm = error.StackTraceTerm,
                            ExplicitStackTrace = true
                        };
                    }
                }
            case GenStatemFunctionNames.SendRequest when a.Length is 2 or 4:
                using (GenStatemSource.Scope(
                    context,
                    function,
                    a.Length,
                    a.Length == 2 ? GenStatemSourceLines.SendRequestDelegation : GenStatemSourceLines.SendRequestCollectionDelegation
                ))
                {
                    return await Call(
                        context,
                        GenStatemDependencyNames.Gen,
                        function,
                        new[] { a[0], Term.A(GenStatemProtocolAtoms.GenCall), a[1] }.Concat(a.Skip(2)).ToArray()
                    );
                }
            case GenStatemFunctionNames.WaitResponse or GenStatemFunctionNames.ReceiveResponse when a.Length == 1:
                return await Invoke(context, function, [a[0], Term.A(GenStatemProtocolAtoms.Infinity)]);
            case GenStatemFunctionNames.WaitResponse or GenStatemFunctionNames.ReceiveResponse or GenStatemFunctionNames.CheckResponse or GenStatemFunctionNames.ReqidsNew or GenStatemFunctionNames.ReqidsSize or GenStatemFunctionNames.ReqidsAdd or GenStatemFunctionNames.ReqidsToList:
                if (function == GenStatemFunctionNames.ReqidsNew)
                {
                    return await Call(
                        context,
                        GenStatemDependencyNames.Gen,
                        function,
                        a
                    );
                }
                int line = GenStatemSource.CallLine(function, a.Length);
                using (GenStatemSource.Scope(
                    context,
                    function,
                    a.Length,
                    line
                ))
                {
                    return await Call(
                        context,
                        GenStatemDependencyNames.Gen,
                        function,
                        a
                    );
                }
            case GenStatemFunctionNames.Reply when a.Length == 2:
                return await Call(
                    context,
                    GenStatemDependencyNames.Gen,
                    function,
                    a
                );
            case GenStatemFunctionNames.Reply when a.Length == 1:
                {
                    foreach (Term reply in GenStatemMachine.Listify(a[0]))
                    {
                        if (reply is not TupleTerm { Items.Count: 3 } tuple || !GenStatemMachine.Is(tuple.Items[0], GenStatemProtocolAtoms.Reply))
                        {
                            throw GenStatemSource.Error(
                                context,
                                GenStatemFunctionNames.Reply,
                                a,
                                GenStatemSourceLines.ReplyClause,
                                ErlangErrorReasons.FunctionClause
                            );
                        }
                        await Call(
                            context,
                            GenStatemDependencyNames.Gen,
                            GenStatemFunctionNames.Reply,
                            tuple.Items[1],
                            tuple.Items[2]
                        );
                    }

                    return Term.A(GenStatemProtocolAtoms.Ok);
                }
            case GenStatemFunctionNames.InitIt when a.Length == 6:
                return await GenStatemMachine.Initialize(context, a);
            case GenStatemFunctionNames.EnterLoop when a.Length is >= 4 and <= 6:
                return await GenStatemMachine.EnterLoop(context, a);
            case GenStatemFunctionNames.SystemContinue when a.Length == 3:
                {
                    var machine = GenStatemMachine.Restore(context, a[2], a[1]);
                    machine.Parent = a[0];

                    return await machine.Run();
                }
            case GenStatemFunctionNames.SystemTerminate when a.Length == 4:
                {
                    var machine = GenStatemMachine.Restore(context, a[3], a[2]);
                    machine.Parent = a[1];

                    return await machine.Terminate(new ErlangException(a[0], ErlangExceptionClasses.Exit));
                }
            case GenStatemFunctionNames.SystemCodeChange when a.Length == 4:
                return await GenStatemMachine.CodeChange(context, a);
            case GenStatemFunctionNames.SystemGetState when a.Length == 1:
                return Term.Tuple(
                    Term.A(GenStatemProtocolAtoms.Ok),
                    SystemState(
                        context,
                        function,
                        a,
                        a[0],
                        GenStatemSourceLines.SystemGetState
                    ).State.Items[GenStatemRecordLayout.State.StateData]
                );
            case GenStatemFunctionNames.SystemReplaceState when a.Length == 2:
                {
                    var records = SystemState(
                        context,
                        function,
                        a,
                        a[1],
                        GenStatemSourceLines.SystemReplaceStateClause
                    );
                    Term value;
                    using (GenStatemSource.Scope(
                        context,
                        function,
                        2,
                        GenStatemSourceLines.SystemReplaceStateFunction
                    ))
                    {
                        if (a[0] is not FunctionTerm transform)
                        {
                            throw GenStatemSource.Failure(
                                context,
                                Term.Tuple(Term.A(GenStatemProtocolAtoms.Badfun), a[0]),
                                function,
                                2,
                                GenStatemSourceLines.SystemReplaceStateFunction
                            );
                        }
                        value = await transform.Invoke(context, [records.State.Items[GenStatemRecordLayout.State.StateData]]);
                    }
                    Term[] fields = records.State.Items.ToArray();
                    fields[GenStatemRecordLayout.State.StateData] = value;

                    return Term.Tuple(
                        Term.A(GenStatemProtocolAtoms.Ok),
                        value,
                        Term.Tuple(records.Pair.Items[GenStatemRecordLayout.SystemState.Parameters], new TupleTerm(fields))
                    );
                }
            case GenStatemFunctionNames.FormatStatus when a.Length == 2:
                return await GenStatemMachine.FormatStatus(context, a[0], a[1]);
            case GenStatemFunctionNames.FormatLog when a.Length is 1 or 2:
                return await GenStatemLog.Format(context, a);
            case GenStatemFunctionNames.BehaviourInfo when a.Length == 1:
                if (a[0] is not Atom { Name: GenStatemProtocolAtoms.Callbacks or GenStatemProtocolAtoms.OptionalCallbacks })
                {
                    throw GenStatemSource.Error(
                        context,
                        GenStatemFunctionNames.InlinedBehaviourInfo1,
                        a,
                        GenStatemSourceLines.InlineFrame,
                        ErlangErrorReasons.FunctionClause
                    );
                }

                return GenStatemMachine.BehaviourInfo(a[0]);
            default:
                throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
        }
    }

    internal static ValueTask<Term> Call(
        ProcessContext context,
        string module,
        string function,
        params Term[] arguments
    )
        => context.Runtime.Modules.Call(
            context,
            module,
            function,
            arguments
        );

    private static async ValueTask<Term> Cast(ProcessContext context, Term destination, Term message)
    {
        Term wrapped = Term.Tuple(Term.A(GenStatemProtocolAtoms.GenCast), message);
        bool valid = destination is Pid or Atom;
        try
        {
            if (destination is TupleTerm tuple)
            {
                if (tuple.Items.Count == 2 && GenStatemMachine.Is(tuple.Items[0], GenStatemProtocolAtoms.Global))
                {
                    valid = true;
                    await Call(
                        context,
                        GenStatemDependencyNames.Global,
                        GenStatemFunctionNames.Send,
                        tuple.Items[1],
                        wrapped
                    );

                    return Term.A(GenStatemProtocolAtoms.Ok);
                }
                if (tuple.Items.Count == 3 && GenStatemMachine.Is(tuple.Items[0], GenStatemProtocolAtoms.Via))
                {
                    valid = true;
                    await Call(
                        context,
                        GenStatemDependencyNames.Erlang,
                        GenStatemFunctionNames.Apply,
                        tuple.Items[1],
                        Term.A(GenStatemProtocolAtoms.Send),
                        Term.List(tuple.Items[2], wrapped)
                    );

                    return Term.A(GenStatemProtocolAtoms.Ok);
                }
                valid = tuple.Items.Count == 2 && tuple.Items[0] is Atom && tuple.Items[1] is Atom;
            }
            if (valid)
            {
                await Call(
                    context,
                    GenStatemDependencyNames.Erlang,
                    GenStatemFunctionNames.Send,
                    destination,
                    wrapped
                );
            }
        }
        catch (ErlangException error) when (destination is TupleTerm t && t.Items[0] is Atom { Name: GenStatemDependencyNames.Global or GenStatemProtocolAtoms.Via }
            || valid && error.ExceptionClass == ErlangExceptionClasses.Error)
        {
        }
        if (!valid)
        {
            throw GenStatemSource.Error(
                context,
                GenStatemFunctionNames.Cast,
                [destination, message],
                GenStatemSourceLines.CastClause,
                ErlangErrorReasons.FunctionClause
            );
        }

        return Term.A(GenStatemProtocolAtoms.Ok);
    }

    private static (TupleTerm Pair, TupleTerm State) SystemState(
        ProcessContext context,
        string function,
        Term[] arguments,
        Term value,
        int line
    )
    {
        if (value is TupleTerm { Items.Count: GenStatemRecordLayout.SystemState.Arity } pair && pair.Items[GenStatemRecordLayout.SystemState.State] is TupleTerm { Items.Count: GenStatemRecordLayout.State.Arity } state
            && GenStatemMachine.Is(state.Items[0], GenStatemProtocolAtoms.State))
        {
            return (pair, state);
        }
        throw GenStatemSource.Error(
            context,
            function,
            arguments,
            line,
            ErlangErrorReasons.FunctionClause
        );
    }
}
