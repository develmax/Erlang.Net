-module(map_argument_probe).
-export([start/7, direct/2, shared/3, duplicate/1]).

start(Sup, Socket, Listen, Connect, Type, Callback, Pid) ->
    Args = [Socket, Listen, Connect, Type, Callback, Pid],
    deliver(Sup, #{id => {Listen,Connect}, start => {worker,start_link,Args}}).

direct(A, B) ->
    #{value => {B,A}}.

shared(A, B, C) ->
    Pair = {A,B},
    Map = #{z => {Pair,C}, a => Pair},
    deliver(Pair, Map).

duplicate(A) ->
    #{value => {left,A}, value => {right,A}}.

deliver(Sup, Spec) ->
    {Sup,Spec}.
