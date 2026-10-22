-module(erts_internal).
-export([purge_module/2,request_system_task/3]).
purge_module(_Module, _Op) ->
    erlang:nif_error(undefined).
request_system_task(_Pid, _Prio, _Request) ->
    erlang:nif_error(undefined).
