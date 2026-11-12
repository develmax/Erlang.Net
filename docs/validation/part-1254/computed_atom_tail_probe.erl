-module(computed_atom_tail_probe).
-export([run/3]).

run(Selector, Left, Right) ->
    case pick(Selector) of
        pair -> pair(check(left, Left), check(right, Right));
        tuple -> wrap({Left, [Right]});
        keep -> same(Left, Right);
        retain -> same(Left, Right);
        local -> local
    end.

pick(Selector) -> maps:get(route, Selector).
check(Side, bad) -> erlang:error({invalid, Side});
check(_, Value) -> Value.
pair(Left, Right) -> {pair, Left, Right}.
wrap(Value) -> {wrapped, Value}.
same(Left, Right) -> {same, Left, Right}.
