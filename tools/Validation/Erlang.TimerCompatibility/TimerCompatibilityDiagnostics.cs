internal static class TimerCompatibilityDiagnostics
{
    internal const string Usage = "Usage: Erlang.TimerCompatibility <repository> <oracle-directory> Managed|Beam [suite|depth:N]";
    internal const string Publication = "Timer test dependency publication failed";
    internal const string Owner = "Timer backend owner/export mismatch";
    internal const string Exports = "Timer exports differ from pinned OTP";
    internal const string Startup = "Timer startup scenario failed: ";
    internal const string OriginalFailure = "Pinned upstream testcase failed: ";
    internal const string Preparing = "Preparing: ";
    internal const string Upstream = "Upstream: ";
    internal const string Scenario = "Scenario: ";
}
