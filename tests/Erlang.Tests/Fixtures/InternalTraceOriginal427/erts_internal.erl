-module(erts_internal).
-export([trace/3,trace/4,trace_info/3,trace_pattern/3,trace_pattern/4,trace_session_create/3,trace_session_destroy/1]).

trace(_PidSpec, _How, _FlagList) ->
    erlang:nif_error(undefined).

trace(_Session, _PidSpec, _How, _FlagList) ->
    erlang:nif_error(undefined).

trace_info(_Session, _PidPortFuncEvent, _Item) ->
    erlang:nif_error(undefined).

trace_pattern(_MFA, _MatchSpec, _FlagList) ->
    erlang:nif_error(undefined).

trace_pattern(_Session, _MFA, _MatchSpec, _FlagList) ->
    erlang:nif_error(undefined).

trace_session_create(_Name, _Tracer, _TracerOpts) ->
    erlang:nif_error(undefined).

trace_session_destroy(_TraceSession) ->
    erlang:nif_error(undefined).
