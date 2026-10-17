-module(oracle_priority_duplicate_invalid).
-export([run/0]).
run() ->
    Parent=self(),
    {Pid,Ref}=spawn_opt(fun()->Parent!{self(),process_info(self(),priority)} end,[monitor,{priority,high},{priority,low}]),
    receive {Pid,{priority,low}} -> ok after 1000 -> error(wrong_duplicate) end,
    receive {'DOWN',Ref,process,Pid,normal} -> ok after 1000 -> error(missing_down) end,
    lists:foreach(fun(Value)->
        try spawn_opt(fun()->Parent!unexpected_child end,[{priority,Value}]) of _ -> error(expected_badarg) catch error:badarg -> ok end
    end,[wrong,0,true,{},[]]),
    receive unexpected_child -> error(created_invalid_child) after 0 -> done end.
