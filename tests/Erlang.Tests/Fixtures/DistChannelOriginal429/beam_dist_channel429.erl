-module(beam_dist_channel429).
-export([run/0,run_stacks/0,worker/2,kernel/1,controller/0,capture/1]).
cases()->[same,cross,same_twice,missing_pending,bad_node,bad_controller,bad_flags,bad_flags_negative,bad_flags_huge,bad_creation_negative,bad_creation_huge,dead_controller,wrong_arity_one,wrong_arity_two].
run()->ensure_kernel(),[{N,observe(N)}||N<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),E=erlang:system_flag(backtrace_depth,D),{D,E,run()}.
observe(N)->{P,R}=spawn_monitor(?MODULE,worker,[self(),N]),receive {N,V}->receive {'DOWN',R,process,P,normal}->V end;{'DOWN',R,process,P,Why}->exit({worker_failed,N,Why}) after 15000->exit(P,kill),exit(worker_timeout) end.
worker(Parent,N)->V=try {value,perform(N)} catch C:R:S->{exception,C,R,S} end,Parent!{N,V}.
ensure_kernel()->case whereis(net_kernel) of undefined->Parent=self(),K=spawn(?MODULE,kernel,[Parent]),receive {kernel_ready,K}->ok after 5000->error(kernel_timeout) end;_->ok end.
kernel(Parent)->true=register(net_kernel,self()),true=erlang:setnode(channel429_local@localhost,1),Parent!{kernel_ready,self()},receive stop->ok end.
handle({Id,Ref})->is_integer(Id) andalso Id >= 0 andalso is_reference(Ref);handle(_)->false.
flags()->element(3,erts_internal:get_dflags()).
node_for(N)->list_to_atom("channel429_"++atom_to_list(N)++"@localhost").
pending(N)->Node=node_for(N),_=erts_internal:new_connection(Node),Node.
create(Node,Pid,Flags,Creation)->erts_internal:create_dist_channel(Node,Pid,{Flags,Creation}).
probe(H)->Size=erlang:dist_ctrl_get_opt(H,get_size),Input=erlang:dist_ctrl_input_handler(H,self()),Put=erlang:dist_ctrl_put_data(H,[]),Data=erlang:dist_ctrl_get_data(H),{Size,Input,Put,Data}.
perform(same)->Node=pending(same),{ok,H}=create(Node,self(),flags(),1),{handle(H),probe(H)};
perform(cross)->Node=pending(cross),{P,R}=spawn_monitor(?MODULE,controller,[]),
 try {message,Ref}=create(Node,P,flags(),1),receive {Ref,H}->P!{probe,self(),H},receive {probed,P,V}->{is_reference(Ref),handle(H),V} after 5000->error(probe_timeout) end after 5000->error(reply_timeout) end
 after P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->exit(P,kill) end end;
perform(same_twice)->Node=pending(same_twice),{ok,H}=create(Node,self(),flags(),1),Other=pending(same_twice_other),{handle(H),create(Other,self(),flags(),1)};
perform(missing_pending)->create(node_for(missing_pending),self(),flags(),1);
perform(bad_node)->create(bad_node429,self(),flags(),1);
perform(bad_controller)->create(node_for(bad_controller),bad_controller429,flags(),1);
perform(bad_flags)->create(node_for(bad_flags),self(),0,1);
perform(bad_flags_negative)->create(node_for(bad_flags_negative),self(),-1,1);
perform(bad_flags_huge)->create(node_for(bad_flags_huge),self(),1 bsl 64,1);
perform(bad_creation_negative)->create(node_for(bad_creation_negative),self(),flags(),-1);
perform(bad_creation_huge)->create(node_for(bad_creation_huge),self(),flags(),1 bsl 32);
perform(dead_controller)->Node=pending(dead_controller),{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->create(Node,P,flags(),1) end;
perform(wrong_arity_one)->apply(erts_internal,create_dist_channel,[bad429]);
perform(wrong_arity_two)->apply(erts_internal,create_dist_channel,[bad429,other429]).
controller()->receive {probe,Parent,H}->Parent!{probed,self(),probe(H)},controller();stop->ok end.
capture([D])->ok=prim_file:write_file(D++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(D++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
