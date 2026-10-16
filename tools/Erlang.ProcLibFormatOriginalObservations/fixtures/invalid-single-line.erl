-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:report_cb(#{label=>{proc_lib,crash},report=>[[],[]]},#{single_line=>unexpected}) of
        Text -> {unexpected,Text}
    catch
        error:function_clause -> ok
    end.
