-module(beam_terminal_call_match).
-export([one/0, nested/0, integer/0, tagged/1]).
one() ->
    ok = beam_probe:reply().
nested() ->
    ok = beam_probe:accept(erase(key)).
integer() ->
    42 = beam_probe:reply().
tagged(X) ->
    {ok,_} = beam_probe:accept(X).
