-module(entry_external_tail_probe).
-export([f/3]).
identity(V) -> V.
fallback(V) -> {fallback,V}.
f(Value, Fun, Args) ->
    case identity(Value) of
        {tag, 42} -> apply(Fun, Args);
        {tag, _} -> fallback(Args);
        _Other -> fallback(Args)
    end.
