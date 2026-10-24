namespace Erlang.Tests;

internal static class DistDecode423Names
{
    internal const string CacheTest = "runtime/dist-decode423/explicit-private-cache-and-default-denial";
    internal const string EpochTest = "runtime/dist-decode423/cached-node-pid-port-reference-real-epoch";
    internal const string LocalTest = "runtime/dist-decode423/local-seed-identities-and-trailing-prefix";
    internal const string RawAtomTest = "runtime/dist-decode423/actual-internal-atom-ordinals-and-concurrent-registration";
    internal const string RawAtomPrefix = "dist423_raw_atom_", RawAtomAnchor = "false";
    internal const string OwnerTest = "runtime/dist-decode423/native-owner-cancel-and-closed-guards";
    internal const string Module = "erts_debug", Function = "dist_ext_to_term", Runtime = "erlang", Decode = "binary_to_term", Encode = "term_to_binary";
    internal const string First = "first423", Second = "second423", Last = "last423", Remote = "dist423@node", Local = "local";
    internal const string MakeReference = "make_ref";
    internal const string NativeEntry = "IsNativeEntry";
    internal const string KnownEpoch = "HasRetainedExternalEpoch", Functions = "functions";
    internal const string Failure = "Distribution debug decoder423 ownership/protocol assertion failed";
}
