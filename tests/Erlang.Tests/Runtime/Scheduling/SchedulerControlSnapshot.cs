namespace Erlang.Tests;

internal sealed record SchedulerControlSnapshot(
    int Online,
    Pid? Changing,
    Pid? Granted,
    int Queued
);
