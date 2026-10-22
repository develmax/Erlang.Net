-module(erts_internal).
-export([get_dflags/0]).
get_dflags() ->
    erlang:nif_error(undefined).
