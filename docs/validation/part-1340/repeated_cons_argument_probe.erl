-module(repeated_cons_argument_probe).
-export([dedup/2, keep/2, keep_right/2, duplicate_tail/2, fallback/2]).
dedup([Head|Tail], Head) -> dedup(Tail, Head);
dedup([Head|Tail], _) -> [Head|dedup(Tail, Head)];
dedup([], _) -> [].
keep(Key, [{tag,Key,_}=Pair|Tail]) -> [Pair|keep(Key, Tail)];
keep(Key, [_|Tail]) -> keep(Key, Tail);
keep(_, []) -> [].
keep_right([{Key,_}=Pair|Tail], Key) -> [Pair|keep_right(Tail, Key)];
keep_right([_|Tail], Key) -> keep_right(Tail, Key);
keep_right([], _) -> [].
duplicate_tail([Head|Tail], Tail) -> [{same,Head}|duplicate_tail(Tail, Tail)];
duplicate_tail([Head|Tail], _) -> [Head|duplicate_tail(Tail, [])];
duplicate_tail([], _) -> [].
fallback([Head|Tail], Head) -> [Head|fallback(Tail, Head)];
fallback(Original, _) -> Original.
