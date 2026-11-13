-module(beam_terminal_predicates).
-export([any/1, both/1, unknown/1]).
any(0) -> literal;
any(V) -> is_one(V) orelse mark(V).
both(V) -> is_one(V) andalso mark(V).
unknown(V) -> identity(V) orelse mark(V).
is_one(1) -> true;
is_one(_) -> false.
identity(V) -> V.
mark(V) -> put(seen,V), {seen,V}.
