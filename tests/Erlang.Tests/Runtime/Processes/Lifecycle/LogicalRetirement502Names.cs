namespace Erlang.Tests;

internal static class LogicalRetirement502Names
{
    internal const string Test = "runtime/process-liveness502/logical-exit-before-physical-dirty-completion";
    internal const string Prefix = "Erlang.Tests.LogicalRetirement502.";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "FD42BD841FA9091C472D93FF2C548312E329690D288FF580D2F8BE9A41F5E4E2";
    internal const string Returned = "returned.etf";
    internal const string Registered = "retirement_probe502_target";
    internal const string BeforeRelease = "before_release";
    internal const string AfterRelease = "after_release";
    internal const string Released = "released";
    internal const string BoundedTimeout = "bounded_timeout";
    internal const string Internal = "erts_internal";
    internal const string Failure = "Logical process retirement502 differs from controlled original OTP";
    internal const int WaitPending = -1;
    internal const int WaitReleased = 0;
    internal const int WaitTimedOut = 258;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal static readonly Atom BeforeAtom = Term.A(BeforeRelease);
    internal static readonly Atom AfterAtom = Term.A(AfterRelease);
    internal static readonly Atom ReleasedAtom = Term.A(Released);
    internal static readonly Atom TimeoutAtom = Term.A(BoundedTimeout);
    internal static readonly Atom TrueAtom = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom FalseAtom = Term.A(ErlangBooleanAtoms.False);
    internal static readonly Atom KillAtom = Term.A(ProcessExitSignals.Kill);
    internal static readonly Atom KilledAtom = Term.A(ProcessExitReasons.Killed);
    internal static readonly Atom DownAtom = Term.A(ProcessMessageTags.MonitorDown);
}
