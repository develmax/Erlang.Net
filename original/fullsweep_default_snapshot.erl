-module(fullsweep_default_snapshot).
-export([run/0]).
run() ->
    Old=erlang:system_flag(fullsweep_after,3),
    try
        {fullsweep_after,3}=erlang:system_info(fullsweep_after),
        SelfOld=process_flag(fullsweep_after,19),
        try
            Parent=self(),
            {Pid,Ref}=spawn_monitor(fun()->Parent!{self(),process_flag(fullsweep_after,23)} end),
            receive {Pid,3}->ok after 5000->error(inherited_parent) end,
            receive {'DOWN',Ref,process,Pid,normal}->ok after 5000->error(missing_down) end,
            3=erlang:system_flag(fullsweep_after,65536),
            {fullsweep_after,65535}=erlang:system_info(fullsweep_after),
            {fullsweep_after,19}=process_info(self(),fullsweep_after), done
        after process_flag(fullsweep_after,SelfOld) end
    after erlang:system_flag(fullsweep_after,Old) end.
