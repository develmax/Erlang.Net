-module(named_fun_source_probe).
-export([make/1, self_fun/0, nested/0, shadow/0, immediate/1]).

make(Offset) ->
    Loop = outer,
    {Loop, fun Loop(0, Sum) -> Sum + Offset;
               Loop(N, Sum) -> Loop(N - 1, Sum + N)
           end}.

self_fun() -> fun Self() -> Self end.
nested() -> fun Outer() -> fun() -> Outer end end.
shadow() ->
    F = fun This(This) -> matched; This(_) -> other end,
    {F(F), F(shadowed)}.

immediate(Value) ->
    {fun() -> Value end(),
     fun Identity(X) -> X end(Value),
     case Value of _ -> fun() -> Value end end(),
     fun local/0()}.

local() -> local.