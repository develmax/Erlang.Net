-module(beam_bound_case_selector).
-export([run/2]).
run(Value, Fallback) ->
    Result = select(Value),
    case Result of
        {ok, Answer} -> Answer;
        _ -> Fallback
    end.
select(hit) -> {ok, 42};
select(miss) -> error;
select(Other) -> {unexpected, Other}.
