-module(erts_internal).
-export([spawn_request/4]).
spawn_request(_Module, _Function, _Args, _Opts) ->
    erlang:nif_error(undef).
