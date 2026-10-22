namespace Erlang.Tests;

internal static class LibraryCollectionsNames
{
    internal const string Test = "runtime/library390/original-collections-whole-values-and-stacks";
    internal const string Refusals = "runtime/library390/beam-only-and-missing-dependency-refused";
    internal const string Prefix = "Erlang.Tests.LibraryCollections390.";
    internal const string Observer = "collections_observer390";
    internal const string MathModule = "math";
    internal const string OrderedDictionary = "orddict";
    internal const string RunEntry = "run";
    internal const string Stacks = "run_stacks";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string BeamExtension = ".beam";
    internal const string Failure = "Original collection backend contract observation failed";
    internal static readonly string[] Modules = ["lists", "queue", "ordsets", "sets", "dict", "orddict", "gb_sets", "gb_trees", "maps", "proplists"];
    internal static readonly string[] BeamOnly = ["sets", "dict", "orddict", "gb_sets", "gb_trees"];
    internal static readonly IReadOnlyDictionary<string, string[]> Dependencies = new Dictionary<string, string[]>
    {
        ["lists"] = [],
        ["queue"] = ["lists"],
        ["ordsets"] = ["lists"],
        ["sets"] = ["lists", "maps", "proplists"],
        ["dict"] = ["lists"],
        ["orddict"] = ["lists"],
        ["gb_sets"] = ["ordsets"],
        ["gb_trees"] = ["orddict"],
        ["maps"] = ["lists"],
        ["proplists"] = ["lists", "maps", "sets"]
    };
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["collections_observer390.beam"] = "83CF26AD5BFC05DB2CDCDDD8B6C86C69FDBBF777A0AD8A628AE0FFA0AAAF629F",
        ["collections_observer390.erl"] = "AAD9F82D33312EF25DE472D94BA9891DA37BDD4F59351BAAF8C164F662AA545D",
        ["dict.beam"] = "03C6221D87460F29CC813EDCC7B4541DEAF61A25F87EAEE0A8A60E9D2438490C",
        ["dict.erl"] = "6DF6D403DBA645C13D93F3BEF2AC1E8793765DB4717CCC5E05AA8820D28DB7C8",
        ["gb_sets.beam"] = "FEEE25741573E77EDDC07BD98625A2ABCBE7D60722A8619D13A6CE2B59D9CBF7",
        ["gb_sets.erl"] = "D06DE6E9BC2FF396E40140098C31DFFC51C9E3366CA18F538462DCBCB4FA8687",
        ["gb_trees.beam"] = "78BAECF006A8639A2076DA71A2C606927D09A26B5C629C9A44B7C4E4F130B026",
        ["gb_trees.erl"] = "890B641FA4E901C4F11BF65288EA12E8A8C0480D3A4867274F699FB4EF9C60A3",
        ["lists.beam"] = "8941092DC34F0470CD404979E1890DF514D83F2EEB3B0AA8A276553E76471262",
        ["lists.erl"] = "D429E541CA76ACA8C56BB9FF31130A3B636F33D3E1923A08683157BF5709AB47",
        ["maps.beam"] = "DCDBAF8551AE63258BDA7BC1415C7DCD2D3310AAA9036A34BD09F24E90966454",
        ["maps.erl"] = "350C707FF10180E1677CD911A4E981ABCCE2207DB69CE5C986BB1C000D69A816",
        ["orddict.beam"] = "346C4B0200A2F88031CB57BA555741896F088E13C38C4E44BA9C0A6CED7B1E42",
        ["orddict.erl"] = "CA497657F8813AEECD732003F85885B9545681918BEED4D29CF539790A6723E1",
        ["ordsets.beam"] = "A72097155EDAF57113FD8DF1BAA4F7F1360626F3E938180CA8D26461B7708E93",
        ["ordsets.erl"] = "526D1505D6CC482497F812879CDB41C533EA4BFC103E74850245F84C465CD70D",
        ["proplists.beam"] = "E77939A89B0ECAB0986CA46DFEDD04DEB4C1FB8ED406BB0568F83523E3A9B5D1",
        ["proplists.erl"] = "A0CABB89DD3FC82DDCEFC2C14502D12B6A4E0503A84B45867FCC18EE1F2E96A0",
        ["queue.beam"] = "93DA2CC6E6110DBFA39F512ED2C80A1497BE14B0167CFDF80FBC2CE79D812248",
        ["queue.erl"] = "61B322302ECF74578D72B442D2317EA200A5DEE4E0994B58C5E1E961F252B101",
        ["sets.beam"] = "90D3C1A11CEACA05EC072328BC55E8753B2E8EF1D488C5DAE67820E6818E5FB3",
        ["sets.erl"] = "74CD10E9FCF5E653AB7B999C733AC1B531F3B80A45C63AE5F87C638F3AA83812",
        ["stacks.etf"] = "9FD4EA4FB7D724227924A973638E482706427A19A7EC4E32669D9683C7212DDE",
        ["values.etf"] = "3BFF70E4F6CC4CB79D2584F9A246E5FE742DB85241E74A44D9616A7030543049",
    };
}
