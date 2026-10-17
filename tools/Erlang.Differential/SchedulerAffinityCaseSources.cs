namespace Erlang.Differential;

public static class SchedulerAffinityCaseSources
{
    public const string FirstInstruction = """
        -module(scheduler_first_instruction).
        -export([run/0]).
        run() ->
            Count=erlang:system_info(schedulers), true=Count>=2, Parent=self(),
            lists:foreach(fun(N)->
                {Pid,Ref}=spawn_opt(fun()->Parent!{self(),erlang:system_info(scheduler_id)} end,[monitor,{scheduler,N}]),
                receive {Pid,N}->ok after 5000->error(wrong_scheduler) end,
                receive {'DOWN',Ref,process,Pid,normal}->ok after 5000->error(missing_down) end
            end,lists:usort([1,Count])), done.

        """;

    public const string MigrationPreviousBinding = """
        -module(scheduler_migration_previous_binding).
        -export([run/0]).
        run() ->
            Count=erlang:system_info(schedulers), true=Count>=2,
            0=process_flag(scheduler,1), 1=erlang:system_info(scheduler_id),
            1=process_flag(scheduler,Count), Count=erlang:system_info(scheduler_id),
            Count=process_flag(scheduler,0), 0=process_flag(scheduler,0), done.

        """;

    public const string InvalidAndDuplicate = """
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

        """;

    public const string OfflineBoundResume = """
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

        """;

    public const string OfflinePendingKill = """
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

        """;
}
