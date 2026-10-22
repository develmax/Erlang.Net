-module(erts_debug).
-export([dist_ext_to_term/2]).
dist_ext_to_term(_, _) ->
    erlang:nif_error(undef).
