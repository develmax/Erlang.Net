namespace Erlang;

internal sealed class DistributionSignalNestedWaitException : InvalidOperationException
{
    internal DistributionSignalNestedWaitException()
        : base(DistributionSignalDrainDiagnostics.NestedWait)
    {
    }
}
