-module(shared_cons_probe).
-export([latest/2]).

latest([Head|Tail], Current) when Head > Current ->
    latest(Tail, Head);
latest([_|Tail], Current) ->
    latest(Tail, Current);
latest([], Current) ->
    Current.
