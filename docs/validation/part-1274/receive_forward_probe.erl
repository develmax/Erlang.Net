-module(receive_forward_probe).
-export([run/0, retained/0, first/0, second/1, finish/1]).
run() ->
    receive
        {run} ->
            Value = ?MODULE:first(),
            ?MODULE:finish([{message, ?MODULE:second(Value)}]),
            done
    end.
retained() ->
    receive
        {retained} ->
            Value = ?MODULE:first(),
            ?MODULE:second(Value),
            Value
    end.
first() -> self() ! first, value.
second(Value) -> self() ! second, {wrapped, Value}.
finish(Report) -> self() ! Report, ok.
