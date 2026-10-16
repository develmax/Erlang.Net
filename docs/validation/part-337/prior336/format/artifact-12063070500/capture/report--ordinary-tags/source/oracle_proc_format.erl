-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{registered_name,worker},{message_queue_len,0},{trap_exit,false}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
