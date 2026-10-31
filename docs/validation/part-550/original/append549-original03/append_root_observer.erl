-module(append_root_observer).
-export([run/0,concat/2,app/2]).
run() ->
    L = [1,2],
    {concat(L,tail),concat(L,[]),concat([],tail),concat([a|bad],[b]),
     concat(not_a_list,[]),app(L,tail),app(L,[]),app([],tail),
     app([a|bad],[b]),app(not_a_list,[])}.
concat(L,R) ->
    try erlang:'++'(L,R) of V -> {returned,V}
    catch C:E:S -> {caught,C,E,S} end.
app(L,R) ->
    try erlang:append(L,R) of V -> {returned,V}
    catch C:E:S -> {caught,C,E,S} end.
