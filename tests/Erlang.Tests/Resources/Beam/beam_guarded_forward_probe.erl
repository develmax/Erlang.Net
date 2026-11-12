-module(beam_guarded_forward_probe).
-export([run/2, run/3]).
-record(left, {value}).
-record(right, {value}).
run(Left, Right) when is_record(Left, left), is_record(Right, right) ->
    run(Left, Right, []).
run(Left, Right, Options) ->
    {Left, Right, Options}.
