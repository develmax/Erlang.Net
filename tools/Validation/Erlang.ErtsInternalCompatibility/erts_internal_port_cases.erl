-module(erts_internal_port_cases).
-export([write/1, capture/2, scenario/1, identity/1, worker/3, scenario_worker/2, multi_owner/3, multi_bound_worker/1]).

write(Directory) ->
    Rows = [{F,A,observe(F,A)} || {F,A} <- cases()],
    ok=file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Rows)),
    ok=file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        erts_internal:module_info(exports)--[{module_info,0},{module_info,1}])),
    Effects=[{N,observe_scenario(N)} || N <- [port_false,port_true,port_other,await_reply,io_bytes,
        flush_one,flush_all,flush_nonboolean,flush_empty,flush_refopt,
        microstates,microstate_await,microstate_zero,microstate_runtime,wall_time,wall_undefined,wall_await,
        system_check,spawn,spawn_error,dist_spawn,dist_spawn_error,dist_decode_failure,dynamic,alive,dead,
        check_self,check_dead,check_async,check_options,atom_map_order,ets,ets_bad,blocked,breakpoint,
        multi_sequences,multi_bound,multi_online,multi_owners,multi_death,multi_exception,multi_helper_cleanup,multi_badargs,
        multi_helper_stack,multi_helper_enabled]],
    ok=file:write_file(filename:join(Directory,"effects.etf"),term_to_binary(Effects)),ok.

worker(F,A,Parent) -> Parent!{self(),capture(F,A)}.
observe(F,A) -> P=spawn(?MODULE,worker,[F,A,self()]),receive {P,V}->V after 5000->error(observation_timeout) end.
scenario_worker(Name,Parent) -> Parent!{self(),scenario(Name)}.
observe_scenario(Name) -> P=spawn(?MODULE,scenario_worker,[Name,self()]),receive {P,V}->V after 5000->error({scenario_timeout,Name}) end.

capture(F,A) ->
    try apply(erts_internal,F,A) of V -> {ok,V}
    catch C:R:S -> request_reference({C,R,S})
    end.

%% Project only the fresh request identifier in this exact native error frame.
%% Keep the complete reason, remaining arguments, source frames and metadata.
request_reference({C,R,[{erts_internal,request_system_task,[P,Prio,{check_process_code,Ref,M}],Info}|Tail]}) when is_reference(Ref) ->
    {C,R,[{erts_internal,request_system_task,[P,Prio,{check_process_code,runtime_reference,M}],Info}|Tail]};
request_reference(Outcome) -> Outcome.

