-module(variable_tail_probe).
-export([run/3]).
run([H|_], Acc, Tag) -> step(Tag, [H|Acc], Acc);
run([], Acc, Tag) -> step(Tag, Acc, Acc);
run(Other, _, _) -> Other.
step(Tag, List, Acc) -> {Tag,List,Acc}.
