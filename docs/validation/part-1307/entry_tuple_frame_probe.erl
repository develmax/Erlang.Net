-module(entry_tuple_frame_probe).
-export([run/2]).
run({X,_}, L) when is_atom(X) -> {X,lists:reverse(L)};
run(Original, _) -> Original.
