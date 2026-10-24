namespace Erlang.Tests;

internal static class LibraryBinaryNames
{
    internal const string Test = "runtime/library390/original-binary-whole-values-and-stacks";
    internal const string Prefix = "Erlang.Tests.LibraryBinary390.";
    internal const string Module = "binary";
    internal const string Lists = "lists";
    internal const string Observer = "beam_binary_observer390";
    internal const string RunEntry = "run";
    internal const string Stacks = "run_stacks";
    internal const string ValuesFile = "values.etf";
    internal const string StacksFile = "stacks.etf";
    internal const string BeamExtension = ".beam";
    internal const string Failure = "Original binary backend contract observation failed";
    internal const int NativePrimitiveCount = 22;
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["beam_binary_observer390.beam"] = "5DF957E353EBB41BBE58B972F25FF8DBE3986D11667FC5178A97A22082923871",
        ["beam_binary_observer390.erl"] = "475C6F23BA881AED8DB88382D27948C72929F1FBA503798396F0131CE8586880",
        ["binary.beam"] = "E398EE7CFEEFFBE73A47AF70BFCB7D7BE1B221F101F7BC52ABF28EE70DB19553",
        ["binary.erl"] = "824EB8574AB19B7DF1A80402562A6B05012E9B1B464916E5694E07E601FB24DC",
        ["lists.beam"] = "8941092DC34F0470CD404979E1890DF514D83F2EEB3B0AA8A276553E76471262",
        ["stacks.etf"] = "CC2D3ABB325812163570914ECC12ECB72BDA50A7154D966F317BCB0A277F4BB2",
        ["values.etf"] = "EFB0AF3F00C214E18F9B8CB99FD5F00E70C7B862A58441A6FAB3291361BF31CE",
    };
}
