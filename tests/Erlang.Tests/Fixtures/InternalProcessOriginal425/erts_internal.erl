-module(erts_internal).
-export([group_leader/2,group_leader/3,process_display/2,process_flag/3,suspend_process/2,system_monitor/1,system_monitor/3]).

group_leader(_GL, _Pid) ->
    erlang:nif_error(undefined).

group_leader(_GL, _Pid, _Ref) ->
    erlang:nif_error(undefined).

process_display(_Pid, _Type) ->
    erlang:nif_error(undefined).

process_flag(_Pid, _Flag, _Value) ->
    erlang:nif_error(undefined).

suspend_process(_Suspendee, _OptList) ->
    erlang:nif_error(undefined).

system_monitor(_Session) ->
    erlang:nif_error(undefined).

system_monitor(_Session, _MonitorPid, _Options) ->
    erlang:nif_error(undefined).
