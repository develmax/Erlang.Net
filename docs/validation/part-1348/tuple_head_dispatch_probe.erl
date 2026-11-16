-module(tuple_head_dispatch_probe).
-export([collect/1, collect/2, strip/1]).

collect([{node,_,_}=Head|Tail]) ->
    collect(Tail,[Head]);
collect([{skip,_}|Tail]) ->
    collect(Tail,[]);
collect(Rest) ->
    {[],Rest}.

collect([{node,_,_}=Head|Tail], Acc) ->
    collect(Tail,Acc++[Head]);
collect([{skip,_}|Tail], Acc) ->
    collect(Tail,Acc);
collect(Rest, Acc) ->
    {Acc,Rest}.

strip([{_,_,_}|Tail]) ->
    strip(Tail);
strip([{_,_}|Tail]) ->
    Tail;
strip(Rest) ->
    {rest,Rest}.
