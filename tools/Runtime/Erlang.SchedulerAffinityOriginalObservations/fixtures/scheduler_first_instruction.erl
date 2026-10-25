-module(scheduler_first_instruction).
-export([run/0]).
run() ->
    Count=erlang:system_info(schedulers), true=Count>=2, Parent=self(),
    lists:foreach(fun(N)->
        {Pid,Ref}=spawn_opt(fun()->Parent!{self(),erlang:system_info(scheduler_id)} end,[monitor,{scheduler,N}]),
        receive {Pid,N}->ok after 5000->error(wrong_scheduler) end,
        receive {'DOWN',Ref,process,Pid,normal}->ok after 5000->error(missing_down) end
    end,lists:usort([1,Count])), done.
