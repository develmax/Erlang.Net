namespace Erlang;

internal static class LoggerSource
{
    internal const string File = "logger.erl";
    internal const int AllowClause = 596;
    internal const int GetModuleLevelClause = 1268;
    internal const int CompareBadArgument = 1306;
    internal const int SetMetadataBadArgument = 1328;
    internal const int UpdateMetadataBadArgument = 1355;
    internal const int InspectBadArgument = 1420;
    internal const int InvalidPrimaryConfiguration = 1600;
    internal const int InvalidDefaultConfiguration = 1618;
    internal const int InvalidHandlerConfiguration = 1698;
    internal const string InitializeHandlers = "init_kernel_handlers";
    internal const string LogAllowed = "log_allowed";
    internal const string DoLogAllowed = "do_log_allowed";
    internal const int GroupLeaderNode = 1897;
    internal const int DoLogAllowedClause = 1922;

    internal static IDisposable Scope(
        ProcessContext context,
        string function,
        int arity,
        int line
    )
        => context.EnterGeneratedFunction(
            LoggerModule.Name,
            function,
            arity,
            Term.List(
                Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(File)),
                Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
            ),
            new object()
        );

    internal static ErlangException Error(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        string reason,
        int line
    )
    {
        Term location = Term.List(
            Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(File)),
            Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
        );
        using var frame = context.EnterGeneratedFunction(
            LoggerModule.Name,
            function,
            arguments.Count,
            location,
            new object()
        );

        return new ErlangException(Term.A(reason))
        {
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), null),
            ExplicitStackTrace = true
        };
    }
}
