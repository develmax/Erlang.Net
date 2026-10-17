-module(common_private_wrapper_clause).
-export([run/0,leaf/1]).

run() ->
    Old = erlang:system_flag(backtrace_depth,8),
    try
        [{wrapped,done} = {wrapped,common_private_wrapper_clause:leaf(Depth)} || Depth <- [0,1,2,8]],
        done
    after erlang:system_flag(backtrace_depth,Old)
    end.

leaf(Depth) ->
    erlang:system_flag(backtrace_depth,Depth),
    try proc_lib:initial_call(invalid_input)
    catch error:function_clause:Stack ->
        [{proc_lib,raw_initial_call,[invalid_input],_}|_] = Stack,
        Expected = lists:sublist([{proc_lib,raw_initial_call,[invalid_input]},
                                 {proc_lib,initial_call,1},
                                 {common_private_wrapper_clause,leaf,1}],erlang:max(1,Depth)),
        Actual = lists:sublist([{M,F,A} || {M,F,A,_} <- Stack],length(Expected)),
        Expected = Actual,
        true = length(Stack) =< erlang:max(1,Depth),
        done
    end.
