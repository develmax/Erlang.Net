-module(receive_projection_probe).
-export([project/0]).
project() ->
    receive
        {take, Value} -> Value;
        {ignore, _Unused, _AlsoUnused} -> ignored
    after 0 -> timeout
    end.
