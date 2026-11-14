-module(beam_exact_list_projection).
-export([start/1, finish/1, keep/1]).
start([_, _, Last]) ->
    Value = erlang:abs(Last),
    {last, Value}.
finish([First, _]) ->
    Value = erlang:abs(First),
    {first, Value}.
keep([First, Middle, Last]) ->
    Value = erlang:abs(Middle),
    {First, Value, Last}.
