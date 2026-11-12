-module(general_list_operands_probe).
-export([f/3]).
identity(V) -> V.
f(V, Tail, Which) ->
    case identity(Which) of
        bound -> [tag, V, 42 | Tail];
        literal -> [tag, V | {improper, tail}];
        computed -> [tag, identity(V) | identity(Tail)]
    end.
