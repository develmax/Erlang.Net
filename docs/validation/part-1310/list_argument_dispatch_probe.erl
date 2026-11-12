-module(list_argument_dispatch_probe).
-export([run/2,run_tuple/1]).
run([Head|_], Tag) -> step([Head,Tag]);
run([], Tag) -> finish(Tag);
run(Other, _) -> Other.
step(List) -> {items,List}.
finish(Tag) -> {empty,Tag}.

run_tuple({tag,A,B}) -> step([A,B]);
run_tuple(Other) -> Other.
