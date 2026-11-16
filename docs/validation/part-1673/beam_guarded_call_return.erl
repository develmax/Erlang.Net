-module(beam_guarded_call_return).
-export([choose/2, value/1]).
choose(Value, Default) ->
    case ?MODULE:value(Value) of
        Selected when is_integer(Selected), Selected >= 0 -> Selected;
        _ -> Default
    end.
value(Value) -> Value.
