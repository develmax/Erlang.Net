-module(beam_distribution_connected402).
-export([cases/0,run/0,run_stacks/0,initialize/1,worker/1,kernel/2,controller/2,creation_noname_reuse_probe/0]).

cases()->[creation_started,connected_abort,stale_connected,controller_exit_then_idle,wrong_node_connected_raw].
initialize(Creation)->put(distribution402_creation,Creation),ensure_kernel().
run()->
    ensure_kernel(),{Child,Ref}=spawn_monitor(?MODULE,worker,[self()]),
    receive
        {Child,Values}->receive {'DOWN',Ref,process,Child,normal}->Values; {'DOWN',Ref,process,Child,R}->error({worker_exit,R}) after 5000->error(worker_not_settled) end;
        {'DOWN',Ref,process,Child,R}->error({worker_exit_without_report,R})
    after 30000->exit(Child,kill),error(worker_timeout) end.
worker(Parent)->Parent!{self(),rows(cases())},ok.
rows([])->[];
rows([Case|Tail])->[{Case,observe(Case)}|rows(Tail)].
observe(Case)->try value(Case) of V->{returned,V} catch C:R:S->{caught,C,R,S} end.
run_stacks()->
    Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,run()},at_depth(0),at_depth(1),at_depth(2),at_depth(8)]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(N)->erlang:system_flag(backtrace_depth,N),Effective=erlang:system_flag(backtrace_depth,N),{N,Effective,run()}.

value(creation_started)->{erts_internal:get_creation(),node()};
value(connected_abort)->with_connected(fun(N,H)->{erts_internal:abort_pending_connection(N,H),H=:=erts_internal:new_connection(N)} end);
value(stale_connected)->with_connected(fun(N,{Id,Ref}=H)->{erts_internal:abort_pending_connection(N,{Id+1,Ref}),H=:=erts_internal:new_connection(N),erts_internal:abort_pending_connection(N,H)} end);
value(controller_exit_then_idle)->
    N=distribution402_remote@localhost,H=reserve_pending(N),{Pid,Ref}=start_controller(N),
    false=erts_internal:abort_pending_connection(N,H),stop_controller(Pid,Ref),
    H2=await_new_generation(N,H,1000),
    {H2=/=H,erts_internal:abort_pending_connection(N,H),H2=:=erts_internal:new_connection(N),erts_internal:abort_pending_connection(N,H2)};
value(wrong_node_connected_raw)->with_connected(fun(_,H)->
    Wrong=distribution402_wrong@localhost,
    try erts_internal:abort_pending_connection(Wrong,H) of Unexpected->{unexpected_success,Unexpected}
    catch C:R:S->{same_vm_raw_identity_protocol,raw_identity(C,R,S,Wrong,H),{caught,C,R,S}} end
end).
raw_identity(error,badarg,[{erts_internal,abort_pending_connection,[Wrong,H],Info}|_],Wrong,H)->
    case lists:keyfind(error_info,1,Info) of {error_info,#{module:=erl_erts_errors}}->true; _->false end;
raw_identity(_,_,_,_,_)->false.

with_connected(Body)->
    N=distribution402_remote@localhost,_=reserve_pending(N),{Pid,Ref}=start_controller(N),
    H=erts_internal:new_connection(N),
    try Body(N,H) after stop_controller(Pid,Ref) end.
start_controller(Node)->
    {Pid,Ref}=spawn_monitor(?MODULE,controller,[self(),Node]),
    receive {controller_ready,Pid}->{Pid,Ref}; {'DOWN',Ref,process,Pid,R}->error({controller_setup_exit,R}) after 5000->exit(Pid,kill),error(controller_setup_timeout) end.
stop_controller(Pid,Ref)->
    Pid!stop,
    receive {'DOWN',Ref,process,Pid,normal}->ok; {'DOWN',Ref,process,Pid,R}->error({controller_exit,R}) after 5000->exit(Pid,kill),error(controller_exit_timeout) end.
controller(Parent,Node)->
    _=erlang:setnode(Node,self(),{flags(),1}),Parent!{controller_ready,self()},receive stop->ok end.
ensure_kernel()->
    case whereis(net_kernel) of
        undefined->Creation=case get(distribution402_creation) of undefined->1; Value->Value end,
                   Pid=spawn(?MODULE,kernel,[self(),Creation]),receive {kernel_ready,Pid}->ok after 5000->exit(Pid,kill),error(kernel_setup_timeout) end;
        _->ok
    end.
kernel(Parent,Creation)->
    true=register(net_kernel,self()),true=erlang:setnode(distribution402_local@localhost,Creation),Parent!{kernel_ready,self()},receive stop->ok end.
reserve_pending(Node)->
    H=erts_internal:new_connection(Node),true=erts_internal:abort_pending_connection(Node,H),await_new_generation(Node,H,1000).
await_new_generation(Node,Old,N)->
    New=erts_internal:new_connection(Node),
    case New=:=Old of false->New; true when N>0->receive after 1->await_new_generation(Node,Old,N-1) end; true->error(idle_generation_not_observed) end.
flags()->
    16#04 bor 16#10 bor 16#80 bor 16#100 bor 16#200 bor 16#400 bor 16#800 bor
    16#10000 bor 16#20000 bor 16#40000 bor 16#1000000 bor 16#2000000 bor
    (16#04 bsl 32) bor (16#01 bsl 32) bor (16#08 bsl 32) bor 16#08 bor 16#20.

%% Separate one-shot native audit, never part of cases()/profiles: it stops
%% the real primitive kernel and deliberately probes Noname DistEntry reuse.
creation_noname_reuse_probe()->
    ensure_kernel(),Before=erts_internal:get_creation(),
    N='nonode@nohost',H=reserve_pending(N),{Pid,Ref}=start_controller(N),stop_controller(Pid,Ref),
    Kernel=whereis(net_kernel),KRef=monitor(process,Kernel),Kernel!stop,
    receive {'DOWN',KRef,process,Kernel,normal}->ok; {'DOWN',KRef,process,Kernel,R}->error({kernel_exit,R}) after 5000->error(kernel_exit_timeout) end,
    await_noname(1000),{Before,erts_internal:get_creation(),node(),H}.
await_noname(N)->
    case node() of 'nonode@nohost'->ok; _ when N>0->receive after 1->await_noname(N-1) end; _->error(node_shutdown_not_observed) end.
