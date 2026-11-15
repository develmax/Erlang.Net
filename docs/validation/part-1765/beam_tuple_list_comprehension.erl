-module(beam_tuple_list_comprehension).
-export([project/2, repeated/1, identity/1, called/2, nested/1]).

project(List, Prefix) ->
    [{Key, Prefix, Value} || {Key, Value} <- List].

repeated(List) ->
    [{same, X} || {X, X} <- List].

identity(List) ->
    [{Key, Value} || {Key, Value} <- List].

called(List, Prefix) ->
    [collect(Prefix, K, V) || {K, V} <- List].

nested(List) ->
    [V || {tag, {V, _}} <- List].

collect(Prefix, Key, Value) ->
    put(seen, [Key | get(seen)]),
    {Prefix, Key, Value}.