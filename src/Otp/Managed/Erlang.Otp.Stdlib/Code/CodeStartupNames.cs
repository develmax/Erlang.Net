namespace Erlang;

internal static class CodeStartupNames
{
    internal const string PathCacheArgument = "code_path_cache";
    internal const string RootArgument = "root";
    internal const string ModeArgument = "mode";
    internal const string NoStickArgument = "nostick";
    internal const string ModuleInformationKey = "module";
    internal const string CompilerApplication = "compiler";
    internal const string StdlibApplication = "stdlib";
    internal const string KernelApplication = "kernel";
    internal const string RuntimeApplication = "erts";
    internal const string EbinDirectory = "ebin";
    internal const string WarningLevel = "warning";
    internal const string MfaMetadata = "mfa";
    internal const string StartGetModeFunction = "start_get_mode";
    internal const string RemovedCacheWarning = "The code path cache functionality has been removed";
    internal const string MultipleModesWarning = "Multiple -mode given to erl, using the first, ~p";
}
