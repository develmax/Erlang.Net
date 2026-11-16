-module(tagged_cons_probe).
-export([single/1, shared/1, repeat/1, optional/1]).

single([{tag, Value}|Tail]) ->
    {Value, Tail, length(Tail)}.

shared([{tag, a, Value}|Tail]) ->
    {a, Value, length(Tail)};
shared([{tag, b, Value}|Tail]) ->
    {b, Value, length(Tail)}.

repeat([{tag, Value, Value}|Tail]) ->
    {Value, length(Tail)}.

optional([{tag, Value}|Tail]) ->
    {Value, Tail, length(Tail)};
optional([]) ->
    empty.
