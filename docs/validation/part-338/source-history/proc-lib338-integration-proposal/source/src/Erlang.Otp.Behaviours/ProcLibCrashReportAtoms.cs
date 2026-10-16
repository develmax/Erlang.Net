namespace Erlang.Otp;

internal static class ProcLibCrashReportAtoms
{
    public static readonly Atom Error = Term.A(ProcLibCrashReportNames.Error);
    public static readonly Atom Normal = Term.A(ProcLibCrashReportNames.Normal);
    public static readonly Atom Shutdown = Term.A(ProcLibCrashReportNames.Shutdown);
    public static readonly Atom Dictionary = Term.A(ProcLibCrashReportNames.Dictionary);
    public static readonly Atom ProcessLabel = Term.A(ProcLibCrashReportNames.ProcessLabel);
    public static readonly Atom InitialCall = Term.A(ProcLibCrashReportNames.InitialCall);
    public static readonly Atom Ancestors = Term.A(ProcLibCrashReportNames.Ancestors);
    public static readonly Atom Links = Term.A(ProcLibCrashReportNames.Links);
    public static readonly Atom TrapExit = Term.A(ProcLibCrashReportNames.TrapExit);
    public static readonly Atom Unlimited = Term.A(ProcLibCrashReportNames.Unlimited);
    public static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    public static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
    public static readonly TupleTerm InitialCallQuery = Term.Tuple(Dictionary, ProcLibAtoms.InitialCall);
    public static readonly TupleTerm ProcessLabelQuery = Term.Tuple(Dictionary, ProcLibIntrospectionAtoms.ProcessLabel);
    public static readonly TupleTerm AncestorsQuery = Term.Tuple(Dictionary, ProcLibAtoms.Ancestors);
    public static readonly Term OwnKeys = Cons.From(new[]
    {
        ProcLibCrashReportNames.RegisteredName,
        ProcLibCrashReportNames.Dictionary,
        ProcLibCrashReportNames.MessageQueueLength,
        ProcLibCrashReportNames.Links,
        ProcLibCrashReportNames.TrapExit,
        ProcLibCrashReportNames.Status,
        ProcLibCrashReportNames.HeapSize,
        ProcLibCrashReportNames.StackSize,
        ProcLibCrashReportNames.Reductions
    }.Select(name => (Term)Term.A(name)));
    public static readonly Term NeighbourKeys = Cons.From(new[]
    {
        ProcLibCrashReportNames.RegisteredName,
        ProcLibCrashReportNames.InitialCall,
        ProcLibCrashReportNames.CurrentFunction,
        ProcLibCrashReportNames.MessageQueueLength,
        ProcLibCrashReportNames.Links,
        ProcLibCrashReportNames.TrapExit,
        ProcLibCrashReportNames.Status,
        ProcLibCrashReportNames.HeapSize,
        ProcLibCrashReportNames.StackSize,
        ProcLibCrashReportNames.Reductions,
        ProcLibCrashReportNames.CurrentStacktrace
    }.Select(name => (Term)Term.A(name)));
    public static readonly Term NeighbourDictionaryKeys = Term.List(ProcessLabelQuery, InitialCallQuery, AncestorsQuery);
    public static readonly FunctionTerm ReportCallback = new(
        new ExternalFunctionIdentity(ProcLibAtoms.Module, Term.A(ProcLibCrashReportNames.ReportCallback), ProcLibCrashReportNames.ReportCallbackArity),
        (executionContext, arguments) =>
        {
            var process = (ProcessContext)executionContext;

            return process.Runtime.Modules.Call(process, ProcLibNames.Module, ProcLibCrashReportNames.ReportCallback, arguments.ToArray());
        }
    );
}
