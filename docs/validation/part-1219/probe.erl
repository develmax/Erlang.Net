-module(boolean_tail_branch_probe).
-export([f/3]).
identity(V) -> V.
fallback(V) -> {fallback,V}.
f(Value, Fun, Selector) ->
    case identity(Value) of
        {tag, 42} ->
            case identity(Selector) of
                true -> apply(Fun, []);
                false -> fallback(Selector)
            end;
        {tag, _} -> fallback(Selector);
        _Other -> fallback(Selector)
    end.
