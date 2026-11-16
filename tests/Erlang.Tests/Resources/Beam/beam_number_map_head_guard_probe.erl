-module(beam_number_map_head_guard_probe).
-export([num/1, map/1, head/1, exact/1]).
num(X) when is_number(X) -> yes;
num(_) -> no.
map(X) when is_map(X) -> yes;
map(_) -> no.
head(X) when hd(X) -> yes;
head(_) -> no.
exact(X) when hd(X) =:= ok -> yes;
exact(_) -> no.
