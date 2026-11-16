-module(beam_trim_failure_edges).
-export([external/3, matched/3]).
external(First, Last, Keep) ->
    Value = step(First),
    Result = lists:reverse(Value, Last),
    {Keep, Result}.
matched(First, Last, Keep) ->
    Value = step(First),
    {ok, Result} = pair(Value, Last),
    {Keep, Result}.
step(Value) ->
    Value.
pair(Value, Last) ->
    case Value of
        fail -> Last;
        _ -> {ok, {Value, Last}}
    end.
