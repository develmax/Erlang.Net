-module(wrapped_operands_probe).
-export([reuse/2, retain/2, nested/3]).

reuse(A, B) ->
    triple(record(first, A), [{name, record(second, B)}, literal], marker).

retain(A, B) ->
    triple(record(first, A), [{name, record(second, B)}], B).

nested(A, B, C) ->
    triple(record(first, A), {tag, record(second, B), record(third, C)}, [B]).

record(Key, Value) ->
    put(trace, [Key | get(trace)]),
    case Value of
        fail -> error(Key);
        _ -> Value
    end.

triple(A, B, C) ->
    {A, B, C}.
