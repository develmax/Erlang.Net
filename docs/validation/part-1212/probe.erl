-module(shared_case_fallback_probe).
-export([f/2, distinguish/2]).
identity(V) -> V.
fallback(V) -> {fallback,V}.
f(Value, Expected) ->
    case identity(Value) of
        {tag, Expected} -> hit;
        {tag, _} -> fallback(Expected);
        _Other -> fallback(Expected)
    end.
distinguish(Value, Expected) ->
    case identity(Value) of
        {tag, Expected} -> hit;
        {tag, _} -> fallback(Expected);
        Other -> fallback(Other)
    end.
