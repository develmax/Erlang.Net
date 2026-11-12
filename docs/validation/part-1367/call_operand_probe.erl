-module(call_operand_probe).
-export([pair/2, trailing/3, binding/2, computed_first/2]).

pair(A, B) ->
    pair_result(record(first, A), {tag, record(second, B)}).

trailing(A, B, Keep) ->
    four_result(marker, {tag, record(first, A)}, record(second, B), Keep).

binding(A, B) ->
    Value = {A, B},
    pair_result(record(first, A), record(second, Value)).

computed_first(A, B) ->
    pair_result(record(first, A), B).

record(Key, Value) ->
    put(trace, [Key | get(trace)]),
    case Value of
        fail -> error(Key);
        _ -> Value
    end.

pair_result(A, B) ->
    {A, B}.

four_result(A, B, C, D) ->
    {A, B, C, D}.
