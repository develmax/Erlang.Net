namespace Erlang.Otp;

internal static class StartupHandshakeAtoms
{
    public static readonly Atom Acknowledged = Term.A(StartupHandshakeNames.Acknowledged);
    public static readonly Atom Rejected = Term.A(StartupHandshakeNames.Rejected);
    public static readonly Atom Down = Term.A(ProcessMessageTags.MonitorDown);
    public static readonly Atom Exit = Term.A(ProcessMessageTags.LinkedExit);
    public static readonly Atom Process = Term.A(ProcessMonitorKinds.Process);
}
