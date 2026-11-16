-module(computed_cons_head_probe).
-export([carry/2, unary/1, packed/2, visit/2]).
carry([Head|Tail], Context) ->
    [make(Head, Context)|carry(Tail, Context)];
carry([], _) -> [].
unary([Head|Tail]) ->
    [make(Head)|unary(Tail)];
unary([]) -> [].
packed([Head|Tail], Context) ->
    [{Tail,Context,make(Head, Context)}|packed(Tail, Context)];
packed([], _) -> [].
visit([Head|Tail], Key) ->
    [record(Head, Key)|visit(Tail, Key)];
visit([], _) -> [].
make(Head, Context) -> {Head,Context}.
make(Head) -> {value,Head}.
record(stop, _) ->
    erlang:error(head_failure);
record(Head, Key) ->
    put(Key, [Head|get(Key)]),
    Head.
