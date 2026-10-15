namespace Erlang.Tests;

internal static class NifLifetimeHostNames
{
    internal const string Test = "runtime/nif-owned-purge-discard-shutdown-and-real-unwind";
    internal const string Module = "nif_lifetime_host";
    internal const string Function = "fallback";
    internal const string Admit = "Admit";
    internal const string Invoke = "InvokeAsync";
    internal const string Close = "CloseNifLifetimeAsync";
    internal const string Retire = "RetireNifImageAsync";
    internal const string Gate = "gate";
    internal const string Pending = "pendingBeamImages";
    internal const string RuntimeClosing = "RuntimeClosing";
    internal const string CleanupReentry = "ReentrantRetirementOperation";
    internal const string RegistryReentry = "ReentrantRegistryShutdown";
    internal const string Broken = "Owned NIF lifecycle contract failed";
    internal const string CleanupFailure = "Injected owned NIF cleanup failure";
    internal const string CancellationFailure = "Injected process stop cleanup failure";
    internal const string Erlang = "erlang";
    internal const string Purge = "purge_module";
    internal const string Kill = "kill";
    internal const string Resource = ".BeamOnLoad.fresh_failure.new.beam_on_load_fresh_failure.beam";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}
