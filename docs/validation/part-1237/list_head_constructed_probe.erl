-module(list_head_constructed_probe).
-export([scan/2, mixed/2]).
scan([skip | Rest], Acc) ->
    scan(Rest, [skipped | Acc]);
scan([Value | Rest], Acc) ->
    scan(Rest, [{value, Value} | Acc]);
scan([], Acc) ->
    Acc.
mixed([skip | Rest], Acc) ->
    mixed(Rest, [skipped | Acc]);
mixed([Value | Rest], Acc) ->
    mixed(Rest, identity({Value, Acc}));
mixed([], Acc) ->
    Acc.
identity(Value) -> Value.
