-module(nested_cons_call_probe).
-export([rewrite/1, same/1, literal/1, coupled/3]).
rewrite([{limit, Count},{exact, Count}|Tail]) ->
    [{exact, Count}|rewrite(Tail)];
rewrite([{limit, Count},{minimum, Min, Unit}|Tail])
  when Count >= Min, Count rem Unit =:= 0 ->
    [{exact, Count}|rewrite(Tail)];
rewrite([{limit, Count}|Tail]) ->
    [{fallback, Count}|rewrite(Tail)];
rewrite([Head|Tail]) ->
    [Head|rewrite(Tail)];
rewrite([]) -> [].
same([{pair, Value, Value}|Tail]) ->
    [{same, Value}|same(Tail)];
same([Head|Tail]) ->
    [Head|same(Tail)];
same(Other) -> Other.
literal([marker,{pair, fixed, Value}|Tail]) ->
    [Value|literal(Tail)];
literal([Head|Tail]) ->
    [Head|literal(Tail)];
literal(Other) -> Other.
coupled({key, Value}, [{pair, Value}|Tail], [_|Rest]) ->
    [{ok, Value}|coupled(Value, Tail, Rest)];
coupled(_, List, _) -> List.
