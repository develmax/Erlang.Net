-module(erts_internal).
-export([is_process_executing_dirty/1]).
is_process_executing_dirty(_Pid) ->
    erlang:nif_error(undefined).
