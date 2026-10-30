namespace Erlang;

internal static class StatisticsTrapNames
{
    internal const string Module = "erts_internal";
    internal const string AwaitMicrostates = "await_microstate_accounting_modifications";
    internal const string GatherMicrostates = "gather_microstate_accounting_result";
    internal const string AwaitWallTime = "await_sched_wall_time_modifications";
    internal const string GatherWallTime = "gather_sched_wall_time_result";
    internal const string Resource = "Erlang.Runtime.PinnedOtp.erts_internal.beam";
    internal const string SourceResource = "Erlang.Runtime.PinnedOtp.erts_internal.erl";
    internal const string ProvenanceResource = "Erlang.Runtime.PinnedOtp.erts_internal.provenance.json";
    internal const string ImageSha256 = "6740F088697BCF6285227B95D3B91ECFF38AE89ADF9A1B6976413CEC2AF7B806";
    internal const string SourceSha256 = "22B6A291C32778EDB84DC1E9BB2CFD5AF44CA7E9F0AE53CB01F3A674218BC260";
    internal const string Pin = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string InvalidImage = "Original statistics helper image or invocation identity differs";
    internal const string System = "system";
}
