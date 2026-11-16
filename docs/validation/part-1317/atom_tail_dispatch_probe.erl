-module(atom_tail_dispatch_probe).
-export([run/3,run_second/3]).
run(alpha, First, Second) -> first(First,Second);
run(zulu, First, Second) -> second(Second,First).
first(A,B) -> {first,A,B}.
second(A,B) -> {second,A,B}.

run_second(First, alpha, Second) -> first(First,Second);
run_second(First, zulu, Second) -> second(Second,First).
