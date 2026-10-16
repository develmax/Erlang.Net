-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:report_cb(#{label=>{proc_lib,crash},report=>[[{registered_name,worker},{error_info,{error,boom,[]}},{trap_exit,false}],[]]},#{single_line=>true}) of
        Text -> {raw, Text}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
