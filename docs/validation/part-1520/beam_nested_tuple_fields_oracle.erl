-module(beam_nested_tuple_fields_oracle).
-export([two/2, three/2, four/2]).

two(Suffix, Rows) ->
    Payload = lists:last(Rows),
    {{_Unused}, {A, B}} = lists:last(Payload),
    {"prefix " ++ Suffix, [wrap(A), wrap(B) | Rows]}.

three(Suffix, Rows) ->
    Payload = lists:last(Rows),
    {{_Unused}, {A, B, C}} = lists:last(Payload),
    {"prefix " ++ Suffix, [wrap(A), wrap(B), wrap(C) | Rows]}.

four(Suffix, Rows) ->
    Payload = lists:last(Rows),
    {{_Unused}, {A, B, C, D}} = lists:last(Payload),
    {"prefix " ++ Suffix, [wrap(A), wrap(B), wrap(C), wrap(D) | Rows]}.

wrap(fail) -> erlang:error(field_failed);
wrap(Value) -> {value, Value}.
