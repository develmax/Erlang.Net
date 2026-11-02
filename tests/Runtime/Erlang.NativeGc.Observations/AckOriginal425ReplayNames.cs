namespace Erlang.Tests;

internal static class AckOriginal425ReplayNames
{
    internal const string Command = "--ack-original425";
    internal const string Ordinary = "ordinary";
    internal const string Private = "private-owned";
    internal const string Inputs = "managed-inputs.json";
    internal const string Files = "Files";
    internal const string Name = "Name";
    internal const string Bytes = "Bytes";
    internal const string Sha = "Sha256";
    internal const string Facet = "erts_internal.beam";
    internal const string Worker = "beam_internal_process425.beam";
    internal const string Module = "beam_internal_process425";
    internal const string Encode = "term_to_binary";
    internal const string MinorVersion = "minor_version";
    internal const string Receipt = "managed-completion.json";
    internal const string Preparation = "preparation";
    internal const string ActorCleanup = "actor-cleanup";
    internal const string RuntimeCleanup = "runtime-cleanup";
    internal const string WorkerCleanup = "worker-cleanup";
    internal const string FacetCleanup = "facet-cleanup";
    internal const string OutputExists = "Fresh425 replay output directory already exists.";
    internal const string Failure = "Fresh425 managed replay failed; retain complete outputs and cleanup receipt.";
    internal const int ImageCount = 2;
    internal const int RouteCount = 2;
    internal const int MinorVersionValue = 2;
    internal const int NativeCount = 7;
    internal const int FacetExports = 9;
    internal const int Rows = 25;
    internal const int StackProfiles = 5;
    internal const int WholeRows = 150;
    internal static readonly string[] Dependencies = ["erlang", "file", "erts_internal"];
    internal static readonly (string Function, string File)[] Routes = [("run", "values.etf"), ("run_stacks", "stacks.etf")];
    internal static readonly BeamFunctionSignature[] NativeSignatures =
    [
        new("group_leader", 2), new("group_leader", 3), new("process_display", 2), new("process_flag", 3),
        new("suspend_process", 2), new("system_monitor", 1), new("system_monitor", 3)
    ];
}
