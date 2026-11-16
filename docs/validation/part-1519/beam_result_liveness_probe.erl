-module(beam_result_liveness_probe).
-export([pad/1, pad_base/2, pad_unknown/1, nested/1]).

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

pad_unknown(Rows) ->
    case lists:last(Rows) of
        [Item] -> [prefix, Item];
        Other -> Other
    end.

nested(Rows) ->
    {{_Unused, Captured}, {Same, Same}} = lists:last(Rows),
    {Captured, Same}.
