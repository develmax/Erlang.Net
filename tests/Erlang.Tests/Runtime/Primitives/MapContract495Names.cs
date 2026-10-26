namespace Erlang.Tests;

internal static class MapContract495Names
{
    internal const string TestPrefix = "runtime/bif-contract495/";
    internal const string Prefix = "Erlang.Tests.MapContract495.";
    internal const string Failure = "Map BIF contract resource is missing or differs";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "1188B222429E9986427FF0EC963580895A6B81E06FDA5F72DDBD96DC0BE515B1";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string Separator = ".";
    internal const string Records = "records";
    internal const string Internal = "erts_internal";
    internal const string Debug = "erts_debug";
    internal const string Erlang = "erlang";
    internal const string PrimFile = "prim_file";
    internal const string Binary = "binary";
    internal const string Lists = "lists";
    internal const string Maps = "maps";
    internal static readonly (string Group, string Subject, int Values, int Profiles)[] Groups =
    [
        ("map_size", "map_contract495_map_size", 53, 265),
        ("map_get", "map_contract495_map_get", 71, 355),
        ("is_map_key", "map_contract495_is_map_key", 70, 350)
    ];
}
