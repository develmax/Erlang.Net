-module(private_cons_probe).
-export([run/1, run_unknown/1, seed/0, capture/0, public/2]).

run(Head) -> known([Head|tail], fallback).
run_unknown(Value) -> unknown(Value, fallback).
seed() -> escaped([seed], fallback).
capture() -> fun escaped/2.

known([Head|Tail], _) when is_atom(Head) -> pair(Head, Tail);
known(Value, Other) -> pair(Other, Value).

escaped([Head|Tail], _) when is_atom(Head) -> pair(Head, Tail);
escaped(Value, Other) -> pair(Other, Value).

unknown([Head|Tail], _) when is_atom(Head) -> pair(Head, Tail);
unknown(Value, Other) -> pair(Other, Value).

public([Head|Tail], _) when is_atom(Head) -> pair(Head, Tail);
public(Value, Other) -> pair(Other, Value).

pair(First, Second) -> {First, Second}.
