namespace Erlang;

// Issuance under the real registry M creates this one-shot literal grant.
// Its physical builder never has to reacquire M after VM admission closes.
internal sealed class PreparedLoaderGrant807 : IThreadPoolWorkItem
{
    private readonly object gate807 = new();
    private ModuleLiteralLease800? literals807;
    private bool claimed807, closed807, registered807;
    private PreparedLoaderReservation807? reservation807;
    internal ModuleRegistry Issuer807 { get; }
    internal PreparedBeamImage Image807 { get; }
    internal PreparedLoaderOwner807? FailedCandidate807 { get; set; }
    internal Exception? CleanupFailure807 { get; set; }
    internal TaskCompletionSource Closed807 { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource PhysicalCompleted807 { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal TaskCompletionSource Completed807 { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal PreparedLoaderGrant807(ModuleRegistry issuer, PreparedBeamImage image, ModuleLiteralLease800? literals)
    {
        Issuer807 = issuer;
        Image807 = image;
        literals807 = literals;
    }

    internal bool IsClosed807
    {
        get
        {
            lock (gate807) return closed807;
        }
    }
    internal bool IsRegistered807
    {
        get
        {
            lock (gate807) return registered807;
        }
    }

    internal ModuleLiteralLease800? Claim807(ModuleRegistry issuer, PreparedBeamImage image)
    {
        lock (gate807)
        {
            if (!ReferenceEquals(issuer, Issuer807) || !ReferenceEquals(image, Image807) || claimed807)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            ObjectDisposedException.ThrowIf(closed807, PreparedLoaderNames807.Closed);
            claimed807 = true;
            ModuleLiteralLease800? result = literals807;
            literals807 = null;

            return result;
        }
    }

    internal void PublishReservation807(PreparedLoaderReservation807 reservation, bool registered)
    {
        lock (gate807)
        {
            if (closed807 || reservation807 is not null)
                throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
            reservation807 = reservation;
            registered807 = registered;
        }
    }

    internal PreparedLoaderReservation807? TakeReservation807()
    {
        lock (gate807)
        {
            PreparedLoaderReservation807? result = reservation807;
            reservation807 = null;

            return result;
        }
    }

    internal void CloseAdmission807()
    {
        lock (gate807) closed807 = true;
        Closed807.TrySetResult();
    }

    internal void ReleaseUnclaimed807()
    {
        ModuleLiteralLease800? pending;
        lock (gate807)
        {
            pending = literals807;
            literals807 = null;
        }
        pending?.Dispose();
    }

    public void Execute() => Issuer807.CompletePreparedBuilderMetadata807(this);
}
