-module(tagged_case_x0_probe).
-export([bound/2, bind/1, literal/1, ignored/1]).
identity(X) -> X.
bound(Value, Expected) ->
    case identity(Value) of {tag, Expected} -> hit; _ -> miss end.
bind(Value) ->
    case identity(Value) of {tag, Field} -> {hit, Field}; _ -> miss end.
literal(Value) ->
    case identity(Value) of {tag, 42} -> hit; _ -> miss end.
ignored(Value) ->
    case identity(Value) of {tag, _} -> hit; _ -> miss end.
