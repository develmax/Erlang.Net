-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[],[]],latin1) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
