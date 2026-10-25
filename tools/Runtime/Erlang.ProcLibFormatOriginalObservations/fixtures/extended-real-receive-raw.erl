-module(oracle_proc_format).
-export([run/0,format_error/2]).
run() ->
    Stack=[{oracle_proc_format,run,[sample],[{error_info,#{module=>oracle_proc_format}}]}],
    try proc_lib:format([[{error_info,{error,boom,Stack}}],[]],unicode) of
        Text -> {raw,Text}
    catch
        Class:Reason -> {exception,Class,Reason}
    end.
format_error(_Reason,_Stack) -> P=self(),R=make_ref(),spawn(fun()->P!{release,R} end),receive {release,R}->#{reason=>"async custom reason"} end.
