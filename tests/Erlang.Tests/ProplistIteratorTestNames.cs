namespace Erlang.Tests;

internal static class ProplistIteratorTestNames
{
    internal const string Test = "runtime/proplists-family-map-iterator-contracts";
    internal const string Resource = "Erlang.Tests.ProplistFamily.proplists_iterator_contract.erl";
    internal const string File = "proplists_iterator_contract.erl";
    internal const string Run = "run";
    internal const string FromMap = "from_map";
    internal const string Maps = "maps";
    internal const string ToList = "to_list";
    internal const string SourceFile = "file";
    internal const string SourceLine = "line";
    internal const string MapsFile = "maps.erl";
    internal const string ErrorInfo = "error_info";
    internal const string ErrorModule = "module";
    internal const string ErrorFormatter = "erl_stdlib_errors";
    internal const string Ok = "ok";
    internal const string Error = "error";
    internal const string BadMap = "badmap";
    internal const int MapsLine = 1303;
    internal static readonly (string Name, string Input, string? Expected)[] Cases =
    [
        ("none", "none", "[]"),
        ("literal_next", "{k,v,none}", "[{k,v}]"),
        ("ordered", "maps:iterator(#{1=>integer,1.0=>float,a=>atom},ordered)", "[{1,integer},{1.0,float},{a,atom}]"),
        ("reversed", "maps:iterator(#{1=>integer,1.0=>float,a=>atom},reversed)", "[{a,atom},{1.0,float},{1,integer}]"),
        ("wrapped_iterator", "{prefix,item,maps:iterator(#{a=>1,b=>2},ordered)}", "[{prefix,item},{a,1},{b,2}]"),
        ("malformed_atom", "invalid", null),
        ("malformed_tail", "{k,v,invalid}", null),
        ("malformed_keys", "[[missing]|#{a=>1}]", null)
    ];
}
