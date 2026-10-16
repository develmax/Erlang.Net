-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:report_cb(#{label=>{proc_lib,crash},report=>[[{messages,lists:seq(1,80)},{error_info,{error,boom,[]}}],[{neighbour,[{pid,other},{messages,lists:seq(1,40)}]}]]},#{chars_limit=>80}) of
        Text -> {raw, Text}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
