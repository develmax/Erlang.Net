namespace Erlang;

public sealed partial class RuntimeDistributionPending
{
    internal DistributionOutputTransferScope BeginOutputTransfer() => new(this);

    internal void StartTransferredOutput(Action start)
    {
        // A HOST caller can hold an outer runtime gate. Never release or encode beneath that foreign lock.
        if (System.Threading.Monitor.IsEntered(gate) || DistributionOutputTransferScope.HasActiveScope)
            _ = Task.Run(start);
        else
            start();
    }
}
