using System.Collections.Concurrent;

namespace Erlang;

internal static class SysSource
{
    private static readonly ConcurrentDictionary<(string Function, int Arity, int Line), object> Continuations = new();

    internal static Term Location(int line) => line == 0 ? Nil.Value : Term.List(
        Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(SysSourceNames.FileName)),
        Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
    );

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            SysModule.Name,
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
                Term.A(SysModule.Name),
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
                Term.A(SysModule.Name),
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

    internal static ErlangException Exit(
        ProcessContext context,
        Term reason,
        string function,
        int arity,
        int line
    )
        => Capture(
            context,
            new ErlangException(reason, ErlangExceptionClasses.Exit),
            Term.Tuple(
                Term.A(SysModule.Name),
                Term.A(function),
                Term.I(arity),
                Location(line)
            )
        );

    internal static ErlangException Clause(ProcessContext context, string function, IReadOnlyList<Term> arguments)
        => Error(
            context,
            function,
            arguments,
            function == SysApiNames.Install ? (arguments.Count == 2 ? SysSourceLines.InstallDefaultClause : SysSourceLines.InstallTimeoutClause) : 0,
            ErlangErrorReasons.FunctionClause
        );

    internal static int CommandLine(Term message) => message switch
    {
        Atom { Name: SysApiNames.GetState } => SysSourceLines.CommandGetState,
        Atom { Name: SysApiNames.GetStatus } => SysSourceLines.CommandGetStatus,
        TupleTerm { Items: [Atom { Name: SysApiNames.ReplaceState }, _] } => SysSourceLines.CommandReplaceState,
        TupleTerm { Items: [Atom { Name: SysProtocolNames.Debug }, _] } => SysSourceLines.CommandDebug,
        TupleTerm { Items: [Atom { Name: SysApiNames.ChangeCode }, _, _, _] } => SysSourceLines.CommandCodeChange,
        _ => 0
    };

    internal static int ReturnLine(string function, int arity) => (function, arity) switch
    {
        (SysApiNames.GetState, 1) => SysSourceLines.GetStateDefaultError,
        (SysApiNames.GetState, 2) => SysSourceLines.GetStateTimeoutError,
        (SysApiNames.ReplaceState, 2) => SysSourceLines.ReplaceStateDefaultError,
        (SysApiNames.ReplaceState, 3) => SysSourceLines.ReplaceStateTimeoutError,
        _ => 0
    };
}
