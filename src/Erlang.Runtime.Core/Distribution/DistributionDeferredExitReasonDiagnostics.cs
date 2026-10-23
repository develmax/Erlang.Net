namespace Erlang;

internal static class DistributionDeferredExitReasonDiagnostics
{
    internal const string Locked = "Deferred exit reason factories must execute outside runtime and mailbox gates.";
    internal const string Payload = "Deferred distribution reason is not an allowed versioned ETF payload.";
}
