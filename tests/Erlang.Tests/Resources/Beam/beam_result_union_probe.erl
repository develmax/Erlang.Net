-module(beam_result_union_probe).
-export([choose/2]).

choose(Input, Default) ->
    case lookup(Input) of
        missing -> {value, Default};
        Found -> Found
    end.

lookup(missing) -> missing;
lookup(Value) -> {value, Value}.
