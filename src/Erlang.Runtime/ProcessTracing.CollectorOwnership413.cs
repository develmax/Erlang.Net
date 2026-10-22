namespace Erlang;

internal sealed partial class ProcessTracing
{
    // Every tracee/configuration/return snapshot owns ordinary CLR references.
    // Session retirement clears owner dictionaries under the same event gate;
    // no tracee contains an unmanaged pointer requiring a later sweep.
    internal ManagedTraceStorageModel413 CollectorTraceStorage413 { get; }
        = ManagedTraceStorageModel413.IndependentManagedNamedSessions425;
}
