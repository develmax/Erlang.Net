-module(node_identity501).
-export([run/0,run_stacks/0,probe/1]).
run()->ordsets_core_support:run(?MODULE,[identity_cycle]).
run_stacks()->ordsets_core_support:profiles(?MODULE,[identity_cycle]).
probe(identity_cycle)->
 P=self(),R=make_ref(),M=atomics:new(1,[]),Port=list_to_port("#Port<0.123>"),
 {Dead,Down}=spawn_monitor(fun()->ok end),receive {'DOWN',Down,process,Dead,normal}->ok end,
 N=term_to_binary('kept@host'),<<131,Name/binary>>=N,
 External=binary_to_term(<<131,88,Name/binary,123:32,0:32,42:32>>),
 Values=[P,R,M,Port,Dead,External],
 Before=query(Values),First=cycle('identity501a@localhost',Values),
 Between=query(Values),Second=cycle('identity501b@localhost',Values),
 After=query(Values),{Before,First,Between,Second,After}.
cycle(Name,Values)->
 Parent=self(),{Kernel,Ref}=spawn_monitor(fun()->
  true=register(net_kernel,self()),true=erlang:setnode(Name,0),Parent!{started,self()},
  receive stop->ok end
 end),
 try
  receive {started,Kernel}->ok; {'DOWN',Ref,process,Kernel,Reason}->error({kernel_failed,Reason}) after 5000->error(kernel_timeout) end,
  Snapshot=query(Values),Kernel!stop,
  receive {'DOWN',Ref,process,Kernel,normal}->ok after 5000->error(kernel_stop_timeout) end,
  wait_nonode(5000),Snapshot
 after exit(Kernel,kill) end.
query(Values)->[erlang:apply(erlang,opaque(node),[V]) || V<-Values].
opaque(V)->put(node_identity501_input,V),erase(node_identity501_input).
wait_nonode(0)->error(node_reset_timeout);
wait_nonode(N)->case node() of 'nonode@nohost'->ok;_->receive after 1->wait_nonode(N-1) end end.
