namespace Erlang.Tests;

internal static class ProplistHashHostNames
{
    internal const string HashTest = "runtime/maps/internal-hash-native-vectors";
    internal const string CursorTest = "runtime/maps/raw-integer-binary-cursors";
    internal const string SourceResource = "Erlang.Tests.ProplistInternalHash.proplists_raw_cursor_contract.erl";
    internal const string SourceFile = "proplists_raw_cursor_contract.erl";
    internal const string Run = "run";
    internal const string KnownHashReport = "artifacts/proplists-internal-hash-known-domain.json";
    internal const string KnownCursorReport = "artifacts/proplists-hamt-known-domains.json";
    internal const string TupleAtomCase = "hamt33_tuple_keys";
    internal const string AtomCursorCase = "hamt33_atom_keys";
    internal const int NativeCount = 20;
    internal const int PrimitiveCount = 19;
    internal const int CursorCount = 18;
    internal const int SupportedCursorCount = 16;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal static readonly string[] HashCases = ["integer_zero", "integer_one", "integer_minus_one", "small_max", "small_min", "positive_big_boundary", "negative_big_boundary", "big_word", "big_two_words", "float_zero", "float_fraction", "binary_empty", "binary_three", "bitstring_partial", "tuple_empty", "tuple_integers", "list_empty", "list_bytes", "list_improper", "atom_key"];
    internal const string HashResource = "Erlang.Tests.ProplistInternalHash.native-hashes.etf";
    internal const string CursorResource = "Erlang.Tests.ProplistInternalHash.raw-cursors.etf";
    internal const string HashType = "Erlang.OtpMapInternalHash64";
    internal const string HashMethod = "Hash";
    internal const string ProviderType = "Erlang.OtpMapOwnAtomIdentityProvider";
    internal const string AtomCase = "atom_key";
    internal const string Ok = "ok";
    internal const string Error = "error";
    internal const string Proplists = "proplists";
    internal const string FromMap = "from_map";
    internal const string InputFailure = "Original hash/cursor test resource has unexpected shape or count.";
    internal const string HashMismatch = "Internal hash differs from independently admitted native vector: ";
    internal const string CursorSequenceMismatch = "Supported raw cursor whole-row sequence differs from original.";
    internal const string CursorMismatch = "Raw cursor full outcome differs from independently admitted original: ";
    internal const string MissingEngine = "Internal hash engine/provider is not integrated.";
    internal static readonly string[] CursorCases = ["flat32_one", "flat32_fifteen", "flat32_limit", "flat32_over", "flat32_negative", "flat32_big", "hamt33_zero", "hamt33_one", "hamt33_two", "hamt33_fifteen", "hamt33_sixteen", "hamt33_negative_small", "hamt33_small_min", "hamt33_big", "hamt33_negative_big", "hamt33_binary_keys"];
}
