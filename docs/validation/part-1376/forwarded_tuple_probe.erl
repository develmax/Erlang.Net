-module(forwarded_tuple_probe).
-export([wrap/2, keep/3, external/2, retain/2, nested/2]).

wrap(A, B) ->
    R = stage(A, B, []),
    {imports, finish(R)}.

keep(A, B, Saved) ->
    R = stage(A, B, []),
    {finish(R), Saved}.

external(A, B) ->
    R = stage(A, B, []),
    {rev, lists:reverse(R, [])}.

retain(A, B) ->
    R = stage(A, B, []),
    {R, finish(R)}.

nested(A, B) ->
    R = stage(A, B, []),
    {tag, finish_pair({input, R})}.

stage(A, B, Tail) ->
    [A, B | Tail].

finish(List) ->
    lists:reverse(List).

finish_pair({input, List}) ->
    lists:reverse(List).
