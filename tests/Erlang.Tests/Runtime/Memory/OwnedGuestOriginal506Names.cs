namespace Erlang.Tests;

internal static class OwnedGuestOriginal506Names
{
    internal const string Test = "runtime/owned-guest506/original-compiled-worker-whole-values-and-stacks";
    internal const string ResourcePrefix = "Erlang.Tests.OwnedGuestOriginal506.";
    internal const string WorkerModule = "paired_worker506";
    internal const string EntryPoint = "run";
    internal const string WitnessKey = "paired_witness506";
    internal const string Ordinary = "ordinary";
    internal const string Owned = "private-owned";
    internal const string Raw = "observed.etf";
    internal const string DeterministicRaw = "observed-deterministic.etf";
    internal const string Witnesses = "witnesses.etf";
    internal const string Artifacts = "artifacts";
    internal const string OutputPrefix = "paired-observable506-";
    internal const string ErlangModule = "erlang";
    internal const string TermToBinary = "term_to_binary";
    internal const string MinorVersion = "minor_version";
    internal const string Deterministic = "deterministic";
    internal const string MissingResource = "Original guest-ownership fixture is missing: ";
    internal const string Mismatch = "Whole original guest-ownership protocol differs: ";
    internal const string PublicationFailure = "Original ownership observer control modules could not be published.";
    internal static readonly string[] RegistryModules =
    [
        "owned_probe506", "owned_guest506", "owned_observer506", "paired_worker506"
    ];
    internal static readonly string[] BeamFiles = ["owned_observer506.beam", "paired_worker506.beam"];
    internal static readonly string[] SourceFiles =
    [
        "owned_probe506.erl",
        "owned_guest506.erl"
    ];
}
