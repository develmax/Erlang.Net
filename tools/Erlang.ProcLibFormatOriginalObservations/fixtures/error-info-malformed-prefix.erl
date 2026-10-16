-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[malformed,{error_info,{error,boom,[]}}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
