namespace Erlang.Otp;

internal static class ProcLibReportFormatAtoms
{
    public static readonly Atom Label = Term.A(ProcLibReportFormatNames.Label);
    public static readonly Atom Report = Term.A(ProcLibReportFormatNames.Report);
    public static readonly Atom CharsLimit = Term.A(ProcLibReportFormatNames.CharsLimit);
    public static readonly Atom Depth = Term.A(ProcLibReportFormatNames.Depth);
    public static readonly Atom SingleLine = Term.A(ProcLibReportFormatNames.SingleLine);
    public static readonly Atom Encoding = Term.A(ProcLibReportFormatNames.Encoding);
    public static readonly Atom Latin1 = Term.A(ProcLibReportFormatNames.Latin1);
    public static readonly Atom Utf8 = Term.A(ProcLibReportFormatNames.Utf8);
    public static readonly Atom Unlimited = Term.A(ProcLibReportFormatNames.Unlimited);
    public static readonly Atom InitialCall = Term.A(ProcLibReportFormatNames.InitialCall);
    public static readonly Atom ProcessLabel = Term.A(ProcLibReportFormatNames.ProcessLabel);
    public static readonly Atom ErrorInfo = Term.A(ProcLibReportFormatNames.ErrorInfo);
    public static readonly Atom Neighbour = Term.A(ProcLibReportFormatNames.Neighbour);
    public static readonly Atom Pid = Term.A(ProcLibReportFormatNames.Pid);
    public static readonly Atom Undefined = ProcessInfoAtoms.Undefined;
    public static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    public static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
    public static readonly TupleTerm CrashLabel = Term.Tuple(ProcLibAtoms.Module, Term.A(ProcLibReportFormatNames.Crash));
}
