namespace Erlang.Tests;

internal static class SourceLoadNifCaptureHostNames
{
    internal const string Source = "-module(common_load_nif_capture). -export([run/0]). run()->try erlang:load_nif(42,[]) catch error:badarg:Stack -> [{erlang,load_nif,[42,[]],Location},{common_load_nif_capture,run,0,_}|_]=Stack, [{error_info,ErrorInfo}]=Location, true=(ErrorInfo =:= #{module=>erl_erts_errors}), done end.";
    internal const string Module = "common_load_nif_capture";
    internal const string Function = "run";
    internal const string Done = "done";
    internal const string ErrorInfo = "error_info";
    internal const string Formatter = "erl_erts_errors";
    internal const string FormatterKey = "module";
}
