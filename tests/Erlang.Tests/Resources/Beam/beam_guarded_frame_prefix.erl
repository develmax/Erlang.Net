-module(beam_guarded_frame_prefix).
-export([run/2]).

run(Value, Mode) when is_integer(Value) ->
    case Mode of
        refresh ->
            Next = Value + 1,
            erlang:garbage_collect(),
            {ok, Next};
        _ ->
            Value
    end.
