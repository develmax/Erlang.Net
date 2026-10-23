-module(beam_ddll_identity447).
-export([new/0,request/3,actor/0,setup/1,new_ref/0,demon/1,decode/1,encode/1,start_node/1,stop_node/0,identity/0,cleanup/0,kernel/0,depth/1,wait_unloaded/0,save_ref/1,wire/1,save_wire/1,demon_saved/1,demon_wire/1,demon_binary/1,waiting/0]).
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
save_wire(Key) -> put({wire447,Key},wire(Key)),true.
demon_saved(Key) -> demon(get({ref447,Key})).
demon_wire(Key) -> demon_binary(get({wire447,Key})).
demon_binary(Bytes) -> demon(decode(Bytes)).
waiting() -> [{_,Count}]=erl_ddll:info(echo_drv,awaiting_unload),Count.
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
    receive {'DOWN',Mon,process,Kernel,normal}->identity() after 5000->error(kernel_timeout) end.
identity() -> {node(),erlang:system_info(creation),is_alive()}.
cleanup() ->
    Final=erl_ddll:monitor(driver,{echo_drv,unloaded_only}),
    Unload=erl_ddll:try_unload(echo_drv,[]),
    true=port_close(erase(port447)),
    receive {'DOWN',Final,driver,echo_drv,unloaded}->{Unload,true,unloaded} after 5000->error(unload_timeout) end.
wait_unloaded() ->
    receive {_,_,driver,echo_drv,unloaded}->true after 0->false end.
