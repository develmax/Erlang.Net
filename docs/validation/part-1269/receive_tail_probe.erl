-module(receive_tail_probe).
-export([read/1]).
read(Expected) ->
    receive
        {tag, Expected} -> matched;
        {tag, Other} when is_integer(Other) -> finish(Other);
        {tag, X, Y} -> {pair, X, Y};
        {retry, _, _, _} -> read(Expected);
        {fail, Value, _, _} -> erlang:error({failed, Value})
    after 0 -> timeout
    end.

finish(Other) -> {other, Other}.
