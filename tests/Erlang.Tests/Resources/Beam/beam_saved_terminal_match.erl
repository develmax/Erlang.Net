-module(beam_saved_terminal_match).
-export([a/1,b/2,c/2,d/1]).
a(X) -> beam_probe:side(), ok = beam_probe:accept(X).
b(X,Y) -> beam_probe:side(), ok = beam_probe:pair(X,Y).
c(X,Y) ->
    Z = beam_probe:accept(X),
    ok = beam_probe:pair(Y,Z).
d(X) ->
    Y = beam_probe:accept(X),
    beam_probe:side(),
    ok = beam_probe:accept(Y).
