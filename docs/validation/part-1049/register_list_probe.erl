-module(register_list_probe).
-export([run/3, repeat/2, choose/2]).
run(T, Svc, F) -> lists:reverse([F, T, Svc]).
repeat(A, B) -> lists:reverse([A, B, A, fixed]).
choose(tag, A) -> lists:reverse([A, first]);
choose(_, A) -> lists:reverse([A, second]).
