namespace Erlang;

public sealed class DistributionOutputWouldBlockException : InvalidOperationException
{
    internal DistributionOutputWouldBlockException() : base(DistributionOutputLifecycleDiagnostics.WouldBlock)
    {
    }
}
