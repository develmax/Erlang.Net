namespace Erlang.Tests;

internal static class DistributionOriginal402Names
{
    internal const string CoreTest = "runtime/library402/original-distribution-core-portable-complete-outcomes";
    internal const string ConnectedTest = "runtime/library402/original-connected-portable-and-same-vm-identity";
    internal const string ReuseTest = "runtime/library402/original-creation-reuse-semantic-and-owned-handle";
    internal const string CoreObserver = "beam_distribution_observer402";
    internal const string CoreSubject = "beam_distribution_subject402";
    internal const string ConnectedObserver = "beam_distribution_connected402";
    internal const string OrdinaryMode = "ordinary";
    internal const string FixedMode = "fixed_creation";
    internal const string MinMode = "creation_min_clamp";
    internal const string MaxMode = "creation_uint32_max";
    internal const string CorePrefix = "distribution402-core-";
    internal const string ConnectedPrefix = "distribution402-connected-";
    internal const string ProbeResource = "distribution402-reuse-probe.etf";
    internal const string InitializeFixed = "initialize_fixed";
    internal const string Initialize = "initialize";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string ReuseProbe = "creation_noname_reuse_probe";
    internal const string Returned = "returned";
    internal const string Caught = "caught";
    internal const string Protocol = "same_vm_raw_identity_protocol";
    internal const string ProbeReturned = "probe_returned";
    internal const string WrongNode = "distribution402_wrong@localhost";
    internal const string NativeModule = DistributionState402Names.ModuleName;
    internal const string NativeAbort = DistributionState402Names.AbortFunction;
    internal const string NativeCreation = DistributionState402Names.CreationFunction;
    internal const string Failure = "Original distribution402 complete portable contract differs";
    internal const string ArtifactRoot = "artifacts/library402-permanent-observations";
    internal const string OwnedRunFormat = "N";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string ProbeFile = "probe.etf";
    internal const string ReceiptFile = "scope.json";
    internal const int CoreRows = 15;
    internal const int CorePortableRows = 11;
    internal const int ConnectedRows = 5;
    internal const int ConnectedPortableRows = 4;
    internal const int Profiles = 5;
    internal static readonly string[] CorePortable = ["creation_current", "pending_abort", "idle_repeat", "stale_generation", "high_word_alias", "low_word_alias", "stale_negative_id", "bad_node", "bad_handle", "bad_shape", "bad_generation_type"];
    internal static readonly string[] CoreRaw = ["wrong_node_raw", "ordinary_reference_raw", "serialized_handle_raw", "handle_raw"];
    internal static readonly string[] ConnectedPortable = ["creation_started", "connected_abort", "stale_connected", "controller_exit_then_idle"];
    internal const string ConnectedRaw = "wrong_node_connected_raw";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(180);
}
