-module(beam_identifier_retention403).
-export([run/1,run_profile/2,worker/2,kernel/2,controller/2]).

run(Kind)->
    {Child,Ref}=spawn_monitor(?MODULE,worker,[self(),Kind]),
    receive
        {Child,Outcome}->receive {'DOWN',Ref,process,Child,normal}->{Kind,Outcome}; {'DOWN',Ref,process,Child,R}->error({worker_exit,R}) after 5000->error(worker_not_settled) end;
        {'DOWN',Ref,process,Child,R}->error({worker_exit_without_report,R})
    after 30000->exit(Child,kill),error(worker_timeout) end.
worker(Parent,Kind)->
    Outcome=try value(Kind) of V->{returned,V} catch C:R:S->{caught,C,R,S} end,
    Parent!{self(),Outcome},ok.
run_profile(Kind,Requested)->
    Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
    Effective=case Requested of default->Old; _->erlang:system_flag(backtrace_depth,Requested),erlang:system_flag(backtrace_depth,Requested) end,
    {Requested,Effective,run(Kind)}.

value(Kind)->
    {Before,Kernel,Root}=setup_and_drop_handle(Kind),
    %% No connection handle is returned from setup: its actual helper frame is gone.
    GcCompleted=erlang:garbage_collect(self()),
    KRef=monitor(process,Kernel),Kernel!stop,
    receive {'DOWN',KRef,process,Kernel,normal}->ok; {'DOWN',KRef,process,Kernel,R}->error({kernel_exit,R}) after 5000->error(kernel_exit_timeout) end,
    await_noname(1000),
    {retained_dist_entry,Kind,Before,erts_internal:get_creation(),node(),GcCompleted,materialize(Kind,Root)}.
setup_and_drop_handle(Kind)->
    Kernel=ensure_kernel(),Before=erts_internal:get_creation(),N='nonode@nohost',
    _H=reserve_pending(N),{Controller,Ref}=start_controller(N),
    Root=make_root(Kind),stop_controller(Controller,Ref),
    {Before,Kernel,Root}.

make_root(pid)->external_pid();
make_root(port)->external_port();
make_root(reference)->external_reference();
make_root(fun_pid)->Id=external_pid(),fun()->Id end;
make_root(nested_fun_reference)->Id=external_reference(),Box=#{retained=>[{id,Id}]},fun()->Box end.
materialize(fun_pid,Root)->Root();
materialize(nested_fun_reference,Root)->#{retained:=[{id,Id}]}=Root(),Id;
materialize(_,Root)->Root.
external_pid()->binary_to_term(<<131,88,119,13,"nonode@nohost",1:32,0:32,1:32>>).
external_port()->binary_to_term(<<131,120,119,13,"nonode@nohost",1:64,1:32>>).
external_reference()->binary_to_term(<<131,90,3:16,119,13,"nonode@nohost",1:32,1:32,2:32,3:32>>).

ensure_kernel()->
    Parent=self(),Pid=spawn(?MODULE,kernel,[Parent,37]),
    receive {kernel_ready,Pid}->Pid after 5000->exit(Pid,kill),error(kernel_setup_timeout) end.
kernel(Parent,Creation)->
    true=register(net_kernel,self()),true=erlang:setnode(distribution403_local@localhost,Creation),Parent!{kernel_ready,self()},receive stop->ok end.
start_controller(Node)->
    {Pid,Ref}=spawn_monitor(?MODULE,controller,[self(),Node]),
    receive {controller_ready,Pid}->{Pid,Ref}; {'DOWN',Ref,process,Pid,R}->error({controller_setup_exit,R}) after 5000->exit(Pid,kill),error(controller_setup_timeout) end.
controller(Parent,Node)->
    _=erlang:setnode(Node,self(),{flags(),1}),Parent!{controller_ready,self()},receive stop->ok end.
stop_controller(Pid,Ref)->
    Pid!stop,
    receive {'DOWN',Ref,process,Pid,normal}->ok; {'DOWN',Ref,process,Pid,R}->error({controller_exit,R}) after 5000->exit(Pid,kill),error(controller_exit_timeout) end.
reserve_pending(Node)->
    H=erts_internal:new_connection(Node),true=erts_internal:abort_pending_connection(Node,H),await_new_generation(Node,H,1000).
await_new_generation(Node,Old,N)->
    New=erts_internal:new_connection(Node),
    case New=:=Old of false->New; true when N>0->receive after 1->await_new_generation(Node,Old,N-1) end; true->error(idle_generation_not_observed) end.
await_noname(N)->
    case node() of 'nonode@nohost'->ok; _ when N>0->receive after 1->await_noname(N-1) end; _->error(node_shutdown_not_observed) end.
flags()->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor 16#800 bor
    16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor 16#2000000 bor
    (16#04 bsl 32) bor (16#01 bsl 32) bor (16#08 bsl 32) bor 16#08 bor 16#20.
