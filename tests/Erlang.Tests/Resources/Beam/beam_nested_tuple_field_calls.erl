-module(beam_nested_tuple_field_calls).
-export([run/2]).

run(Suffix, Rows) ->
    Payload = lists:last(Rows),
    {{_Unused}, {A, B, C}} = lists:last(Payload),
    {"prefix " ++ Suffix, [wrap(A), wrap(B), wrap(C) | Rows]}.

wrap(Value) ->
    {value, Value}.
