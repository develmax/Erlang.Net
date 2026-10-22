-module(erts_internal).
-export([system_check/1,gather_system_check_result/1]).
system_check(_Type) ->
    erlang:nif_error(undefined).
gather_system_check_result(Ref) when is_reference(Ref) ->
    gather_system_check_result(Ref, erlang:system_info(schedulers)).
gather_system_check_result(_Ref, 0) ->
    ok;
gather_system_check_result(Ref, N) ->
    receive
        Ref ->
            gather_system_check_result(Ref, N - 1)
    end.
