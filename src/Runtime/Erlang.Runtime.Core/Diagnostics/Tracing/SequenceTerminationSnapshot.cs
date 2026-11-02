namespace Erlang;

internal sealed record SequenceTerminationSnapshot(
    SequenceTraceToken? Token,
    SequenceTraceToken? Value,
    ulong Clock,
    ulong LastCount,
    Pid[] AdmissionLinks,
    Pid[] Links,
    Pid[] AdmissionOrigins,
    Pid[] PendingOrigins,
    Pid[] Destinations,
    bool[] LiveDestinations,
    ProcessContext?[] Recipients
)
{
    internal int Count => checked(Links.Length + PendingOrigins.Length);

    internal Pid Destination(int index) => Destinations[index];

    internal bool UsesExitReason(int index) => index < Links.Length;
}
