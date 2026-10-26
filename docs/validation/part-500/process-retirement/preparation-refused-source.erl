-module(process_retirement500).
-export([run/0,run_stacks/0,probe/1]).
run()->ordsets_core_support:run(?MODULE,[physical_dirty_exit]).
run_stacks()->ordsets_core_support:profiles(?MODULE,[physical_dirty_exit]).
probe(physical_dirty_exit)->
 Parent=self(),
 {Pid,Ref}=spawn_monitor(fun()->
  true=register(process_retirement500_owner,self()),
  Parent!{ready,self()},
  _=crypto:pbkdf2_hmac(sha256,<<"owned-test">>,<<"salt">>,1000000,32),
  receive stop->ok end
 end),
 try
  receive {ready,Pid}->ok after 10000->erlang:error(no_ready) end,
  true=await_dirty(Pid,10000),
  true=erlang:exit(Pid,kill),
  Alive=erlang:apply(erlang,is_process_alive,[Pid]),
  Name=whereis(process_retirement500_owner),
  Dirty=erts_internal:is_process_executing_dirty(Pid),
  receive {'DOWN',Ref,process,Pid,killed}->ok after 10000->erlang:error(no_down) end,
  {true,Alive,Name,Dirty}
 after erlang:exit(Pid,kill),erlang:demonitor(Ref,[flush]) end.
await_dirty(_,0)->erlang:error(no_physical_dirty_admission);
await_dirty(Pid,N)->
 case erts_internal:is_process_executing_dirty(Pid) of
  true->true;
  false->receive after 1->await_dirty(Pid,N-1) end
 end.
