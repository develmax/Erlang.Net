-module(literal_list_head_probe).
-export([run/2,run_tuple/2]).
run([tag|Tail], A) -> step(Tail,A);
run(Other, _) -> Other.
step(Tail,A) -> {Tail,A}.

run_tuple([{tag,V}|Tail], A) -> step3(V,Tail,A);
run_tuple(Other, _) -> Other.
step3(V,Tail,A) -> {V,Tail,A}.
