-module(scheduler_offline_bound_resume).
-export([run/0]).
run() ->
    Total=erlang:system_info(schedulers),
    case Total > 1 of
        false -> error(two_scheduler_profile_required);
        true ->
            PreviousBinding=process_flag(scheduler,1),
            PreviousOnline=erlang:system_flag(schedulers_online,1),
            try
                Parent=self(),
                {Pid,Ref}=spawn_opt(fun()->Parent!{self(),erlang:system_info(scheduler_id)} end,[monitor,{scheduler,Total}]),
                receive {Pid,_}->error(ran_offline) after 0->ok end,
                1=erlang:system_flag(schedulers_online,Total),
                receive {Pid,Total}->ok after 5000->error(did_not_resume) end,
                receive {'DOWN',Ref,process,Pid,normal}->done after 5000->error(missing_down) end
            after
                erlang:system_flag(schedulers_online,PreviousOnline),
                process_flag(scheduler,PreviousBinding)
            end
    end.
