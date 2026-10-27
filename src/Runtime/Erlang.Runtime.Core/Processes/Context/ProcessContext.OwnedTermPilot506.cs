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

    internal OwnedTermInvocation506? EnterOwnedInvocation506(IReadOnlyList<Term> arguments, bool hostBoundary = false)
    {
        if (!Runtime.OwnedTermsPilot506)
            return null;
        if (hostBoundary)
        {
            Runtime.ThrowIfHalted();
            Cancellation.ThrowIfCancellationRequested();
        }

        return new(this, arguments, hostBoundary);
    }

    internal Term OwnGuestResult506(Term value) => CurrentOwned506?.Frame.Retain(value) ?? value;

    internal void BeginOwnedTermRetirement506()
    {
        if (!Runtime.OwnedTermsPilot506)
            return;
        Dictionary.Close506();
        if (termHeap506 is { } heap)
        {
            OwnedTermRetirement506 = heap.RetireAsync();
            Runtime.TrackOwnedTermRetirement506(this, OwnedTermRetirement506);
        }
    }
}
