namespace Erlang.Tests;

internal static class BeamLoaderAdmissionNames
{
    internal const string SupportedOnLoadResource = "fresh_success.new.beam_on_load_fresh_success.beam";
    internal const string Admit = "FinishPreparedReferences";
    internal const string Ok = "ok";
    internal const string Failure = "Loader typed resource admission contract failed";
    internal const string RuntimeNamespace = "Erlang";
    internal const string Prepare = "prepare_loading";
    internal const string Query = "has_prepared_code_on_load";
    internal const string Finish = "finish_loading";
    internal const string OnLoad = "on_load";
    internal const string FinishOnLoad = "finish_after_on_load";
    internal const string Error = "error";
    internal const string Module = "module";
    internal const string ErrorInfo = "error_info";
    internal const string Formatter = "erl_erts_errors";
    internal const string BadFile = "badfile";
    internal const string ForeignModule = "loader_wrong_module";
    internal const string Extension = ".beam";
    internal const string Separator = ".";
    internal const string Missing = "loader_missing";
}
