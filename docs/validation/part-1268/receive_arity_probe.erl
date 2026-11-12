-module(receive_arity_probe).
-export([read/1]).
read(Expected) ->
    receive
        {tag, Expected} -> matched;
        {tag, Other} when is_integer(Other) -> {other, Other};
        {tag, X, Y} -> {pair, X, Y}
    after 0 -> timeout
    end.
