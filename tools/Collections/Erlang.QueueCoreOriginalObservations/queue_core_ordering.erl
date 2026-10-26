-module(queue_core_ordering).
-export([run/0]).
run()->L=lists:seq(1,1024), Q=build(L,queue:new()), 1024=queue:len(Q), L=queue:to_list(Q), L=drain(Q), L=queue:to_list(Q), done.
build([],Q)->Q; build([X|Xs],Q)->build(Xs,queue:in(X,Q)).
drain(Q)->case queue:out(Q) of {empty,_}->[]; {{value,V},Rest}->[V|drain(Rest)] end.