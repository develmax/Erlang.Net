-module(beam_linear_frame_trim).
-export([run/3, run_ext/3]).
run(First, Last, Keep) ->
    Value = step(First),
    Result = pair(Value, Last),
    {Keep, Result}.
run_ext(First, Last, Keep) ->
    Value = erlang:integer_to_list(First),
    Result = lists:append(Value, Last),
    {Keep, Result}.
step(Value) ->
    erlang:abs(Value).
pair(Value, Last) ->
    {Value, Last}.
