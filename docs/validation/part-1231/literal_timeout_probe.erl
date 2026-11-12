-module(literal_timeout_probe).
-export([run/0, dynamic/1, escape/0]).
run() -> forward(1), mixed(1), mixed(2), escaped(1), constant(unused).
forward(T) -> private_sleep(T).
private_sleep(T) -> receive after T -> ok end.
mixed(T) -> receive after T -> ok end.
dynamic(T) -> receive after T -> ok end.
constant(_) -> receive after 1 -> ok end.
escape() -> fun escaped/1.
escaped(T) -> receive after T -> ok end.
