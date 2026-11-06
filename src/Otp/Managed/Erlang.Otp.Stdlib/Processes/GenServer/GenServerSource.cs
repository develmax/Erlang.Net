// SPDX-License-Identifier: Apache-2.0
// Copyright Ericsson AB 1996-2026. All Rights Reserved.
// Modified: OTP-29.1.1 gen_server ported to C#.
using System.Collections.Concurrent;

namespace Erlang;

internal static class GenServerSource
{
    private static readonly ConcurrentDictionary<(string Function, int Arity, int Line), object> Continuations = new();

    internal static Term Location(int line) => line == 0 ? Nil.Value : Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(GenServerSourceNames.GenServerErl)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            GenServerModule.Name,
            function,
            arity,
            Location(line),
            Continuations.GetOrAdd((function, arity, line), static _ => new object())
        );

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line,
        string reason = ErlangErrorReasons.BadArgument
    )
        => Capture(
            context,
            new ErlangException(reason),
            Term.Tuple(
                Term.A(GenServerModule.Name),
                Term.A(function),
                Cons.From(arguments),
                Location(line)
            )
        );

    internal static ErlangException Failure(
        ProcessContext context,
        Term reason,
        string function,
        int arity,
        int line
    )
        => Capture(
            context,
            new ErlangException(reason),
            Term.Tuple(
                Term.A(GenServerModule.Name),
                Term.A(function),
                Term.I(arity),
                Location(line)
            )
        );

    private static ErlangException Capture(ProcessContext context, ErlangException error, Term origin)
    {
        var frames = new List<Term> { origin };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        int depth = Math.Max(1, context.Runtime.BacktraceDepth);
        while (caller is not null && frames.Count < depth)
        {
            if (caller.ContinuationIdentity is { } identity && ReferenceEquals(identity, previous))
            {
                caller = caller.Parent;
                continue;
            }
            frames.Add(Term.Tuple(
                Term.A(caller.Module),
                Term.A(caller.Function),
                caller.HostHandlerArguments is { } args ? Cons.From(args) : Term.I(caller.Arity),
                caller.HostHandlerArguments is not null ? Nil.Value : caller.SourceLocation ?? Nil.Value
            ));
            previous = caller.ContinuationIdentity;
            caller = caller.Parent;
        }

        return error.WithCapturedStack(Cons.From(frames));
    }

    internal static int ApiLine(string function, int arity, bool error = false)
        => Sites.TryGetValue((function, arity, error), out int line) ? line : 0;

    internal static ErlangException Failure(
        ProcessContext c,
        string f,
        int arity,
        int line,
        Term reason
    )
        => Failure(
            c,
            reason,
            f,
            arity,
            line
        );

    internal static ErlangException Clause(
        ProcessContext c,
        string f,
        Term[] a,
        int line
    )
        => Error(
            c,
            f,
            a,
            line,
            ErlangErrorReasons.FunctionClause
        );

    internal static ErlangException Exit(
        ProcessContext c,
        string f,
        int arity,
        int line,
        Term reason
    )
        => Capture(
            c,
            new ErlangException(reason, ErlangExceptionClasses.Exit),
            Term.Tuple(
                Term.A(GenServerModule.Name),
                Term.A(f),
                Term.I(arity),
                Location(line)
            )
        );

    internal static readonly Dictionary<(string, int, bool), int> Sites = new()
    {
        [(GenServerFunctionNames.Start, 3, false)] = GenServerSourceLines.StartDelegation,
        [(GenServerFunctionNames.Start, 3, true)] = GenServerSourceLines.StartArgumentError,
        [(GenServerFunctionNames.Start, 4, false)] = GenServerSourceLines.StartLine1009,
        [(GenServerFunctionNames.Start, 4, true)] = GenServerSourceLines.StartLine1011,
        [(GenServerFunctionNames.StartLink, 3, true)] = GenServerSourceLines.StartLinkLine1030,
        [(GenServerFunctionNames.StartLink, 4, true)] = GenServerSourceLines.StartLinkLine1100,
        [(GenServerFunctionNames.StartMonitor, 3, true)] = GenServerSourceLines.StartMonitorLine1120,
        [(GenServerFunctionNames.StartMonitor, 4, true)] = GenServerSourceLines.StartMonitorLine1151,
        [(GenServerFunctionNames.Call, 2, false)] = GenServerSourceLines.CallLine1217,
        [(GenServerFunctionNames.Call, 2, true)] = GenServerSourceLines.CallLine1221,
        [(GenServerFunctionNames.Call, 3, false)] = GenServerSourceLines.CallLine1297,
        [(GenServerFunctionNames.Call, 3, true)] = GenServerSourceLines.CallLine1301,
        [(GenServerFunctionNames.SendRequest, 2, false)] = GenServerSourceLines.SendRequestLine1347,
        [(GenServerFunctionNames.SendRequest, 2, true)] = GenServerSourceLines.SendRequestLine1350,
        [(GenServerFunctionNames.SendRequest, 4, false)] = GenServerSourceLines.SendRequestLine1378,
        [(GenServerFunctionNames.SendRequest, 4, true)] = GenServerSourceLines.SendRequestLine1381,
        [(GenServerFunctionNames.WaitResponse, 2, false)] = GenServerSourceLines.WaitResponseLine1417,
        [(GenServerFunctionNames.WaitResponse, 2, true)] = GenServerSourceLines.WaitResponseLine1420,
        [(GenServerFunctionNames.WaitResponse, 3, false)] = GenServerSourceLines.WaitResponseLine1491,
        [(GenServerFunctionNames.WaitResponse, 3, true)] = GenServerSourceLines.WaitResponseLine1494,
        [(GenServerFunctionNames.ReceiveResponse, 2, false)] = GenServerSourceLines.ReceiveResponseLine1533,
        [(GenServerFunctionNames.ReceiveResponse, 2, true)] = GenServerSourceLines.ReceiveResponseLine1536,
        [(GenServerFunctionNames.ReceiveResponse, 3, false)] = GenServerSourceLines.ReceiveResponseLine1609,
        [(GenServerFunctionNames.ReceiveResponse, 3, true)] = GenServerSourceLines.ReceiveResponseLine1612,
        [(GenServerFunctionNames.CheckResponse, 2, false)] = GenServerSourceLines.CheckResponseLine1644,
        [(GenServerFunctionNames.CheckResponse, 2, true)] = GenServerSourceLines.CheckResponseLine1647,
        [(GenServerFunctionNames.CheckResponse, 3, false)] = GenServerSourceLines.CheckResponseLine1709,
        [(GenServerFunctionNames.CheckResponse, 3, true)] = GenServerSourceLines.CheckResponseLine1712,
        [(GenServerFunctionNames.ReqidsSize, 1, false)] = GenServerSourceLines.ReqidsSizeLine1746,
        [(GenServerFunctionNames.ReqidsSize, 1, true)] = GenServerSourceLines.ReqidsSizeLine1748,
        [(GenServerFunctionNames.ReqidsAdd, 3, false)] = GenServerSourceLines.ReqidsAddLine1765,
        [(GenServerFunctionNames.ReqidsAdd, 3, true)] = GenServerSourceLines.ReqidsAddLine1767,
        [(GenServerFunctionNames.ReqidsToList, 1, false)] = GenServerSourceLines.ReqidsToListLine1783,
        [(GenServerFunctionNames.ReqidsToList, 1, true)] = GenServerSourceLines.ReqidsToListLine1785,
    };
}
