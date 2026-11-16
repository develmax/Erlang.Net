-module(beam_guarded_list_scan_probe).
-export([scan/1, token/1]).

scan(List) ->
    scan(List, []).

scan([], Acc) ->
    lists:reverse([{done, 1, done}|Acc]);
scan(List, Acc) ->
    case token(List) of
        {Token, Rest} -> scan(Rest, [Token|Acc])
    end.

token([$ , H|T]) ->
    token([H|T]);
token([H|T]) when H==$A; H==$Q ->
    {{first,1,H},T};
token([H|T]) when H==$B; H==$C ->
    {{second,1,H},T};
token([H|T]) when H==$D; H==$E ->
    {{second,1,H},T};
token("="++T) ->
    {{equals,1,"="},T};
token([H|_]) ->
    exit({error,{bad_character,H}}).
