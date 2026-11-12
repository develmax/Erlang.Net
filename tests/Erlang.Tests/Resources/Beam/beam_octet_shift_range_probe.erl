-module(beam_octet_shift_range_probe).
-export([mixed/1, unknown/1]).

mixed(Value) when Value >= 0 ->
    with_lower_bound(Value, []);
mixed(_) ->
    negative.

unknown(Value) ->
    without_lower_bound(Value, []).

with_lower_bound(0, Acc) -> Acc;
with_lower_bound(Value, Acc) ->
    with_lower_bound(Value bsr 8, [Value band 255 | Acc]).

without_lower_bound(0, Acc) -> Acc;
without_lower_bound(Value, Acc) ->
    without_lower_bound(Value bsr 8, [Value band 255 | Acc]).
