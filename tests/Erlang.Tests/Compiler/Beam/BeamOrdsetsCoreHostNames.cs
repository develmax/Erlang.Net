using Erlang;

namespace Erlang.Tests;

internal static class BeamOrdsetsCoreHostNames
{
    internal const string Support = "ordsets_core_support";
    internal const string Binary = "binary";
    internal const string BeamExtension = ".beam";
    internal const string Case = "runtime/beam/ordsets381/original-core-values-and-full-stacks";
    internal const string ResourcePrefix = "Erlang.Tests.BeamOrdsets381.";
    internal const string Lists = "lists";
    internal const string Ordsets = "ordsets";
    internal const string Erlang = "erlang";
    internal const string Observer = "ordsets_core_observe";
    internal const string Worker = "ordsets_core_worker";
    internal const string Run = "run";
    internal const string TermToBinary = "term_to_binary";
    internal const string Stacks = "run_stacks";
    internal const string ModuleInfo = "module_info";
    internal const string Normal = "normal";
    internal const string Failure = "Original compiled ordsets corpus or backend ownership differs";
    internal const string FailureDirectory = "artifacts/beam-contract-failures";
    internal const string ActualSuffix = ".actual";
    internal const string ExpectedSuffix = ".expected";
    internal const int ValueCount = 112;
    internal const int ProfileCount = 560;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(75);
    internal static readonly string[] Fixtures = ["ordsets_core_observe", "ordsets_core_support", Worker];
    internal static readonly (string Entry, string Resource, int Count)[] Observations = [(Run, "values.etf", ValueCount), (Stacks, "stacks.etf", ProfileCount)];
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["lists.beam"] = "8941092DC34F0470CD404979E1890DF514D83F2EEB3B0AA8A276553E76471262",
        ["ordsets.beam"] = "417595CFD47FF9DC43673CEE03083776CBACD3ABB61166BF58C61DC6A13CCE1D",
        ["ordsets_core_observe.beam"] = "09F77665EAB8EA756415FCFC4791E765691F05D7B88AB29D090FF15E43292E4C",
        ["ordsets_core_support.beam"] = "E96720C753F9438E66367C71A90C5D0C1E3AA29091B178799986E36A8438022F",
        ["ordsets_core_worker.beam"] = "E345AE81943C41F82F3CBCA24B963D2D25CB24C0C15E2A0B21B72A65757D6B3F",
        ["values.etf"] = "B961EDDE3A850DA349FEDB8D69AA1F3A6B65E052E8562EAEDF8163EBE8E154DF",
        ["stacks.etf"] = "FF3848C12E29E5CA904CE985710852CFF0924AEA95A716DAD6FF10557063F0F1",
    };
}
