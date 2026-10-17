-module(size_import_bit_size).
-import(erlang,[bit_size/1]).
-export([run/0,call/1]).
run() ->
    V=bad,
    try call(V) of _->error(missing_failure)
    catch error:Reason:Stack ->
        badarg=Reason,
        [{erlang,bit_size,[V],[{error_info,ErrorInfo}]}|_]=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done
    end.
call(V) -> bit_size(V).
