-module(beam_disjunctive_forward_probe).
-export([run/2, run/3]).
-record(left, {value}).
run(Left, Value) when is_record(Left, left), (is_list(Value) orelse is_binary(Value)) ->
    run(Left, Value, []).
run(Left, Value, Options) ->
    {Left, Value, Options}.
