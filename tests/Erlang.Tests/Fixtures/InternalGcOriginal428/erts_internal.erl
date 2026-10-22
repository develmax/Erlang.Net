-module(erts_internal).
-export([garbage_collect/1]).

garbage_collect(_Mode) ->
    erlang:nif_error(undefined).
