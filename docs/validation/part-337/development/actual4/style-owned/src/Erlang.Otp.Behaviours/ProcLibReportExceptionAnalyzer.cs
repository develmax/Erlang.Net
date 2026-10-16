namespace Erlang.Otp;

internal static class ProcLibReportExceptionAnalyzer
{
    public static ProcLibReportExceptionAnalysis Analyze(Term exceptionClass, Term reason, Term stackTrace)
    {
        var frames = new List<TupleTerm>();
        Term tail = stackTrace;
        while (tail is Cons entry)
        {
            if (entry.Head is not TupleTerm { Items: [Atom, Atom, var arity, var location] } frame ||
                arity is not Integer && !IsProperList(arity) || location is not Cons and not Nil)
                return new(Term.Tuple(reason, stackTrace), [], []);
            frames.Add(frame);
            tail = entry.Tail;
        }
        if (tail is not Nil)
            return new(Term.Tuple(reason, stackTrace), [], []);
        if (exceptionClass is Atom { Name: ErlangExceptionClasses.Error } && frames.Count > 0)
        {
            TupleTerm first = frames[0];
            if (reason is Atom { Name: ErlangErrorReasons.FunctionClause } && IsProperList(first.Items[2]) ||
                reason is Atom { Name: ErlangErrorReasons.UndefinedFunction })
                return new(reason, [first], frames.Skip(1).ToArray());
            if (reason is Atom { Name: ErlangErrorReasons.FunctionClause } && frames.Count == 1 &&
                first.Items[0] is Atom { Name: ProcLibReportExceptionNames.Shell } && first.Items[2] is Integer)
                return new(reason, [Term.Tuple(first.Items[1], first.Items[2])], []);
        }

        return new(reason, [], frames);
    }

    public static IReadOnlyList<TupleTerm> Trim(IReadOnlyList<TupleTerm> frames)
    {
        int count = frames.Count;
        while (count > 0 && frames[count - 1].Items[0] is Atom { Name: ProcLibNames.Module or ProcLibReportExceptionNames.Evaluator })
            count--;

        return frames.Take(count).ToArray();
    }

    private static bool IsProperList(Term value)
    {
        while (value is Cons list)
            value = list.Tail;

        return value is Nil;
    }
}
