-module(oracle_proc_format).
-export([run/0,format_error/2]).
run() ->
    Stack=[{oracle_proc_format,run,[sample],[{error_info,#{module=>oracle_proc_format}}]}],
    try proc_lib:format([[{error_info,{error,boom,Stack}}],[]],unicode) of
        Text -> {text,lists:flatten(Text)}
    catch
        Class:Reason -> {exception,Class,Reason}
    end.
format_error(_Reason,_Stack) -> exit(callback_exited).
