-module(beam_import_probe).
-export([run/1, combine/2, ignore/1]).
-import(lists, [reverse/1]).
-import(lists, [append/2]).
run(X) ->
    reverse(X).
combine(A,B) -> append(A,B).
ignore(X) ->
    reverse(X),
    X.
