-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:report_cb(#{label=>{wrong,crash},report=>[[],[]]},#{}) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
