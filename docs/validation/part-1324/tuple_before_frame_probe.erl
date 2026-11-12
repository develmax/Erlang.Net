-module(tuple_before_frame_probe).
-export([run/2, tagged/2, same/2, target/1]).
run(tag, {A,B}) -> X = target(A), {X,B};
run(tag, _) -> no.
tagged(7, {kind,A}) -> X = target(A), {X,A};
tagged(7, _) -> no.
same(A, {A,B}) -> X = target(A), {X,B};
same(_, _) -> no.
target(A) -> {called,A}.
