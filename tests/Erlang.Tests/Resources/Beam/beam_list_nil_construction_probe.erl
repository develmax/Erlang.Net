-module(beam_list_nil_construction_probe).
-export([scan/3]).

scan([{mark,_,_}|Rest], Acc, Other) ->
    finish(Rest, Acc, Other);
scan([_|Rest], Acc, Other) ->
    scan(Rest, Acc, Other);
scan([], Acc, _) ->
    [0,Acc,0].

finish(Rest, Acc, Other) ->
    {Rest,Acc,Other}.
