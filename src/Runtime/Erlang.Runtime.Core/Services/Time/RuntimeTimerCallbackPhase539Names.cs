namespace Erlang;

internal static class RuntimeTimerCallbackPhase539Names
{
    internal const string AttachAfterAdmission = "Timer callback phase instrumentation must attach once before timer admission.";
    internal const string UnderGate = "Timer callback phase wait entered under the runtime monitor.";
    internal const string DuplicateClaim = "Timer callback phase was claimed more than once.";
    internal const string ReleaseMissing = "Timer callback phase owner did not release its bounded gate.";
    // Instrumentation watchdog only; no actual timer deadline is altered.
    internal static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);
}
