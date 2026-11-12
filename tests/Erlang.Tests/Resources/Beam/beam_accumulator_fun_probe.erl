-module(beam_accumulator_fun_probe).
-export([make/1]).

make(Offset) ->
    fun Loop(0, Sum) -> Sum + Offset;
        Loop(N, Sum) -> Loop(N - 1, Sum + N)
    end.
