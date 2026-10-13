namespace Erlang;

internal sealed record ProcessMonitorEntry(
    Pid Observer,
    Pid? Target,
    Term ReportedTarget,
    Term Tag,
    bool Priority,
    bool KeepAlias,
    ProcessMonitorSource Source = ProcessMonitorSource.Process,
    PortTerm? PortTarget = null
);
