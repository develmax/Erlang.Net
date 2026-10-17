-module(fullsweep_spawn_forms).
-export([run/0,child/2]).
run() ->
    Parent=self(),Options=[monitor,{fullsweep_after,2},{fullsweep_after,65537}],
    lists:foreach(fun(Start)->
        {Pid,Ref}=Start(),
        receive {Pid,1}->ok after 5000->error(missing_option) end,
        receive {'DOWN',Ref,process,Pid,normal}->ok after 5000->error(missing_down) end
    end,[fun()->spawn_opt(fun()->child(Parent,1) end,Options) end,
         fun()->spawn_opt(node(),fun()->child(Parent,1) end,Options) end,
         fun()->spawn_opt(fullsweep_spawn_forms,child,[Parent,1],Options) end,
         fun()->spawn_opt(node(),fullsweep_spawn_forms,child,[Parent,1],Options) end]), done.
child(Parent,Expected) ->
    {fullsweep_after,Expected}=process_info(self(),fullsweep_after),
    Parent!{self(),process_flag(fullsweep_after,7)},ok.
