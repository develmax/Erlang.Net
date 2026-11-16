-module(receive_list_probe).
-export([run/0, retained/0, pack/2, wrap/1]).
run() ->
    receive
        {run, First, Second} ->
            Value = ?MODULE:pack(marker, [First, Second]),
            ?MODULE:wrap(Value)
    end.
retained() ->
    receive
        {retained, First, Second} ->
            Value = ?MODULE:pack(marker, [First, Second]),
            {?MODULE:wrap(Value), First}
    end.
pack(Tag, Values) -> {Tag, Values}.
wrap(Value) -> {wrapped, Value}.
