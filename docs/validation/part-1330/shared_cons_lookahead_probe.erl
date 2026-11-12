-module(shared_cons_lookahead_probe).
-export([run/2,captured/2]).
run([x,y|T], Acc) -> run([y|T],Acc);
run([H|T], Acc) -> run(T,[H|Acc]);
run([], Acc) -> lists:reverse(lists:dropwhile(fun(X) -> X =:= drop end, Acc)).
captured([], Acc) -> lists:reverse(lists:dropwhile(fun(X) -> X =:= hd(Acc) end, Acc)).
