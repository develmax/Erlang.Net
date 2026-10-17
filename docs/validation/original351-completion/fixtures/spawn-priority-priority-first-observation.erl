-module(oracle_priority_first_observation).
-export([run/0, child/1]).
run() ->
    Parent=self(),
    lists:foreach(fun(Level)->
        {Pid,Ref}=spawn_opt(oracle_priority_first_observation,child,[Parent],[monitor,{priority,Level}]),
        receive {Pid,{priority,Level}} -> ok after 1000 -> error(missing_priority) end,
        receive {'DOWN',Ref,process,Pid,normal} -> ok after 1000 -> error(missing_down) end
    end,[low,normal,high,max]), done.
child(Parent) -> Parent ! {self(),process_info(self(),priority)}.
