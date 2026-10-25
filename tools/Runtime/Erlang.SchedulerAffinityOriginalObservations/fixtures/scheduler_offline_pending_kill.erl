-module(scheduler_offline_pending_kill).
-export([run/0]).
run() ->
    Total=erlang:system_info(schedulers),
    case Total > 1 of
        false -> error(two_scheduler_profile_required);
        true ->
            PreviousBinding=process_flag(scheduler,1), Parent=self(),
            {Pid,Ref}=spawn_opt(fun()->Parent!{self(),ready},receive hold->ok end end,[monitor,{scheduler,2}]),
            receive {Pid,ready}->ok after 5000->error(no_ready) end,
            PreviousOnline=erlang:system_flag(schedulers_online,1),
            try
                true=exit(Pid,kill),
                receive {'DOWN',Ref,process,Pid,_}->error(died_while_offline) after 0->ok end,
                1=erlang:system_flag(schedulers_online,2),
                receive {'DOWN',Ref,process,Pid,killed}->done after 5000->error(no_killed_down) end
            after
                erlang:system_flag(schedulers_online,PreviousOnline),
                process_flag(scheduler,PreviousBinding)
            end
    end.
