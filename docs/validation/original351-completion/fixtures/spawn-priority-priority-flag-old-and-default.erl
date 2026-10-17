-module(oracle_priority_flag_old_and_default).
-export([run/0]).
run() ->
    normal=process_flag(priority,high), {priority,high}=process_info(self(),priority),
    high=process_flag(priority,low), low=process_flag(priority,max), max=process_flag(priority,normal),
    Parent=self(), normal=process_flag(priority,high),
    {Pid,Ref}=spawn_monitor(fun()->Parent!{self(),process_info(self(),priority)} end),
    receive {Pid,{priority,normal}} -> ok after 1000 -> error(inherited_priority) end,
    receive {'DOWN',Ref,process,Pid,normal} -> ok after 1000 -> error(missing_down) end,
    high=process_flag(priority,normal), done.
