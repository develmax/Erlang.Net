-module(dynamic_apply370_host).
-export([run/0,loop/1]).
run() -> {capture(abs,[bad]),capture(error,[marker]),capture(exit,[marker]),capture(throw,[marker]),capture(abs,[1|tail]),non_tail(),loop(32768)}.
capture(Function,Arguments) ->
    try tail_apply(Function,Arguments) of Value -> Value
    catch Class:Reason:Stack -> {Class,Reason,Stack} end.
tail_apply(Function,Arguments) -> erlang:apply(erlang,Function,Arguments).
non_tail() ->
    try non_tail_apply(abs,[bad]) of Value -> Value
    catch Class:Reason:Stack -> {Class,Reason,Stack} end.
non_tail_apply(Function,Arguments) -> Value=erlang:apply(erlang,Function,Arguments),{after_call,Value}.
loop(0) -> ok;
loop(N) -> Function=loop,erlang:apply(dynamic_apply370_host,Function,[N-1]).
