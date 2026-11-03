namespace Erlang.Tests;

internal static class OrdsetsTermContractNames
{
    internal const string Test = "runtime/ordsets/whole-term-order-and-runtime-hamt-equality";
    internal const string Resource = "Erlang.Tests.OrdsetsTermContracts.matrix.etf";
    internal const string TestPrefix = Test + "/";
    internal const string BeamTestPrefix = TestPrefix + "beam/";
    internal const string Module = "ordsets";
    internal const string Equality = "is_equal";
    internal const string Membership = "is_element";
    internal const string IsSet = "is_set";
    internal const string True = "true";
    internal const string Failure = "Pinned OTP term comparison contract differs";
    internal const string Output = "artifacts/ordsets-term-contracts/runtime.json";
    internal const string Observer = "ordsets_term_contracts";
    internal const string ObserverImage = "Erlang.Tests.OrdsetsTermContracts.ordsets_term_contracts.beam";
    internal const string Pool = "pool";
    internal const string ProbeEntry = "ordsets_probes";
    internal const string ProbeResource = "Erlang.Tests.OrdsetsTermContracts.probes.etf";
    internal const int ProbeCount = 10;
    internal const int TermCount = 190;
    internal const int PairCount = TermCount * TermCount;
    internal const int TermsPerShard = 16;
    internal const int ShardCount = (TermCount + TermsPerShard - 1) / TermsPerShard;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(120);
}
