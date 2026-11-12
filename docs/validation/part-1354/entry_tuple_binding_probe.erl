-module(entry_tuple_binding_probe).
-export([branch/2, tagged/2]).

branch([{item, Value} | Tail], Context) ->
    Result = box(Value),
    {Tail, Result, Context, Result};
branch(Other, Context) ->
    {other, Other, Context}.

tagged({item, Value}, Context) ->
    Result = box(Value),
    {ok, Result, Context};
tagged(Other, Context) ->
    {other, Other, Context}.

box(Value) ->
    {boxed, Value}.
