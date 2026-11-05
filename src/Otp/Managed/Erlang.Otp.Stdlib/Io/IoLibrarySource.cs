namespace Erlang;

internal static class IoLibrarySource
{
    internal const string File = "io.erl";
    internal const int LocalOutputError = 200;
    internal const int DeviceOutputError = 205;
    internal const int GetCharsClause = 359;
    internal const int RequestsCall = 1695;
    internal const int BatchClause = 1720;
    internal const int BatchRecursion = 1727;
    internal const int ReadParser = 768;
    internal const int ExpressionsParser = 1556;
    internal const int FormParser = 1635;
    internal const int OutputRequest = 197;
    internal const int RequestClause = 1652;
    internal const int RequestsClause = 1692;
    internal const int ReadRequest = 709;
    internal const int ReadOptionsRequest = 765;
    internal const int ExpressionsRequest = 1554;
    internal const int FormRequest = 1633;
    internal const int ColumnsRequest = 298;
    internal const int RowsRequest = 321;
    internal const string BatchFunction = "io_requests";

    internal static Term Location(int line, params Term[] extra)
        => Cons.From(new[]
        {
            Term.Tuple(Term.A(ErlangSourceLocationNames.File), Term.String(File)),
            Term.Tuple(Term.A(ErlangSourceLocationNames.Line), Term.I(line))
        }.Concat(extra));

    internal static Term Frame(string function, Term arguments, int line)
        => Term.Tuple(
            Term.A(IoLibrary.Name),
            Term.A(function),
            arguments,
            Location(line)
        );

    internal static ErlangException Clause(
        ProcessContext context,
        string function,
        Term arguments,
        int line
    )
        => Capture(context, Term.A(ErlangErrorReasons.FunctionClause), Frame(function, arguments, line));

    internal static ErlangException Capture(ProcessContext context, Term reason, params Term[] frames)
    {
        Term stack = Cons.From(frames, context.CaptureErrorStack(Term.I(0), null));

        return new ErlangException(reason)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = Cons.From(Cons.Items(stack).Take(Math.Max(1, context.Runtime.BacktraceDepth)))
        };
    }
}
