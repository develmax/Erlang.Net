-module(guarded_list_tail_probe).
-export([has_value/1]).
has_value([{outer, {inner, Value}, _} | _]) when Value =/= [] ->
    true;
has_value([_ | Rest]) ->
    has_value(Rest);
has_value([]) ->
    false.
