-module(collection_request_protocol).
-export([run/0, waiting/1, busy/1]).

capture(F) -> try {returned,F()} catch C:R:S -> {raised,C,R,S} end.

run() ->
    Parent=self(),
    {Waiting,WM}=spawn_monitor(?MODULE,waiting,[Parent]),
    receive {ready,Waiting} -> ok after 5000 -> error(waiting_start) end,
    {Busy,BM}=spawn_monitor(?MODULE,busy,[Parent]),
    receive {ready,Busy} -> ok after 5000 -> error(busy_start) end,
    try
        SelfId={self_async,make_ref(),[one,two]},
        Self=capture(fun()->_Accepted=garbage_collect(self(),[{async,SelfId}]),
                     receive {garbage_collect,SelfId,Result}->{_Accepted,Result} after 5000->error(self_timeout) end end),
        Wait=capture(fun()->garbage_collect(Waiting,[{type,minor}]) end),
        Id={target_async,make_ref(),#{key=>value}},
        BusyResult=capture(fun()->_Accepted=garbage_collect(Busy,[{async,Id},{type,major}]),
                     receive {garbage_collect,Id,Result}->{_Accepted,Result} after 5000->error(busy_timeout) end end),
        {Self,Wait,BusyResult}
    after
        exit(Waiting,kill), exit(Busy,kill),
        receive {'DOWN',WM,process,Waiting,_}->ok after 5000->error(wait_cleanup) end,
        receive {'DOWN',BM,process,Busy,_}->ok after 5000->error(busy_cleanup) end
    end.

waiting(Parent)->put(kept,{make_ref(),[1,2,3]}),Parent!{ready,self()},receive stop->ok end.
busy(Parent)->put(kept,{make_ref(),[1,2,3]}),Parent!{ready,self()},loop().
loop()->receive stop->ok after 0->loop() end.
