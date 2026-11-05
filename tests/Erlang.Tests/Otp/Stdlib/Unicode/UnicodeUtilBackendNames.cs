namespace Erlang.Tests;

internal static class UnicodeUtilBackendNames
{
    internal const string Managed = "runtime/unicode-util/fully-managed-closure-and-original-contracts";
    internal const string Beam = "runtime/unicode-util/original-backend-and-original-contracts";
    internal const string Failure = "Unicode utility backend differs from the pinned OTP contract";
    internal const string Prefix = "Erlang.Tests.UnicodeUtilBackend.";
    internal const string Observer = "unicode_util_port_cases";
    internal const string Module = "unicode_util";
    internal const int Batch = 32;
    internal const int Partitions = 4;
    internal const int CoreCases = 14801;
    internal const int DeepNesting = 12000;
    // Bounded interleaved differential partition budget; actor commands and cleanup remain bounded.
    internal static readonly TimeSpan CorpusDeadline = TimeSpan.FromMinutes(2);
    internal static readonly int[] BalineseConsonants = [6925, 6926, 6927, 6928, 6929, 6930];
    internal const int DevanagariVirama = 2381;
    internal const int DevanagariKa = 2325;
}
