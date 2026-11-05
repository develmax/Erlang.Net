using System.Collections.Concurrent;

namespace Erlang;

internal static class GenStatemSource
{
    private static readonly ConcurrentDictionary<(string Function, int Arity, int Line), object> Continuations = new();

    internal static Term Location(int line) => line == GenStatemSourceLines.Unavailable ? Nil.Value : Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(GenStatemSourceNames.FileName)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
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
        int depth = Math.Max(GenStatemSourceNames.MinimumBacktraceFrames, context.Runtime.BacktraceDepth);
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
        (GenStatemFunctionNames.Start, 3) => GenStatemSourceLines.StartAnonymousError,
        (GenStatemFunctionNames.Start, 4) => GenStatemSourceLines.StartNamedError,
        (GenStatemFunctionNames.StartLink, 3) => GenStatemSourceLines.StartLinkAnonymousError,
        (GenStatemFunctionNames.StartLink, 4) => GenStatemSourceLines.StartLinkNamedError,
        (GenStatemFunctionNames.StartMonitor, 3) => GenStatemSourceLines.StartMonitorAnonymousError,
        (GenStatemFunctionNames.StartMonitor, 4) => GenStatemSourceLines.StartMonitorNamedError,
        (GenStatemFunctionNames.Call, 3) => GenStatemSourceLines.CallTimeoutError,
        (GenStatemFunctionNames.SendRequest, 2) => GenStatemSourceLines.SendRequestError,
        (GenStatemFunctionNames.SendRequest, 4) => GenStatemSourceLines.SendRequestCollectionError,
        (GenStatemFunctionNames.WaitResponse, 2) => GenStatemSourceLines.WaitResponseError,
        (GenStatemFunctionNames.WaitResponse, 3) => GenStatemSourceLines.WaitResponseCollectionError,
        (GenStatemFunctionNames.ReceiveResponse, 2) => GenStatemSourceLines.ReceiveResponseError,
        (GenStatemFunctionNames.ReceiveResponse, 3) => GenStatemSourceLines.ReceiveResponseCollectionError,
        (GenStatemFunctionNames.CheckResponse, 2) => GenStatemSourceLines.CheckResponseError,
        (GenStatemFunctionNames.CheckResponse, 3) => GenStatemSourceLines.CheckResponseCollectionError,
        (GenStatemFunctionNames.ReqidsSize, 1) => GenStatemSourceLines.RequestCollectionSizeError,
        (GenStatemFunctionNames.ReqidsAdd, 3) => GenStatemSourceLines.RequestCollectionAddError,
        (GenStatemFunctionNames.ReqidsToList, 1) => GenStatemSourceLines.RequestCollectionToListError,
        _ => GenStatemSourceLines.Unavailable
    };

    internal static int CallLine(string function, int arity) => (function, arity) switch
    {
        (GenStatemFunctionNames.SendRequest, 2) => GenStatemSourceLines.SendRequestDelegation,
        (GenStatemFunctionNames.SendRequest, 4) => GenStatemSourceLines.SendRequestCollectionDelegation,
        (GenStatemFunctionNames.WaitResponse, 2) => GenStatemSourceLines.WaitResponseDelegation,
        (GenStatemFunctionNames.WaitResponse, 3) => GenStatemSourceLines.WaitResponseCollectionDelegation,
        (GenStatemFunctionNames.ReceiveResponse, 2) => GenStatemSourceLines.ReceiveResponseDelegation,
        (GenStatemFunctionNames.ReceiveResponse, 3) => GenStatemSourceLines.ReceiveResponseCollectionDelegation,
        (GenStatemFunctionNames.CheckResponse, 2) => GenStatemSourceLines.CheckResponseDelegation,
        (GenStatemFunctionNames.CheckResponse, 3) => GenStatemSourceLines.CheckResponseCollectionDelegation,
        (GenStatemFunctionNames.ReqidsSize, 1) => GenStatemSourceLines.RequestCollectionSizeDelegation,
        (GenStatemFunctionNames.ReqidsAdd, 3) => GenStatemSourceLines.RequestCollectionAddDelegation,
        (GenStatemFunctionNames.ReqidsToList, 1) => GenStatemSourceLines.RequestCollectionToListDelegation,
        _ => GenStatemSourceLines.Unavailable
    };
}
