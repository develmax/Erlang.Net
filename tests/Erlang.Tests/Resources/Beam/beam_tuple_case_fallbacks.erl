-module(beam_tuple_case_fallbacks).
-export([lookup/2, identity/1, alternate/1, field/1]).

lookup(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> Value;
        _ -> undefined
    end.

identity(Rows) ->
    case lists:last(Rows) of
        {_, _, Value} -> Value;
        Other -> Other
    end.

alternate(Rows) ->
    case forward(lists:last(Rows)) of
        missing -> fallback();
        {_, _, _} = Tuple -> Tuple
    end.

field(Rows) ->
    case lists:last(Rows) of
        {a, Left, _} -> Left;
        {b, _, Right} -> Right
    end.

forward(Value) -> Value.
fallback() -> {default}.
