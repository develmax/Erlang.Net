-module(backtrace_depth_flag).
-export([run/0]).
run()->
    Old=erlang:system_flag(backtrace_depth,8),
    try
        8=erlang:system_flag(backtrace_depth,0),
        0=erlang:system_flag(backtrace_depth,1),
        1=erlang:system_flag(backtrace_depth,64),
        64=erlang:system_flag(backtrace_depth,65),
        64=erlang:system_flag(backtrace_depth,65536),
        64=erlang:system_flag(backtrace_depth,(1 bsl 59)-1),
        64=erlang:system_flag(backtrace_depth,8),
        lists:foreach(fun invalid/1,[-1,1 bsl 59,1 bsl 200,1.0,bad,[],#{},{}]),
        Parent=self(),{Child,Monitor}=spawn_monitor(fun()->8=erlang:system_flag(backtrace_depth,2),Parent!changed end),
        receive changed->ok after 2000->error(child_timeout) end,
        receive {'DOWN',Monitor,process,Child,normal}->ok after 2000->error(child_not_joined) end,
        2=erlang:system_flag(backtrace_depth,8),true=is_pid(Child),done
    after erlang:system_flag(backtrace_depth,Old) end.
invalid(Value)->
    try erlang:system_flag(backtrace_depth,Value) of _->error(missing_badarg)
    catch error:badarg:Stack->
        [{erlang,system_flag,[backtrace_depth,Value],[{error_info,ErrorInfo}]}|_]=Stack,
        true=(ErrorInfo =:= #{module=>erl_erts_errors})
    end,
    8=erlang:system_flag(backtrace_depth,8).
