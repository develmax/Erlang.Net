-module(beam_full_closure_frame).
-export([run/1, capture/2]).

run(Values) ->
    lists:foldl(fun(Value, Acc) ->
        case cerl:is_c_var(Value) of
            true -> [Value | Acc];
            false -> Acc
        end
    end, [], Values).

capture(Default, Values) ->
    lists:map(fun(Value) ->
        case cerl:is_c_var(Value) of
            true -> Value;
            false -> Default
        end
    end, Values).
