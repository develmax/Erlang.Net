-module(branch_cons_call_probe).
-export([rewrite/1, fallback/1, guarded/1, duplicate/1]).
rewrite([{input, Value}|Tail]) -> [{output, Value}|rewrite(Tail)];
rewrite([Head|Tail]) -> [Head|rewrite(Tail)];
rewrite([]) -> [].
fallback([{input, Value}|Tail]) -> [{output, Value}|fallback(Tail)];
fallback([Head|Tail]) -> [Head|fallback(Tail)];
fallback(Other) -> Other.
guarded([Head|Tail]) when is_atom(Head) -> [{atom, Head}|guarded(Tail)];
guarded([Head|Tail]) -> [Head|guarded(Tail)];
guarded([]) -> [].
duplicate([{input, First, Second}|Tail]) ->
    [{Second, First, Second}|duplicate(Tail)];
duplicate([Head|Tail]) -> [Head|duplicate(Tail)];
duplicate([]) -> [].
