-module(entry_selector_heap_probe).
-export([f/2, g/3]).
take(Value, Other) -> {Value,Other}.
f(A, B) ->
    case take({tag, B}, A) of
        {{tag, Value}, Other} -> {Value, Other, A};
        _ -> mismatch
    end.
g(A, B, C) ->
    Value = case take([B, fixed], A) of
        {List, Other} -> {List, Other}
    end,
    {Value, C}.
