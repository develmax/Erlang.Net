internal static class SupplementalCleanupNames
{
    internal const string SdkMode = "sdk";
    internal const string OriginalMode = "original";
    internal const string Usage = "Usage: sdk|original source.erl report.json [pinned-erl]";
    internal const string ExistingReport = "Report already exists.";
    internal const string Success = "ok";
    internal const string Killed = "killed";
    internal const string NoCleanup = "no_cleanup";
    internal const string OriginalFailed = "Original Erlang child returned nonzero.";
    internal const string NoShell = "-noshell";
    internal const string NoInput = "-noinput";
    internal const string Evaluate = "-eval";
    internal const string SubjectAssemblyPrefix = "Erlang.";
    internal const string CollectorAssembly = "SupplementalCleanup";
    internal const string Pass = "PASS";
    internal const string Fail = "FAIL";
    internal const string Scope = "Guest observable recursive try-after external-kill behavior only; no OTP equivalence claim for CLR leases, Unwound, physical stack or memory.";
    internal const int Depth = 256;
    internal const int QuietMilliseconds = 100;
    internal static readonly TimeSpan OriginalDeadline = TimeSpan.FromSeconds(20);
    internal static readonly TimeSpan CleanupDeadline = TimeSpan.FromSeconds(5);
}
