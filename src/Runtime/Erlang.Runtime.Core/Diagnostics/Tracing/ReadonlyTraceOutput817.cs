namespace Erlang;

// Physical leaf bridge only. The actual tracing caller must select the event
// before PublishSelected; this type never substitutes for matcher/configuration
// selection or the future-prefix ordering proof.
internal sealed class ReadonlyTraceOutput817 : IAsyncDisposable
{
    private readonly ProcessRuntime runtime;
    private readonly ProcessContext tracer;
    private readonly MailboxGraphSnapshot593 graph;
    private readonly Term value;
    private readonly OrdinaryBinaryLease[] leases;
    private readonly object gate = new();
    private Task? closing;
    private ReadonlyTraceSource817? source;
    private int selected;
    private int disposed;
    internal Term ActualValue817 => value;

    internal bool MatchesSelected817(
        Pid tracee,
        ProcessTraceSettings settings,
        IReadOnlyList<Term> details,
        Term? matchValue
    )
    {
        ReadonlyTraceSource817? held = source;

        return held is not null && held.Receiver817.Self.Equals(tracee) && tracer.Self.Equals(settings.Tracer)
            && (settings.Flags & (ProcessTraceFlags.Timestamp | ProcessTraceFlags.MonotonicTimestamp | ProcessTraceFlags.StrictMonotonicTimestamp)) == 0
            && details.Count == 2 && details[0] is Atom { Name: ProcessTraceNames.Receive }
            && ReferenceEquals(details[1], held.Value)
            && (matchValue is null || matchValue is Atom { Name: ErlangBooleanAtoms.True });
    }

    internal ReadonlyTraceOutput817(
        ProcessRuntime runtime,
        ProcessContext tracer,
        MailboxGraphSnapshot593 graph,
        Term value,
        OrdinaryBinaryLease[] leases,
        ReadonlyTraceSource817 source
    )
    {
        this.runtime = runtime;
        this.tracer = tracer;
        this.graph = graph;
        this.value = value;
        this.leases = leases;
        this.source = source;
    }

    // Called only after actual event selection, with the SAME runtime R held.
    // Native shares begin here; the exact prebuilt fragment becomes the queued
    // payload. No source pin/import/Host escape or target heap acquisition runs.
    internal bool PublishSelected()
    {
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed != 0, this);
            if (selected != 0) throw new InvalidOperationException(ReadonlyTrace817Names.Invalid);
            selected = 1;

            return runtime.PublishReadonlyTraceFragment817(
                tracer,
                graph,
                value,
                leases
            );
        }
    }

    public ValueTask DisposeAsync()
    {
        runtime.AssertSequenceTracePreparation();
        lock (gate)
        {
            disposed = 1;

            return new(closing ??= CloseAsync());
        }
    }

    private async Task CloseAsync()
    {
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        try
        {
            // Unselected fragment teardown must complete while existing source
            // custody still keeps all unadmitted descriptor pointers live.
            await graph.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref source, null)?.ReleaseUse();
        }
    }
}
