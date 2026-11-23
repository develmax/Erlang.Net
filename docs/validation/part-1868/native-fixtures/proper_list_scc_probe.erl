-module(proper_list_scc_probe).
-export([parse/1, open/2, escape/1, remote/0]).
parse(Tokens) -> step(Tokens, []).
step([{push, V} | Rest], Stack) -> step(Rest, [V | Stack]);
step([pop | Rest], Stack) -> step(Rest, pop(known, Stack));
step([], Stack) -> Stack.
pop(_, Stack) -> [_ | Tail] = Stack, Tail.
open(Tokens, Stack) -> open_step(Tokens, Stack).
open_step([pop | Rest], Stack) -> open_step(Rest, open_pop(known, Stack));
open_step([], Stack) -> Stack.
open_pop(_, Stack) -> [_ | Tail] = Stack, Tail.
escape(call) -> escaped(known, []);
escape(reference) -> fun escaped/2.
escaped(_, Stack) -> [_ | Tail] = Stack, Tail.
remote() -> remote_pop(known, other:tl([value])).
remote_pop(_, Stack) -> [_ | Tail] = Stack, Tail.
