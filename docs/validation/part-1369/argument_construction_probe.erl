-module(argument_construction_probe).
-export([construct/2, offset/2, nested/2, dynamic_list/2]).
-record(options, {value}).

construct(A, B) ->
    triple(record(first, A), #options{value = B}, [{name, B}]).

offset(A, B) ->
    triple(B, record(first, A), [{name, B}]).

nested(A, B) ->
    triple(record(first, A), {{head, B}, {tail, B}}, [{one, B}, {two, B}, literal]).

dynamic_list(A, B) ->
    triple(record(first, A), [{name, record(second, B)}, literal], marker).

record(Key, Value) ->
    put(trace, [Key | get(trace)]),
    case Value of
        fail -> error(Key);
        _ -> Value
    end.

triple(A, B, C) ->
    {A, B, C}.
