-module(generic_map_input_probe).
-export([pick/1, nested/1, dynamic/3, optional/1]).
pick(#{alpha:=A,beta:=B}) -> {A,B};
pick(_) -> missing.
nested(#{outer := #{alpha := A}, extra := E}) -> {A,E};
nested(_) -> missing.
dynamic(Map,Key,Expected) ->
    case Map of
        #{Key:=Expected, extra:=V} -> V;
        _ -> missing
    end.
optional(Map) -> maps:get(alpha,Map).
