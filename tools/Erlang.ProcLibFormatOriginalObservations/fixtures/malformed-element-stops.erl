-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{pid,one},malformed,{status,never_printed}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
