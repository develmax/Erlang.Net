namespace Erlang.Otp;

internal static class ProcLibCrashReportLogging
{
    public static async ValueTask<Term> Report(
        ProcessContext context,
        string exceptionClass,
        Term reason,
        Term initialCall,
        Term stackTrace
    )
    {
        if (IsSuppressed(exceptionClass, reason))
            return ProcLibAtoms.Ok;
        Term allowed = await context.Runtime.Modules.Call(
            context,
            ProcLibCrashReportNames.Logger,
            ProcLibCrashReportNames.Allow,
            ProcLibCrashReportAtoms.Error,
            ProcLibAtoms.Module
        );
        if (allowed.Equals(ProcLibCrashReportAtoms.False))
            return ProcLibAtoms.Ok;
        if (!allowed.Equals(ProcLibCrashReportAtoms.True))
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), allowed));
        var builder = new ProcLibCrashReportBuilder(
            context,
            () => context.Runtime.Modules.Call(context, ProcLibCrashReportNames.ErrorLogger, ProcLibCrashReportNames.GetFormatDepth),
            term => context.Runtime.Modules.Call(
                context,
                ProcLibCrashReportNames.ErrorLogger,
                ProcLibCrashReportNames.LimitTerm,
                term
            )
        );
        Term report = await builder.Build(
            exceptionClass,
            reason,
            initialCall,
            stackTrace
        );
        Term message = Map(
            (ProcLibCrashReportNames.Label, Term.Tuple(ProcLibAtoms.Module, Term.A(ProcLibCrashReportNames.Crash))),
            (ProcLibCrashReportNames.Report, report)
        );
        Term metadata = Map(
            (ProcLibCrashReportNames.Domain, Term.List(Term.A(ProcLibCrashReportNames.OtpDomain), Term.A(ProcLibCrashReportNames.SaslDomain))),
            (ProcLibCrashReportNames.ReportCallback, ProcLibCrashReportAtoms.ReportCallback),
            (ProcLibCrashReportNames.LoggerFormatter, Map((ProcLibCrashReportNames.Title, Term.String(ProcLibCrashReportNames.CrashTitle)))),
            (ProcLibCrashReportNames.ErrorLogger, Map(
                (ProcLibCrashReportNames.Tag, Term.A(ProcLibCrashReportNames.ErrorReport)),
                (ProcLibCrashReportNames.Type, Term.A(ProcLibCrashReportNames.CrashReport))
            ))
        );
        Term location = Map(
            (ProcLibCrashReportNames.Mfa, Term.Tuple(ProcLibAtoms.Module, Term.A(ProcLibCrashReportNames.CrashReport), Term.I(ProcLibCrashReportNames.CrashReportArity))),
            (ProcLibCrashReportNames.Line, Term.I(ProcLibCrashReportNames.PinnedLogInvocationLine)),
            (ProcLibCrashReportNames.File, Term.String(ProcLibCrashReportNames.OriginalFile))
        );

        return await context.Runtime.Modules.Call(
            context,
            ProcLibCrashReportNames.Logger,
            ProcLibCrashReportNames.MacroLog,
            location,
            ProcLibCrashReportAtoms.Error,
            message,
            metadata
        );
    }

    internal static bool IsSuppressed(string exceptionClass, Term reason)
        => exceptionClass == ErlangExceptionClasses.Exit &&
            (reason.Equals(ProcLibCrashReportAtoms.Normal) || reason.Equals(ProcLibCrashReportAtoms.Shutdown) ||
             reason is TupleTerm { Items: [Atom { Name: ProcLibCrashReportNames.Shutdown }, _] });

    private static MapTerm Map(params (string Key, Term Value)[] entries)
        => new(entries.Select(entry => new KeyValuePair<Term, Term>(Term.A(entry.Key), entry.Value)));
}
