-module(beam_multiple_tuple_fun_probe).
-export([make/2, shared/1, mixed/1]).

make(Left, Right) ->
    {fun() -> Right end, fun() -> Left end, Right}.

shared(Value) ->
    {fun() -> {first, Value} end, fun() -> {second, Value} end}.

mixed(Value) ->
    {fun Loop(0) -> Value;
         Loop(N) -> Loop(N - 1)
     end, fun() -> Value end}.