cases() ->
    Terms=[bad,[],{},42,1.5,<<>>,true,false],
    [{F,[T]} || F <- [await_result,gather_system_check_result,gather_sched_wall_time_result,mc_refill],T<-Terms] ++
    [{gather_io_bytes,[R,N]} || R<-Terms,N<-[0,-1,1,bad,1.5]] ++
    [{flush_monitor_messages,[R,true,result]} || R<-Terms] ++
    [{dynamic_node_name,[V]} || V <- [bad,0,[],{},<<>>]] ++
    [{spawn_init,[V]} || V <- Terms] ++
    [{dist_spawn_init,[V]} || V <- [bad,[],{},42]] ++
    [{mc_iterator,[V]} || V <- Terms++[none,{a,b,none},{a,b,bad},[bad|#{}],#{}]] ++
    [{mc_refill,[[P|#{}]]} || P <- [0,1,-1,bad,[]]] ++
    [{check_process_code,[bad,missing_module,O]} || O<-[bad,[bad],[{allow_gc,bad}],[{async,x}|bad]]] ++
    [{check_process_code,[bad,missing_module,O]} || O<-[[],[{async,request}],[{allow_gc,true}]]] ++
    [{dist_ctrl_put_data,[bad,V]} || V<-[[],[1,2,3],<<"abc">>,[256],bad,[1|bad]]] ++
    [{term_to_string,[T]} || T <- [bad,[],{},42,-123,1.5,<<"hello">>,[a,b],#{1=>a,2=>b}]] ++
    [{spawn_init,[{erlang,error,[witness]}]}] ++
    [{F,[A,B]} || F <- [cmp_term], A <- Terms, B <- Terms] ++
    [{binary_to_integer,[B,Base]} || B <- [<<"12">>,<<"-12">>,<<"12x">>,<<>>,<<"ff">>,<<"+0012">>,bad],Base<-[2,10,16,36]] ++
    [{list_to_integer,[L,Base]} || L <- ["12","-12","12x",[],"ff","+0012",bad,[1|bad]],Base<-[2,10,16,36]].

identity(X) -> X.

scenario(port_false) -> port(false);
scenario(port_true) -> port(true);
scenario(port_other) -> port(other);
scenario(await_reply) ->
    R=make_ref(),self()!untouched,self()!{R,value},
    {erts_internal:await_result(R),receive X -> X end};
scenario(io_bytes) ->
    R=make_ref(),self()!untouched,self()!{R,2,10,20},self()!{R,1,30,40},
    {erts_internal:gather_io_bytes(R,2),receive X -> X end};
scenario(flush_one) -> flush(false);
scenario(flush_all) -> flush(true);
scenario(flush_nonboolean) -> flush(other);
scenario(flush_empty) -> erts_internal:flush_monitor_messages(make_ref(),true,result);
scenario(flush_refopt) -> erts_internal:'@flush_monitor_messages_refopt'();
scenario(microstates) ->
    R=make_ref(),self()!untouched,self()!R,self()!{R,first},self()!{R,second},
    {erts_internal:gather_microstate_accounting_result(R,3),receive X -> X end};
scenario(microstate_await) ->
    R=make_ref(),self()!R,self()!{R,value},erts_internal:await_microstate_accounting_modifications(R,result,2);
scenario(microstate_zero) -> erts_internal:gather_microstate_accounting_result(bad,0);
scenario(microstate_runtime) ->
    _=erlang:system_flag(microstate_accounting,false),
    A=erlang:system_flag(microstate_accounting,true),
    B=is_list(erlang:statistics(microstate_accounting)),
    C=erlang:system_flag(microstate_accounting,false),{A,B,C};
scenario(wall_time) ->
    R=make_ref(),N=erlang:system_info(schedulers),
    [self()!{R,[{I,I*2,I*3}]} || I<-lists:seq(1,N)],
    erts_internal:gather_sched_wall_time_result(R)==[{I,I*2,I*3} || I<-lists:seq(1,N)];
scenario(wall_undefined) ->
    R=make_ref(),N=erlang:system_info(schedulers),
    self()!{R,undefined},[self()!{R,ignored} || _<-lists:seq(2,N)],
    erts_internal:gather_sched_wall_time_result(R);
scenario(wall_await) ->
    R=make_ref(),[self()!R || _<-lists:seq(1,erlang:system_info(schedulers))],
    erts_internal:await_sched_wall_time_modifications(R,result);
scenario(system_check) ->
    R=make_ref(),[self()!R || _<-lists:seq(1,erlang:system_info(schedulers))],
    erts_internal:gather_system_check_result(R);
scenario(spawn) -> erts_internal:spawn_init({?MODULE,identity,[value]});
scenario(spawn_error) -> capture(spawn_init,[{erlang,error,[witness]}]);
scenario(dist_spawn) -> self()![value],self()!dist_spawn_init,erts_internal:dist_spawn_init({?MODULE,identity,1});
scenario(dist_spawn_error) ->
    self()![witness],self()!dist_spawn_init,capture(dist_spawn_init,[{erlang,error,1}]);
scenario(dist_decode_failure) ->
    self()!dist_spawn_init,
    try erts_internal:dist_spawn_init({?MODULE,identity,1}) catch exit:R -> {exit,R} end;
scenario(dynamic) ->
    Key={erts_internal,dynamic_node_name},persistent_term:erase(Key),
    A=erts_internal:dynamic_node_name(),ok=erts_internal:dynamic_node_name(true),
    B=erts_internal:dynamic_node_name(),persistent_term:put(Key,other),
    C=erts_internal:dynamic_node_name(),ok=erts_internal:dynamic_node_name(false),
    {A,B,C,erts_internal:dynamic_node_name(),persistent_term:get(Key,missing)};
scenario(alive) -> erts_internal:is_process_alive(self());
scenario(dead) -> P=spawn(fun()->ok end),R=monitor(process,P),receive {'DOWN',R,_,_,_}->ok end,erts_internal:is_process_alive(P);
scenario(check_self) -> erts_internal:check_process_code(self(),missing_module,[]);
scenario(check_dead) ->
    P=spawn(fun()->ok end),R=monitor(process,P),receive {'DOWN',R,_,_,_}->ok end,
    erts_internal:check_process_code(P,missing_module,[]);
scenario(check_async) ->
    async=erts_internal:check_process_code(self(),missing_module,[{async,first},{async,last}]),
    receive {check_process_code,last,V}->V end;
scenario(check_options) -> erts_internal:check_process_code(self(),missing_module,[{allow_gc,false},{allow_gc,true}]);
scenario(atom_map_order) ->
    M=#{a=>1,b=>2},
    Expected=case erts_internal:map_to_tuple_keys(M) of
        {a,b}->"#{a=>1,b=>2}";
        {b,a}->"#{b=>2,a=>1}"
    end,
    erts_internal:term_to_string(M)=:=Expected;
scenario(ets) ->
    T=ets:new(?MODULE,[set]),ets:insert(T,[{a,1},{b,2}]),
    V=erts_internal:ets_info_binary(T),F=ets:info(T,safe_fixed),ets:delete(T),{V,F};
scenario(ets_bad) -> capture(ets_info_binary,[missing_table]);
scenario(blocked) ->
    try erts_internal:get_internal_state_blocked(bad) of V -> {ok,V}
    catch C:R -> {C,R} end;
scenario(multi_sequences) ->
    Ops=[block,unblock,block_normal,unblock_normal],
    Sequences=[[]]++[[A] || A<-Ops]++[[A,B] || A<-Ops,B<-Ops]++
        [[A,B,C] || A<-Ops,B<-Ops,C<-Ops]++[[A,B,C,D] || A<-Ops,B<-Ops,C<-Ops,D<-Ops],
    [{S,multi_sequence(S)} || S<-Sequences];
scenario(multi_bound) ->
    P=spawn(?MODULE,multi_bound_worker,[self()]),
    receive {P,ready}->ok end,
    blocked=erlang:system_flag(multi_scheduling,block),
    P!probe,
    During=receive {P,ran,_}->unexpected after 0->suspended end,
    enabled=erlang:system_flag(multi_scheduling,unblock),
    After=receive {P,ran,Id}->Id end,{During,After};
scenario(multi_online) ->
    Before=erlang:system_info(schedulers_online),
    blocked=erlang:system_flag(multi_scheduling,block),
    true=Before=:=erlang:system_info(schedulers_online),
    Before=erlang:system_flag(schedulers_online,1),
    1=erlang:system_info(schedulers_online),
    enabled=erlang:system_flag(multi_scheduling,unblock),
    1=erlang:system_flag(schedulers_online,Before),
    erlang:system_info(schedulers_online)=:=Before;
scenario(multi_owners) ->
    A=spawn(?MODULE,multi_owner,[self(),[block,block],normal]),RA=monitor(process,A),
    receive {A,ready}->ok end,
    B=spawn(?MODULE,multi_owner,[self(),[block_normal,block_normal],normal]),RB=monitor(process,B),
    receive {B,ready}->ok end,
    Initial={erlang:system_flag(multi_scheduling,unblock),
        lists:member(A,erlang:system_info(multi_scheduling_blockers)),
        lists:member(B,erlang:system_info(normal_multi_scheduling_blockers))},
    A!stop,receive {'DOWN',RA,_,_,normal}->ok end,
    wait_multi(blocked_normal),
    Middle={erlang:system_info(multi_scheduling),erlang:system_info(multi_scheduling_blockers)=:=[]},
    B!stop,receive {'DOWN',RB,_,_,normal}->ok end,
    wait_multi(enabled),{Initial,Middle,erlang:system_info(normal_multi_scheduling_blockers)};
scenario(multi_death) -> multi_death(kill);
scenario(multi_exception) -> multi_death(witness);
scenario(multi_helper_cleanup) ->
    Values=[enabled,blocked_normal,blocked],
    [{S,helper_cleanup(S)} || S<-Values];
scenario(multi_badargs) ->
    [{V,try erlang:system_flag(multi_scheduling,V) of X->{ok,X} catch C:R:S->{C,R,S} end}
        || V<-[bad,true,false,[],{},42,1.5,<<>>]];
scenario(multi_helper_stack) ->
    R=capture(get_internal_state_blocked,[bad]),{R,erlang:system_info(multi_scheduling)};
scenario(multi_helper_enabled) ->
    Old=erts_debug:set_internal_state(available_internal_state,true),
    try
        R=capture(get_internal_state_blocked,[bad]),{R,erlang:system_info(multi_scheduling)}
    after erts_debug:set_internal_state(available_internal_state,Old) end;
scenario(breakpoint) ->
    try erts_internal:breakpoint(?MODULE,identity,1,1) of V -> {ok,V}
    catch C:R -> {C,R} end.

multi_sequence(Sequence) ->
    Results=[begin
        Result=erlang:system_flag(multi_scheduling,Op),
        {Result,erlang:system_info(multi_scheduling),
            multi_self_count(multi_scheduling_blockers),multi_self_count(normal_multi_scheduling_blockers)}
        end || Op<-Sequence],
    [begin erlang:system_flag(multi_scheduling,unblock),erlang:system_flag(multi_scheduling,unblock_normal) end
        || _<-lists:seq(1,length(Sequence)+1)],
    enabled=erlang:system_info(multi_scheduling),Results.
multi_self_count(Key) -> length([P || P<-erlang:system_info(Key),P=:=self()]).
multi_owner(Parent,Operations,Reason) ->
    [erlang:system_flag(multi_scheduling,Op) || Op<-Operations],
    Parent!{self(),ready},receive stop->exit(Reason) end.
multi_bound_worker(Parent) ->
    0=erlang:process_flag(scheduler,2),Parent!{self(),ready},
    receive probe->Parent!{self(),ran,erlang:system_info(scheduler_id)} end.
multi_death(Reason) ->
    P=spawn(?MODULE,multi_owner,[self(),[block_normal,block,block_normal,block],normal]),
    R=monitor(process,P),receive {P,ready}->ok end,
    exit(P,Reason),receive {'DOWN',R,_,_,_}->ok end,
    wait_multi(enabled),
    {erlang:system_info(multi_scheduling_blockers),erlang:system_info(normal_multi_scheduling_blockers)}.
wait_multi(State) ->
    case erlang:system_info(multi_scheduling) of State->ok;_->receive after 1->wait_multi(State) end end.
helper_cleanup(State) ->
    case State of enabled->ok;blocked_normal->erlang:system_flag(multi_scheduling,block_normal);
        blocked->erlang:system_flag(multi_scheduling,block) end,
    Result=try erts_internal:get_internal_state_blocked(bad) of V->{ok,V} catch C:R->{C,R} end,
    State=erlang:system_info(multi_scheduling),
    erlang:system_flag(multi_scheduling,unblock),erlang:system_flag(multi_scheduling,unblock_normal),
    {Result,State,erlang:system_info(multi_scheduling)}.

port(Value) -> R=make_ref(),self()!untouched,self()!{R,Value},
    {erts_internal:await_port_send_result(R,busy,ok),receive X->X end}.
flush(Multi) ->
    R=make_ref(),self()!untouched,self()!{down,R,pid,pid,reason},self()!{other,R,a,b,c},
    V=erts_internal:flush_monitor_messages(R,Multi,result),
    {V,drain([])}.
drain(Acc) -> receive {Tag,_,A,B,C}->drain([{Tag,reference,A,B,C}|Acc]);X->drain([X|Acc]) after 0->lists:reverse(Acc) end.
