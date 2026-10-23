-module(beam_ddll_distribution448).
-export([new/0,request/3,actor/0,setup/1,new_ref/0,demon/1,decode/1,encode/1,start_node/1,stop_node/0,identity/0,cleanup/0,kernel/0,depth/1,wait_unloaded/0,store_term/2,stored/1,disconnect_peer/1,remote/3,seen_node/1,watch_node/1,await_down/1,retain_wire/1,save_ref/1,wire/1,save_wire/1,demon_saved/1,demon_wire/1,demon_binary/1,waiting/0]).
new() -> spawn(?MODULE,actor,[]).
request(Worker,F,A) ->
    Ref=make_ref(),Worker!{call,self(),Ref,F,A},
    receive {reply,Ref,V}->V after 10000->error(worker_timeout) end.
actor() ->
    receive
        {call,From,Ref,F,A}->
            Result=try apply(?MODULE,F,A) of V->{ok,V} catch C:R:S->{failed,C,R,S} end,
            From!{reply,Ref,Result},actor();
        stop -> ok
    end.
setup(Path) ->
    process_flag(trap_exit,true),
    {ok,loaded}=erl_ddll:try_load(Path,echo_drv,[]),
    Port=open_port({spawn_driver,"echo_drv"},[binary]),put(port447,Port),ok.
new_ref() -> erl_ddll:monitor(driver,{echo_drv,unloaded}).
save_ref(Key) -> Ref=new_ref(),put({ref447,Key},Ref),true.
wire(Key) -> encode(get({ref447,Key})).
retain_wire(Key) -> put({external447,Key},decode(get({wire447,Key}))),true.
save_wire(Key) -> put({wire447,Key},wire(Key)),true.
demon_saved(Key) -> demon(get({ref447,Key})).
demon_wire(Key) -> demon_binary(get({wire447,Key})).
demon_binary(Bytes) -> demon(decode(Bytes)).
waiting() -> waiting(erl_ddll:info(echo_drv,awaiting_unload),0).
waiting([],N)->N;waiting([{_,Count}|Rest],N)->waiting(Rest,N+Count).
demon(Ref) ->
    try erl_ddll:demonitor(Ref) of Result -> {value,Result}
    catch Class:Reason:Stack -> {exception,Class,Reason,Stack} end.
decode(Bytes) -> binary_to_term(Bytes).
encode(Term) -> term_to_binary(Term,[{minor_version,2}]).
depth(N) -> erlang:system_flag(backtrace_depth,N),ok.
start_node(Name) ->
    Kernel=spawn(?MODULE,kernel,[]),true=register(net_kernel,Kernel),
    true=erlang:setnode(Name,4),put(kernel447,Kernel),identity().
kernel() -> receive stop -> ok end.
stop_node() ->
    Kernel=erase(kernel447),Mon=monitor(process,Kernel),Kernel!stop,
    receive {'DOWN',Mon,process,Kernel,normal}->inactive(5000) after 5000->error(kernel_timeout) end.
inactive(0)->error(node_reset_timeout);
inactive(N)->case is_alive() of false->identity();true->receive after 1->inactive(N-1) end end.
identity() -> {node(),erlang:system_info(creation),is_alive()}.
cleanup() ->
    Final=erl_ddll:monitor(driver,{echo_drv,unloaded_only}),
    Unload=erl_ddll:try_unload(echo_drv,[]),
    true=port_close(erase(port447)),
    receive {'DOWN',Final,driver,echo_drv,unloaded}->{Unload,true,unloaded} after 5000->error(unload_timeout) end.
wait_unloaded() ->
    receive {_,_,driver,echo_drv,unloaded}->true after 0->false end.

remote(Node,F,A) ->
    Ref=make_ref(),{ddll448_worker,Node}!{call,self(),Ref,F,A},
    receive {reply,Ref,{ok,V}}->V;{reply,Ref,Other}->error({remote_failure,Other})
    after 10000->error(remote_timeout) end.
seen_node(Node) -> lists:member(Node,nodes()).
store_term(Key,Value) -> put({stored448,Key},Value),true.
stored(Key) -> get({stored448,Key}).
disconnect_peer(Node) -> erlang:disconnect_node(Node).
watch_node(Node) -> monitor_node(Node,true).
await_down(Node) -> receive {nodedown,Node}->down after 10000->error(node_down_timeout) end.
