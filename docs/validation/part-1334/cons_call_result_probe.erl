-module(cons_call_result_probe).
-export([run/1, public/1, literal_head/1, call_head/1]).
run(List) -> rebuild(lists:reverse(List)).
rebuild([H|T]) -> [H|rebuild(T)];
rebuild([]) -> [].
public([H|T]) -> [H|public(T)];
public([]) -> [].
literal_head([_|T]) -> [item|literal_head(T)];
literal_head([]) -> [].
call_head([H|T]) -> [H|relay(H,T)];
call_head([]) -> [].
relay(H,T) -> [{seen,H}|T].
