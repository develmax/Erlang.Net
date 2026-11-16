-module(beam_shared_call_case).
-export([choose/2, value/1]).
choose(Value, Default) ->
    case ?MODULE:value(Value) of
        [] -> Default;
        skip -> Default;
        Other -> Other
    end.
value(Value) -> Value.
