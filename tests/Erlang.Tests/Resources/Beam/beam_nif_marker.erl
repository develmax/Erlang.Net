-module(beam_nif_marker).
-export([native/0,plain/0,stub/2]).
-nifs([native/0]).
native() -> fallback.
plain() -> ok.

stub(_, _) ->
    erlang:nif_error(undef).
