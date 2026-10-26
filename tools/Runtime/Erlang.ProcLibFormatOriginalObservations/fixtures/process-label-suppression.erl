-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{process_label,undefined},{process_label,[]},{process_label,false}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
