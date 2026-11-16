-module(beam_singleton_conversion_probe).
-export([pad/1, pad_base/2]).

pad(N) ->
    case integer_to_list(N) of
        [Digit] -> [$0, Digit];
        Text -> Text
    end.

pad_base(N, Base) ->
    case erlang:integer_to_list(N, Base) of
        [Digit] -> [$0, Digit];
        Text -> Text
    end.
