-module(tuple_entry_probe).
-export([run/1, work/1]).
run({First, 1.5, Second}) ->
    Result = ?MODULE:work(First),
    {Result, Second}.
work(Value) -> {wrapped, Value}.

