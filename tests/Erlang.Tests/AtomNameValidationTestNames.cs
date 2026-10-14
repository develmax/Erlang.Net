namespace Erlang.Tests;

internal static class AtomNameValidationTestNames
{
    internal const string Test = "terms/atom-name-unicode-scalar-validation-pool-cache-and-runtime-conversions";
    internal const string PrimordialType = "Erlang.ErlangPrimordialAtomNames";
    internal const string PrimordialField = "All";
    internal const string ListToAtom = "list_to_atom";
    internal const string BinaryToAtom = "binary_to_atom";
    internal const string AtomToBinary = "atom_to_binary";
    internal const string Utf8 = "utf8";
    internal const string FreshNamePrefix = "atom_name_registry_delta_272_";
    internal const string GuidFormat = "N";
    internal const string Failure = "Atom name validation contract failed";
    internal const string Accepted = "An invalid atom name was accepted";
    internal const string Supplementary = "\U00010000";
    internal const string Replacement = "\uFFFD";
    internal static readonly string[] InvalidNames =
    [
        "\uD800", "\uD801", "\uDC00", "\uDC00\uD800", "\uD800x", "invalid_atom_prefix\uD800"
    ];
    internal static readonly string[] CachedNames =
    [
        "ok", "error", "true", "false", "undefined", "normal", "timeout", "badarg", "noproc", "shutdown"
    ];
}
