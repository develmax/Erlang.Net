namespace Erlang;

internal sealed partial class ProcessTracing
{
    // Deliberately finite: named sessions, compiled programs and timestamps
    // require their own per-view preparation and are not claimed here.
    internal ProcessContext? CaptureReadonlyReceiveRoute817(ProcessContext tracee)
    {
        if (LiveTraceViews425().Length != 0 || !traceReceive || receivePattern is not null) return null;
        if (!System.Threading.Monitor.IsEntered(gate)) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        settings.TryGetValue(tracee.Self, out ProcessTraceSettings? current);
        if (current is null || !processes.ContainsKey(current.Tracer) || (current.Flags & ProcessTraceFlags.Receive) == 0
            || current.Tracer.Equals(tracee.Self)
            || (current.Flags & (ProcessTraceFlags.Timestamp | ProcessTraceFlags.MonotonicTimestamp | ProcessTraceFlags.StrictMonotonicTimestamp)) != 0)
            return null;

        return processes.TryGetValue(current.Tracer, out var tracer) ? tracer : null;
    }

    internal bool HasNoOutputForReadonlyInfo817(ProcessContext requester)
    {
        if (LiveTraceViews425().Length != 0 || sendPattern is not null || receivePattern is not null) return false;
        if (!System.Threading.Monitor.IsEntered(gate)) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
        settings.TryGetValue(requester.Self, out ProcessTraceSettings? current);

        return current is null || !processes.ContainsKey(current.Tracer) || current.Tracer.Equals(requester.Self)
            || (current.Flags & (ProcessTraceFlags.Send | ProcessTraceFlags.Receive)) == 0;
    }
}
