-module(beam_terminal_calls).
-export([run/2]).
run(X, Y) ->
    erlang:list_to_atom(lists:concat([X, Y])).
