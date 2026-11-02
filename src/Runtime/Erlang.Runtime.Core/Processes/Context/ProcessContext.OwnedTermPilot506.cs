namespace Erlang;

public sealed partial class ProcessContext
{
    private ProcessTermHeap506? termHeap506;
    private readonly AsyncLocal<OwnedTermInvocation506?> currentOwned506 = new();
    internal ProcessTermHeap506 TermHeap506 => LazyInitializer.EnsureInitialized(ref termHeap506, () => new(Runtime));
    internal OwnedTermInvocation506? CurrentOwned506
    {
        get => currentOwned506.Value;
        set => currentOwned506.Value = value;
    }
    internal Task OwnedTermRetirement506 { get; private set; } = Task.CompletedTask;
    private volatile bool genuineVmExecution683;
    internal bool OwnedExecutionEnabled683 => Runtime.OwnedTermsPilot506 || genuineVmExecution683 || ordinaryGuestCollector;

    internal void ActivateGenuineVmExecution683(RuntimeProcessPayload641 source)
    {
        if (OwnedExecutionEnabled683)
            return;
        if (!ReferenceEquals(source.Runtime641, Runtime) || !source.OwnsProcess683(this)
            || source.Graph641 is not { HasGraphReferences593: true } graph || !ReferenceEquals(graph.Runtime, Runtime)
            || CurrentOwned506 is not null || !RuntimeActorExecutionTurn.IsOwnedBy(this)
            || Volatile.Read(ref Terminated) || Runtime.IsHaltRequested)
            throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
        Cancellation.ThrowIfCancellationRequested();
        // Enrollment precedes ANY physical frame/slab allocation. Failure still
        // leaves the actual context lifecycle responsible for partial teardown.
        genuineVmExecution683 = true;
        Runtime.RegisterGenuineVmContext683(this);
        Dictionary.ActivateGenuineVmExecution683();
    }


    internal OwnedTermInvocation506? EnterOwnedInvocation506(IReadOnlyList<Term> arguments, bool hostBoundary = false)
    {
        if (!OwnedExecutionEnabled683)
            return null;
        if (hostBoundary)
        {
            Runtime.ThrowIfHalted();
            Cancellation.ThrowIfCancellationRequested();
        }

        return new(this, arguments, hostBoundary);
    }

    // The VM root importer receives only the actual still-live resumed payload's
    // module-literal custody. Existing Host/guest entry points stay unchanged.
    internal OwnedTermInvocation506? EnterOwnedInvocationWithLiteral801(
        IReadOnlyList<Term> arguments,
        ModuleLiteralRoots801? carriedLiteral801
    ) => !OwnedExecutionEnabled683 ? null : new(
        this,
        arguments,
        false,
        false,
        carriedLiteral801
    );

    internal Term OwnGuestResult506(Term value) => CurrentOwned506?.Frame.Retain(value) ?? value;

    internal void BeginOwnedTermRetirement506()
    {
        if (!VmStorageBackingEnabled)
            return;
        SequentialTrace.Close();
        CloseGroupLeaderRoots();
        Dictionary.Close506();
        if (termHeap506 is { } heap)
        {
            OwnedTermRetirement506 = heap.RetireAsync();
            Runtime.TrackOwnedTermRetirement506(this, OwnedTermRetirement506);
        }
    }
}
