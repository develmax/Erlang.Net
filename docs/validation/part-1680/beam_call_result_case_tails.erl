-module(beam_call_result_case_tails).
-export([choose/0, classify/1, read/1]).
choose() ->
    Value = application:get_env(stdlib, beam_test_choice, none),
    case Value of
        none -> selected(default);
        _ -> selected(Value)
    end.
classify(Input) ->
    case read(Input) of
        missing -> selected(default);
        Other -> selected(Other)
    end.
read({ok, Value}) -> Value;
read(_) -> missing.
selected(Value) -> {selected, Value}.
