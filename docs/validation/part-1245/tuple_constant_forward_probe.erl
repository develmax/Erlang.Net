-module(tuple_constant_forward_probe).
-export([run/1, first/1, second/1, public/2]).
run(X) -> private(X, 1000).
private({tag, _}, Value) -> keep(Value);
private(_, _) -> missing.
first(X) -> mixed(X, 1).
second(X) -> mixed(X, 2).
mixed({tag, _}, Value) -> keep(Value);
mixed(_, _) -> missing.
public({tag, _}, Value) -> keep(Value);
public(_, _) -> missing.
keep(Value) -> {value, Value}.
