-module(list_tail_dispatch_probe).
-export([run/2,proper/1]).
run([Head|Tail], Tag) -> step(Head,Tail,Tag);
run([], Tag) -> finish(Tag);
run(Other, _) -> Other.
step(Head,Tail,Tag) -> {Tag,Head,Tail}.
finish(Tag) -> {empty,Tag}.

proper([_|Tail]) -> proper(Tail);
proper([]) -> true;
proper(_) -> false.
