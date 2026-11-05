// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2025. All Rights Reserved.
// Modified: complete OTP-29.1.1 sys dispatcher adapted to the .NET runtime.
namespace Erlang;

public static partial class SysModule
{
    public const string Name = SysLibraryNames.Module;

    public static IReadOnlyList<(string Name, int Arity)> Exports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (SysApiNames.Suspend, 1), (SysApiNames.Suspend, 2), (SysApiNames.Resume, 1), (SysApiNames.Resume, 2),
        (SysApiNames.GetStatus, 1), (SysApiNames.GetStatus, 2), (SysApiNames.GetState, 1), (SysApiNames.GetState, 2),
        (SysApiNames.ReplaceState, 2), (SysApiNames.ReplaceState, 3), (SysApiNames.ChangeCode, 4), (SysApiNames.ChangeCode, 5),
        (SysApiNames.Terminate, 2), (SysApiNames.Terminate, 3), (SysApiNames.Log, 2), (SysApiNames.Log, 3),
        (SysApiNames.Trace, 2), (SysApiNames.Trace, 3), (SysApiNames.Statistics, 2), (SysApiNames.Statistics, 3),
        (SysApiNames.LogToFile, 2), (SysApiNames.LogToFile, 3), (SysApiNames.NoDebug, 1), (SysApiNames.NoDebug, 2),
        (SysApiNames.Install, 2), (SysApiNames.Install, 3), (SysApiNames.Remove, 2), (SysApiNames.Remove, 3),
        (SysApiNames.HandleSystemMsg, 6), (SysApiNames.HandleSystemMsg, 7), (SysApiNames.HandleDebug, 4),
        (SysApiNames.PrintLog, 1), (SysApiNames.GetLog, 1), (SysApiNames.GetDebug, 3), (SysApiNames.DebugOptions, 1), (SysApiNames.SuspendLoopHib, 6), (SysApiNames.BehaviourInfo, 1)
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
        Term[] a = arguments.ToArray();
        switch (function)
        {
            case SysApiNames.BehaviourInfo:
                if (Is(a[0], SysProtocolNames.OptionalCallbacks))
                {
                    return Nil.Value;
                }
                if (Is(a[0], SysProtocolNames.Callbacks))
                {
                    return Term.List(
                        Term.Tuple(A(SysCallbackNames.SystemCodeChange), Term.I(4)),
                        Term.Tuple(A(SysCallbackNames.SystemContinue), Term.I(3)),
                        Term.Tuple(A(SysCallbackNames.SystemGetState), Term.I(1)),
                        Term.Tuple(A(SysCallbackNames.SystemReplaceState), Term.I(2)),
                        Term.Tuple(A(SysCallbackNames.SystemTerminate), Term.I(4))
                    );
                }
                throw SysSource.Error(
                    context,
                    SysSourceNames.InlinedBehaviourInfo,
                    a,
                    0,
                    ErlangErrorReasons.FunctionClause
                );
            case SysApiNames.HandleSystemMsg:
                return await HandleSystem(
                    context,
                    A(SysProtocolNames.Running),
                    a[0],
                    a[1],
                    a[2],
                    a[3],
                    a[4],
                    a[5],
                    a.Length == 7 ? a[6] : A(SysProtocolNames.False)
                );
            case SysApiNames.SuspendLoopHib:
                return await Suspend(
                    context,
                    a[0],
                    a[1],
                    a[2],
                    a[3],
                    a[4],
                    a[5],
                    true
                );
            case SysApiNames.HandleDebug:
                return await HandleDebug(
                    context,
                    a[0],
                    a[1],
                    a[2],
                    a[3]
                );
            case SysApiNames.DebugOptions:
                return await DebugOptions(context, a[0]);
            case SysApiNames.GetDebug:
                using (SysSource.Scope(
                    context,
                    SysSourceNames.GetDebug2,
                    3,
                    SysSourceLines.GetDebugSearch
                ))
                    return await Call(
                        context,
                        SysDependencyNames.Lists,
                        SysDependencyFunctions.Keysearch,
                        a[0],
                        Term.I(1),
                        a[1]
                    ) is TupleTerm { Items: [_, TupleTerm { Items: [var item, var data] }] } && item.Equals(a[0]) ? data : a[2];
            case SysApiNames.GetLog:
                using (SysSource.Scope(
                    context,
                    SysApiNames.GetLog,
                    1,
                    SysSourceLines.GetLogItems
                ))
                    return GetLog(context, a[0]);
            case SysApiNames.PrintLog:
                return await PrintLog(context, a[0]);
        }
        int baseArity = function switch
        {
            SysApiNames.ChangeCode => 4,
            SysApiNames.ReplaceState or SysApiNames.Terminate or SysApiNames.Log or SysApiNames.Trace or SysApiNames.Statistics or SysApiNames.LogToFile or SysApiNames.Install or SysApiNames.Remove => 2,
            _ => 1
        };
        if (function == SysApiNames.Install && a[1] is not TupleTerm { Items.Count: 2 or 3 })
        {
            throw SysSource.Clause(context, function, a);
        }
        Term request = function switch
        {
            SysApiNames.ChangeCode => Term.Tuple(
                A(function),
                a[1],
                a[2],
                a[3]
            ),
            SysApiNames.ReplaceState or SysApiNames.Terminate => Term.Tuple(A(function), a[1]),
            SysApiNames.NoDebug => Term.Tuple(A(SysProtocolNames.Debug), A(function)),
            SysApiNames.Log or SysApiNames.Trace or SysApiNames.Statistics or SysApiNames.LogToFile or SysApiNames.Install or SysApiNames.Remove => Term.Tuple(A(SysProtocolNames.Debug), Term.Tuple(A(function), a[1])),
            _ => A(function)
        };
        Term response;
        using (function is SysApiNames.GetState or SysApiNames.ReplaceState
            ? SysSource.Scope(
                context,
                function,
                a.Length,
                SysSource.ReturnLine(function, a.Length) - 1
            ) : null)
        {
            try
            {
                using var scope = SysSource.Scope(
                    context,
                    SysSourceNames.SendSystemMsg,
                    a.Length == baseArity ? 2 : 3,
                    a.Length == baseArity ? SysSourceLines.SendCallDefault : SysSourceLines.SendCallTimeout
                );
                Term result = a.Length == baseArity
                    ? await Call(
                        context,
                        SysDependencyNames.Gen,
                        SysDependencyFunctions.Call,
                        a[0],
                        A(SysProtocolNames.System),
                        request
                    )
                    : await Call(
                        context,
                        SysDependencyNames.Gen,
                        SysDependencyFunctions.Call,
                        a[0],
                        A(SysProtocolNames.System),
                        request,
                        a[^1]
                    );
                if (result is not TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, var reply] })
                {
                    throw new ErlangException(Term.Tuple(A(SysProtocolNames.TryClause), result));
                }
                response = reply;
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit)
            {
                Term mfa = function == SysApiNames.ReplaceState ? Term.Tuple(A(Name), request, Term.List(a[0]))
                    : function == SysApiNames.NoDebug ? Term.Tuple(A(Name), request, Term.List(a[0]))
                    : Term.Tuple(A(Name), A(function), Cons.From(a.Take(baseArity)));
                if (a.Length != baseArity)
                {
                    var tuple = (TupleTerm)mfa;
                    mfa = Term.Tuple(tuple.Items[0], tuple.Items[1], Cons.From(Cons.Items(tuple.Items[2]).Append(a[^1])));
                }
                throw SysSource.Exit(
                    context,
                    Term.Tuple(error.Reason, mfa),
                    SysSourceNames.SendSystemMsg,
                    a.Length == baseArity ? 2 : 3,
                    a.Length == baseArity ? SysSourceLines.SendExitDefault : SysSourceLines.SendExitTimeout
                );
            }
        }
        if (function is SysApiNames.GetState or SysApiNames.ReplaceState)
        {
            if (response is TupleTerm { Items: [Atom { Name: SysProtocolNames.Ok }, var state] })
            {
                return state;
            }
            if (response is TupleTerm { Items: [Atom { Name: SysProtocolNames.Error }, var reason] })
            {
                throw SysSource.Failure(
                    context,
                    reason,
                    function,
                    a.Length,
                    SysSource.ReturnLine(function, a.Length)
                );
            }
            throw SysSource.Failure(
                context,
                Term.Tuple(A(SysProtocolNames.CaseClause), response),
                function,
                a.Length,
                SysSource.ReturnLine(function, a.Length) - 1
            );
        }

        return response;
    }

    internal static Atom A(string name) => Term.A(name);

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

    internal static bool Is(Term value, string name) => value is Atom atom && atom.Name == name;

    private static async ValueTask<Term> Apply(ProcessContext context, Term function, params Term[] arguments)
    {
        if (function is not FunctionTerm callback)
        {
            throw new ErlangException(Term.Tuple(A(SysProtocolNames.Badfun), function));
        }

        return await callback.Invoke(context, arguments);
    }
}
