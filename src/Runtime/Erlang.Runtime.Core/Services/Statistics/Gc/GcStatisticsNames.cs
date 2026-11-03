namespace Erlang;

internal static class GcStatisticsNames
{
    internal const string Module = ErlangModuleNames.Module;
    internal const string Helper = "gather_gc_info_result";
    internal const int Arity = 1;
    internal const string ImageResource = "Erlang.Runtime.PinnedOtp.erlang.beam";
    internal const string SourceResource = "Erlang.Runtime.PinnedOtp.erlang.erl";
    internal const string ProvenanceResource = "Erlang.Runtime.PinnedOtp.erlang.provenance.json";
    internal const string ImageSha256 = "F7CF9DAA675DA37CFEB5D03F685B3127A4B5D9B09350376D3FDFB55948B875BC";
    internal const string SourceSha256 = "00D7672A16325E43CA221C6D6CEB4200390E2A961A243C2E31DDC141AC145047";
    internal const string ProvenanceSha256 = "EF0E0CED7073A7339EB11958B67CBA1176623ED86560B6AFBA474EF76B045AB6";
    internal const string Pin = StatisticsTrapNames.Pin;
    internal const string Invalid = "Original GC helper image, participant or invocation identity differs";
    internal const string ProvenanceCommit = "SourceCommit";
}
