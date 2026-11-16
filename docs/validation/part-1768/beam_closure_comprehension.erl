-module(beam_closure_comprehension).
-export([fun_first/2, fun_last/2, seq/2, branch_fun/3, remote_fun/1, nested_fun/2]).
fun_first(L, Key) -> {fun() -> Key end, [tag(Key, X) || X <- L]}.
fun_last(L, Key) -> {[tag(Key, X) || X <- L], fun() -> Key end}.
seq(L, Key) -> F = fun() -> Key end, M = [tag(Key, X) || X <- L], {F,M}.
branch_fun(L, Key, yes) -> fun() -> Key end;
branch_fun(L, Key, _) -> [tag(Key, X) || X <- L].
remote_fun(L) -> {fun lists:reverse/1, [tag(key, X) || X <- L]}.
nested_fun(L, Key) -> {fun() -> [tag(Key, X) || X <- L] end, Key}.
tag(Key, X) -> {Key,X}.
