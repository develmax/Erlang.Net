-module(beam_tuple_fun_probe).
-export([make/1, keep/2]).

make(Offset) ->
    {ok, fun Loop(0, Sum) -> Sum + Offset;
             Loop(N, Sum) -> Loop(N - 1, Sum + N)
         end}.

keep(Value, Captured) ->
    {Value, fun() -> Captured end, Captured}.
