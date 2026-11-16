-module(entry_tagged_frame_probe).
-export([run/2]).
run({tag,X}, L) when is_atom(X) -> {X,lists:reverse(L)};
run(Original, _) -> Original.
