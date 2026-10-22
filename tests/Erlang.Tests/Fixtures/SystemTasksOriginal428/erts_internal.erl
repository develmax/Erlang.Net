-module(erts_internal).
-export([garbage_collect/1,request_system_task/3,request_system_task/4,check_dirty_process_code/2,dirty_process_handle_signals/1]).

garbage_collect(_Mode) ->
    erlang:nif_error(undefined).

request_system_task(_Pid, _Prio, _Request) ->
    erlang:nif_error(undefined).

request_system_task(_RequesterPid, _TargetPid, _Prio, _Request) ->
    erlang:nif_error(undefined).

check_dirty_process_code(_Pid,_Module) ->
    erlang:nif_error(undefined).

dirty_process_handle_signals(_Pid) ->
    erlang:nif_error(undefined).
