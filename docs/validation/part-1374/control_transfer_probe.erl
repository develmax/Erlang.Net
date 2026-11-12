-module(control_transfer_probe).
-export([check/3, unwrap/2, local_throw/1]).
-compile({no_auto_import,[throw/1]}).

check([H | T], [K | Ks], Tail) ->
    case lists:member(H, K) of
        false -> erlang:throw(false);
        true -> check(T, Ks, Tail)
    end;
check(Tail, [], Tail) -> true;
check(_, _, _) -> erlang:throw(false).

unwrap([Value | _], _) -> Value;
unwrap(Term, Context) -> erlang:exit({invalid, {Term, Context}}).

local_throw({wrapped, Value}) -> Value;
local_throw(Value) -> throw(Value).

throw(Value) -> {local, Value}.
