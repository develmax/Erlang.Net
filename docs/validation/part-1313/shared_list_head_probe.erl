-module(shared_list_head_probe).
-export([run/2,run_tuple/2]).
run([tag|Tail], A) -> tagged(Tail,A);
run([Head|Tail], A) -> other(Head,Tail,A);
run(Input, _) -> Input.
tagged(Tail,A) -> {tag,Tail,A}.
other(Head,Tail,A) -> {other,Head,Tail,A}.

run_tuple([{tag,V}|Tail], A) -> tuple_head(Tail,V,A);
run_tuple([Head|Tail], A) -> other(Head,Tail,A);
run_tuple(Input, _) -> Input.
tuple_head(Tail,V,A) -> {tuple,Tail,V,A}.
