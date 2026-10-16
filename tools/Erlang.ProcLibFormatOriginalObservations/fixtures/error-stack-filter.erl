-module(oracle_proc_format).
-export([run/0]).
run() ->
    try proc_lib:format([[{error_info,{error,boom,[{sample,run,0,[]},{proc_lib,init_p,5,[]},{erl_eval,do_apply,7,[]}]}}],[]]) of
        Text -> {text, lists:flatten(Text)}
    catch
        Class:Reason -> {exception, Class, Reason}
    end.
