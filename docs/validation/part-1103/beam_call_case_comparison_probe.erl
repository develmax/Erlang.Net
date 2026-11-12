-module(beam_call_case_comparison_probe).
-export([same/2, different/2, fresh/1]).
same(Input, Expected) ->
    case helper(Input) of Expected -> true; _ -> false end.
different(Input, Expected) ->
    case helper(Input) of Expected -> false; _ -> true end.
fresh(Input) ->
    case helper(Input) of _Fresh -> true; _ -> false end.
helper(fail) -> erlang:error(probe_failure);
helper(Value) -> Value.
