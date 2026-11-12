-module(entry_list_probe).
-export([read/1, original/1]).
read(Input) ->
    Value = case Input of [] -> [default]; [_|_] -> Input end,
    erlang:put(entry_key, Value),
    {ok, Value}.
original(Input) ->
    Value = case Input of [] -> [default]; [_|_] -> Input end,
    {Input, Value, erlang:self()}.
