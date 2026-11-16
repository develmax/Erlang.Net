-module(literal_list_suffix_probe).
-export([f/2]).
identity(V) -> V.
f(Value, Tag) ->
    case identity(Tag) of
        proper -> [identity(Value), fixed, 42];
        improper -> [identity(Value), fixed | tail];
        dynamic -> [Value, fixed | identity(Tag)]
    end.
