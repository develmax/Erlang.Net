namespace Erlang;

internal sealed record ProcessMonitorOptions(bool Priority, string? AliasMode, Term Tag)
{
    public static ProcessMonitorOptions Default { get; } = new(false, null, Term.A(ProcessMessageTags.MonitorDown));

    private static ProcessMonitorOptions ClockDefault { get; } = new(false, null, Term.A(ClockMonitorNames.Change));

    public static ProcessMonitorOptions ForKind(string kind) => kind == ProcessMonitorKinds.TimeOffset ? ClockDefault : Default;
}
