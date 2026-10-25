-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:report_cb(#{label=>{proc_lib,crash},report=>[[{pid,sample}],[]]},#{chars_limit=>0}) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
