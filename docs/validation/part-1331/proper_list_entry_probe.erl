-module(proper_list_entry_probe).
-export([run/2, start/1, callback/0]).
run([x,y|T], Acc) -> run([y|T],Acc);
run([H|T], Acc) -> run(T,[H|Acc]);
run([], Acc) -> lists:reverse(lists:dropwhile(fun(X) -> X =:= drop end, Acc)).
start(Value) -> inner(lists:reverse(Value),[]).
inner([x,y|T], Acc) -> inner([y|T],Acc);
inner([H|T], Acc) -> inner(T,[H|Acc]);
inner([], Acc) -> lists:reverse(lists:dropwhile(fun(X) -> X =:= drop end, Acc)).
callback() -> fun indirect/2.
indirect([x,y|T], Acc) -> indirect([y|T],Acc);
indirect([H|T], Acc) -> indirect(T,[H|Acc]);
indirect([], Acc) -> lists:reverse(lists:dropwhile(fun(X) -> X =:= drop end, Acc)).
