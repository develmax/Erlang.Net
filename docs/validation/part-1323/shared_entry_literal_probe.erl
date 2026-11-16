-module(shared_entry_literal_probe).
-export([run/2, run_integer/2, target/1]).
run(tag, {A,B}) -> X = target(A), {X,B};
run(tag, _) -> no.
run_integer(7, {A,B}) -> X = target(A), {X,B};
run_integer(7, _) -> no.
target(A) -> {called,A}.
