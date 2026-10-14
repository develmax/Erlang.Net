namespace Erlang;

internal static class BeamLoaderNames
{
    internal const string Prepare = "prepare_loading";
    internal const string HasOnLoad = "has_prepared_code_on_load";
    internal const string Finish = "finish_loading";
    internal const string CallOnLoad = "call_on_load_function";
    internal const string FinishOnLoad = "finish_after_on_load";
    internal const string LoadModule = "load_module";
    internal const string BadFile = "badfile";
    internal const string Error = "error";
    internal const string Ok = "ok";
    internal const string Module = "module";
    internal const string OnLoad = "on_load";
    internal const string Duplicated = "duplicated";
    internal const string NotPurged = "not_purged";
    internal const string ErrorInfoModule = "module";
    internal const string ErrorFormatter = "erl_erts_errors";
}
