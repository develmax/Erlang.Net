namespace Erlang;

internal static class ApplicationSource
{
    internal const int WhichApplications = 735;
    internal const int GetApplName = 1073;
    internal const int LoadController = 270;
    internal const int LoadName = 272;
    internal const int LoadDistribution = 273;
    internal const int LoadRollback = 277;
    internal const int EnsureLoaded = 552;
    internal const int Start = 544;
    internal const int EnsureStarted = 584;
    internal const int PermitInvalid = 668;
    internal const int PermitController = 670;
    internal const int SetEnvOne = 767;
    internal const int SetEnvTwo = 805;
    internal const int SetEnvController = 806;
    internal const int SetEnvInvalid = 808;
    internal const int SetEnvFour = 850;
    internal const int UnsetEnv = 889;
    internal const int GetApplication = 1021;
    internal const int GetApplicationInfo = 1022;
    internal const int GetSupervisor = 1042;
    internal const int GetMaster = 1043;
    internal const int GetChild = 1046;
    internal const int EnsureAllStarted = 350;
    internal const int EnsureAllTraversal = 355;
    internal const int EnsureAllEmptyDag = 360;
    internal const int EnqueueClause = 367;
    internal const int EnqueueRunning = 376;
    internal const int EnqueueLoad = 378;
    internal const int EnqueueChild = 380;
    internal const int ChildrenKey = 404;
    internal const int OptionalKey = 405;
    internal const int ModKey = 406;
    internal const int ChildrenTraversal = 412;
    internal const int ChildStart = 415;
    internal const int WaitResponse = 455;
    internal const int WaitExit = 463;
    internal const int WaitDeadlock = 465;
    private const string File = "file";
    private const string Line = "line";
    private const string FileName = "application.erl";

    private static Term Location(int line)
        => line == 0 ? Nil.Value : Term.List(
            Term.Tuple(Term.A(File), Term.String(FileName)),
            Term.Tuple(Term.A(Line), Term.I(line))
        );

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            ApplicationModule.Name,
            function,
            arity,
            Location(line),
            new object()
        );

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        int line
    )
        => Error(
            context,
            function,
            Cons.From(arguments),
            Term.A(ErlangErrorReasons.FunctionClause),
            line
        );

    internal static ErlangException Case(
        ProcessContext context,
        string function,
        int arity,
        Term value,
        int line
    )
        => Error(
            context,
            function,
            Term.I(arity),
            Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), value),
            line
        );

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        Term arguments,
        Term reason,
        int line,
        string exceptionClass = ErlangExceptionClasses.Error
    )
    {
        var frames = new List<Term>
        {
            Term.Tuple(
                Term.A(ApplicationModule.Name),
                Term.A(function),
                arguments,
                Location(line)
            )
        };
        ErlangExecutionFrame? caller = context.CaptureExecutionChain();
        object? previous = null;
        bool first = true;
        while (caller is not null && frames.Count < Math.Max(1, context.Runtime.BacktraceDepth))
        {
            if (first && caller.Module == ApplicationModule.Name && caller.Function == function)
            {
                previous = caller.ContinuationIdentity;
                caller = caller.Parent;
                first = false;
                continue;
            }
            first = false;
            if (caller.ContinuationIdentity is { } identity && ReferenceEquals(previous, identity))
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

        return new ErlangException(reason, exceptionClass)
        {
            StackTraceTerm = Cons.From(frames),
            ExplicitStackTrace = true
        };
    }
}
