namespace Erlang.Tests;

internal static class TupleMutation489Names
{
    internal const string TestPrefix = "runtime/bif-contract489/";
    internal const string Prefix = "Erlang.Tests.TupleMutation489.";
    internal const string Failure = "Tuple mutation contract resource is missing or differs";
    internal const string Manifest = "manifest.json";
    internal const string ManifestSha256 = "CCF5CBE1DE4817AAA9D51F2E9EC001A9F9EE65A9A684EE57BBD6D0892FD2411C";
    internal const string BeamExtension = ".beam";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string Separator = ".";
    internal static readonly (string Group, string Subject, int Values, int Profiles)[] Groups =
    [
        ("setelement", "tuple_mutation489_setelement", 80, 400),
        ("insert_element", "tuple_mutation489_insert_element", 80, 400),
        ("delete_element", "tuple_mutation489_delete_element", 80, 400),
        ("append_element", "tuple_mutation489_append_element", 11, 55),
        ("list_to_tuple", "tuple_mutation489_list_to_tuple", 16, 80),
        ("make_tuple2", "tuple_mutation489_make_tuple2", 11, 55),
        ("make_tuple3", "tuple_mutation489_make_tuple3", 66, 330)
    ];
}
