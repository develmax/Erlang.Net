-module(size_import_map_size).
-import(erlang,[map_size/1]).
-export([run/0,call/1]).
run() ->
    V=bad,
    try call(V) of _->error(missing_failure)
    catch error:Reason:Stack ->
        {badmap,V}=Reason,
        [{erlang,map_size,[V],[{error_info,ErrorInfo}]}|_]=Stack,true=(ErrorInfo =:= #{module=>erl_erts_errors}),done
    end.
call(V) -> map_size(V).
