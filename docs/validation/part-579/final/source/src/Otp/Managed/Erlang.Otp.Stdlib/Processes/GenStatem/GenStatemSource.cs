using System.Collections.Concurrent;

namespace Erlang;

internal static class GenStatemSource
{
    private static readonly ConcurrentDictionary<(string Function, int Arity, int Line), object> Continuations = new();

    internal static Term Location(int line) => line == 0 ? Nil.Value : Term.List(
        Term.Tuple(Term.A(GenStatemProtocolAtoms.File), Term.String(GenStatemSourceNames.GenStatemErl)),
        Term.Tuple(Term.A(GenStatemProtocolAtoms.Line), Term.I(line))
    );

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            GenStatemModule.Name,
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
                Term.A(GenStatemModule.Name),
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
                Term.A(GenStatemModule.Name),
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

    internal static int ErrorLine(string function, int arity) => (function, arity) switch
    {
        (GenStatemFunctionNames.Start, 3) => 2235,
        (GenStatemFunctionNames.Start, 4) => 2258,
        (GenStatemFunctionNames.StartLink, 3) => 2276,
        (GenStatemFunctionNames.StartLink, 4) => 2399,
        (GenStatemFunctionNames.StartMonitor, 3) => 2417,
        (GenStatemFunctionNames.StartMonitor, 4) => 2448,
        (GenStatemFunctionNames.Call, 3) => 2589,
        (GenStatemFunctionNames.SendRequest, 2) => 2634,
        (GenStatemFunctionNames.SendRequest, 4) => 2667,
        (GenStatemFunctionNames.WaitResponse, 2) => 2722,
        (GenStatemFunctionNames.WaitResponse, 3) => 2797,
        (GenStatemFunctionNames.ReceiveResponse, 2) => 2854,
        (GenStatemFunctionNames.ReceiveResponse, 3) => 2931,
        (GenStatemFunctionNames.CheckResponse, 2) => 2967,
        (GenStatemFunctionNames.CheckResponse, 3) => 3033,
        (GenStatemFunctionNames.ReqidsSize, 1) => 3071,
        (GenStatemFunctionNames.ReqidsAdd, 3) => 3091,
        (GenStatemFunctionNames.ReqidsToList, 1) => 3110,
        _ => 0
    };
}
