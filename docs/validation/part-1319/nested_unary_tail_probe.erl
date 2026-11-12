-module(nested_unary_tail_probe).
-export([stub/3, forward/3]).
stub(M,F,A) ->
    exit({undef,[{M,F,A,[]}]}).
forward(A,B,C) ->
    identity({key,[{A,B,C,A}]}).
identity(Term) -> Term.
