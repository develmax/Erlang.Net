namespace Erlang.Otp;

internal static class ProcLibStopAtoms
{
    public static readonly Atom Millisecond = Term.A(ProcLibStopNames.Millisecond);
    public static readonly Atom Microsecond = Term.A(ProcLibStopNames.Microsecond);
    public static readonly Atom Normal = Term.A(ProcessExitReasons.Normal);
    public static readonly Atom NoProcess = Term.A(ErlangErrorReasons.NoProcess);
    public static readonly Atom Timeout = Term.A(ErlangErrorReasons.Timeout);
}
