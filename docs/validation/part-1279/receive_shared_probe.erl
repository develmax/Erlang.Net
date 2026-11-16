-module(receive_shared_probe).
-export([run/1, pack/2, wrap/1]).
run(Expected) ->
    receive
        {done, Value} -> Value;
        {item, Expected, Value} -> erlang:error({matched, Expected, Value});
        {item, Other, Value} ->
            Packed = ?MODULE:pack(marker, [Value, Other]),
            ?MODULE:wrap(Packed)
    after 0 -> timeout
    end.
pack(Tag, Values) -> {Tag, Values}.
wrap(Value) -> {wrapped, Value}.
