-module(fullsweep_request_forms).
-export([run/0,child/1]).
run() ->
    Parent=self(),Options=[monitor,{fullsweep_after,65538}],
    lists:foreach(fun(Start)->
        Ref=Start(),
        receive {spawn_reply,Ref,ok,Pid}->
            receive {Pid,2}->ok after 5000->error(missing_option) end,
            receive {'DOWN',Ref,process,Pid,normal}->ok after 5000->error(missing_down) end
        after 5000->error(missing_reply) end
    end,[fun()->spawn_request(fun()->child(Parent) end,Options) end,
         fun()->spawn_request(node(),fun()->child(Parent) end,Options) end,
         fun()->spawn_request(fullsweep_request_forms,child,[Parent],Options) end,
         fun()->spawn_request(node(),fullsweep_request_forms,child,[Parent],Options) end]), done.
child(Parent) ->
    {fullsweep_after,2}=process_info(self(),fullsweep_after),
    Parent!{self(),process_flag(fullsweep_after,7)},ok.
