-module(receive_projection_probe).
-export([project/0, repeated/0]).
project() ->
    receive
        {take, Value} -> Value;
        {ignore, _Unused, _AlsoUnused} -> ignored
    after 0 -> timeout
    end.


repeated() ->
    receive
        {same, Value, Value} -> Value
    after 0 -> timeout
    end.

