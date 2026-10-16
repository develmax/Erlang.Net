-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{pid,sample}],[{neighbour,[{pid,other},{status,waiting}]},{neighbour,[{registered_name,another}]}]]) of
        Text -> {raw, Text}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
