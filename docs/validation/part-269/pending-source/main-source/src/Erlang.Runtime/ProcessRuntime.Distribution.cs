namespace Erlang;

public sealed partial class ProcessRuntime
{
    private RuntimeDistributionPending? distributionPending;

    public RuntimeDistributionPending DistributionPending
    {
        get
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, DistributionPendingDiagnostics.Closed);

                return distributionPending ??= new(this, gate, DistributionPendingParameters.DefaultMaximumNodes);
            }
        }
    }

    public RuntimeDistributionPending InitializeDistributionPending(int maximumNodes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumNodes);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, DistributionPendingDiagnostics.Closed);
            if (distributionPending is not null)
                throw new InvalidOperationException(DistributionPendingDiagnostics.AlreadyInitialized);
            distributionPending = new(this, gate, maximumNodes);

            return distributionPending;
        }
    }
}
