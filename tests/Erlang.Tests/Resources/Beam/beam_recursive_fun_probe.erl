-module(beam_recursive_fun_probe).
-export([countdown/0, captured/1]).

countdown() ->
    fun Loop(0) -> done;
        Loop(N) -> Loop(N - 1)
    end.

captured(Result) ->
    fun Loop(0) -> Result;
        Loop(N) -> Loop(N - 1)
    end.
