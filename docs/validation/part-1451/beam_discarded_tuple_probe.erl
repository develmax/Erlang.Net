-module(beam_discarded_tuple_probe).
-export([discard/1, guarded/1, matched/1]).

discard(Items) ->
    _ = [notify(Value) || {field,Value} <- Items],
    ok.

guarded(Items) when is_list(Items) ->
    _ = [notify(Value) || {field,Value} <- Items],
    ok.

matched(Items) ->
    [notify(Value) || {field,Value} <- Items].

notify(Value) ->
    put(tuple_field_probe, Value),
    Value.
