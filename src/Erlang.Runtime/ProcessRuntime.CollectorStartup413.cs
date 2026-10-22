namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void StartManagedCollectorsBeforeBoot413()
    {
        // The native VM starts both preloaded actors before init. A small explicit
        // bootstrap which did not select their images must not invent them.
        if (Modules.SourceOwner(ManagedCollectorNames413.LiteralModule, ManagedCollectorNames413.Start, 0)
                is not PreparedBeamImage literal
            || Modules.SourceOwner(ManagedCollectorNames413.TraceModule, ManagedCollectorNames413.Start, 0)
                is not PreparedBeamImage trace
            || !ReferenceEquals(literal.Owner, Modules) || !ReferenceEquals(trace.Owner, Modules)
            || !ManagedCollectorSource413.Matches(literal, ManagedCollectorKind413.Literal, this)
            || !ManagedCollectorSource413.Matches(trace, ManagedCollectorKind413.Trace, this))
            return;

        ProcessContext parent;
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ThrowIfHalted();
            parent = processes[InitializationPid];
        }
        if (!TrySpawnManagedCollector413(
            parent,
            ManagedCollectorNames413.LiteralModule,
            ManagedCollectorNames413.Start,
            [],
            out _
        )
            || !TrySpawnManagedCollector413(
                parent,
                ManagedCollectorNames413.TraceModule,
                ManagedCollectorNames413.Start,
                [],
                out _
            ))
            throw new InvalidOperationException(ManagedCollectorNames413.Startup);
    }
}
