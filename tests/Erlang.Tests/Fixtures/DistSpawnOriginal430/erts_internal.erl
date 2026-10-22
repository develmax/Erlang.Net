-module(erts_internal).
-export([dist_spawn_request/4]).

dist_spawn_request(_Node, _MFA, _Opts, _Type) ->
    erlang:nif_error(undef).
