-module(scheduler_invalid_and_duplicate).
-export([run/0]).
run() ->
    Count=erlang:system_info(schedulers), true=Count>=2, Parent=self(),
    lists:foreach(fun(Value)->
        try spawn_opt(fun()->Parent!unexpected end,[{scheduler,Value},{scheduler,1}]) of _->error(accepted)
        catch error:badarg->ok end
    end,[-1,Count+1,1.0,wrong,[],{},1 bsl 100]),
    {Pid,Ref}=spawn_opt(fun()->Parent!{self(),erlang:system_info(scheduler_id)} end,[monitor,{scheduler,Count},{scheduler,1}]),
    receive {Pid,1}->ok after 5000->error(wrong_duplicate) end,
    receive {'DOWN',Ref,process,Pid,normal}->ok after 5000->error(missing_down) end,
    receive unexpected->error(created_invalid_child) after 0->done end.
