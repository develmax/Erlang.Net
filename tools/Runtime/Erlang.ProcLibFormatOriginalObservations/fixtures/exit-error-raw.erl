-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{error_info,{exit,shutdown,[]}}],[]]) of
        Text -> {raw, Text}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
