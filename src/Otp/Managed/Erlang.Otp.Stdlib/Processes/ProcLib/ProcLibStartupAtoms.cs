namespace Erlang.Otp;

internal static class ProcLibStartupAtoms
{
    public static readonly Atom Nack = Term.A(ProcLibStartupNames.Nack);
    public static readonly Atom Infinity = Term.A(ProcLibStartupNames.Infinity);
    public static readonly Atom Timeout = Term.A(ProcLibStartupNames.Timeout);
    public static readonly Atom Error = Term.A(ProcLibStartupNames.Error);
    public static readonly Atom Link = Term.A(ProcLibStartupNames.Link);
    public static readonly Atom Monitor = Term.A(ProcLibStartupNames.Monitor);
    public static readonly Atom Kill = Term.A(ProcLibStartupNames.Kill);
    public static readonly Atom Down = Term.A(ProcessMessageTags.MonitorDown);
    public static readonly Atom LinkedExit = Term.A(ProcessMessageTags.LinkedExit);
    public static readonly Atom Process = Term.A(ProcessMonitorKinds.Process);
}
