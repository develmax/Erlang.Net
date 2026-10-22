namespace Erlang;

internal static class ReNames411
{
    internal const string Module = "re";
    internal const string Formatter = "erl_stdlib_errors";
    internal const string Cause = "cause";
    internal const string BadOption = "badopt";
    internal const string Pattern = "re_pattern";
    internal const string Unsupported = "Opt-in re facet does not implement this valid option/input capability";
    internal const string Foreign = "Foreign re native owner";
    internal const string ImageHash = "27E75CEAEB1C33BAA8089071A47DB1F3473B537906353F874D687AE9A0854B05";
    internal const uint LoadFlags = 0x100 | 0x800;
    internal const uint Utf = 0x00080000;
    internal const int LoopBudget = 10000;
    internal const int LoopLimit = -76;
    internal const int Utf8Yield = -77;
    internal const int NoMatch = -1;
    internal static readonly HashSet<string> ValidHeldOptions = new(StringComparer.Ordinal)
    {
        "caseless", "dollar_endonly", "dotall", "extended", "firstline", "multiline",
        "no_auto_capture", "dupnames", "ungreedy", "ucp", "never_utf", "no_start_optimize", "export",
        "anchored", "notempty", "notempty_atstart", "notbol", "noteol", "global", "report_errors", "bsr_anycrlf", "bsr_unicode"
    };
}
