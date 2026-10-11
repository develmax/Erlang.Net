namespace Erlang;

internal sealed record ProcessMonitorOptions(bool Priority, string? AliasMode, Term Tag)
{
    public static ProcessMonitorOptions Default { get; } = new(false, null, Term.A(ProcessMessageTags.MonitorDown));
}
