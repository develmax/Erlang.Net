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
        multi_helper_stack,multi_helper_enabled,nd_shape,nd_gate,nd_nested,nd_heap,nd_ets,nd_compressed,nd_persistent,nd_timer,nd_mailbox,nd_match,nd_compressed_key,nd_gate_stack,nd_monitor_tag,nd_offheap]],
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
scenario(nd_offheap) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),
    process_flag(message_queue_data,off_heap),self()!{nd_offheap,P,Q,R},
    Signal=nd_count(signal,nd_node(Epoch)),receive {nd_offheap,P,Q,R}->ok end,
    {Signal,nd_valid_snapshot()} end);
scenario(nd_monitor_tag) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),
    M=erlang:monitor(process,self(),[{tag,{P,Q,R}}]),
    Before=nd_count(monitor,nd_node(Epoch)),true=erlang:demonitor(M),
    After=nd_count(monitor,nd_node(Epoch)),{Before,After,nd_valid_snapshot()} end);
scenario(nd_gate_stack) -> nd_enabled(fun()->
    Queries=[{node_and_dist_references,blocked},{node_and_dist_references,false},
        {node_and_dist_references,[]},{node_and_dist_references,42}],
    [try erts_debug:get_internal_state(Q) of V->{ok,V} catch C:R:S->{C,R,S} end || Q<-Queries] end);
scenario(nd_compressed_key) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),T=ets:new(nd_key,[compressed]),
    ets:insert(T,{P,Q,R}),N=nd_count(ets,nd_node(Epoch)),ets:delete(T),{N,nd_valid_snapshot()} end);
scenario(nd_shape) -> nd_enabled(fun() ->
    {{node_references,N},{dist_references,D}}=nd_snapshot(),
    {nd_valid(N) andalso nd_valid(D),
     lists:keymember({node(),erlang:system_info(creation)},1,N),
     lists:keymember(node(),1,D),erlang:system_info(multi_scheduling)} end);
scenario(nd_gate) ->
    Disabled=nd_error(fun()->erts_debug:get_internal_state(node_and_dist_references) end),
    nd_enabled(fun()->
        A=nd_error(fun()->erts_debug:get_internal_state({node_and_dist_references,blocked}) end),
        B=nd_error(fun()->erts_debug:get_internal_state({node_and_dist_references,false}) end),
        blocked_normal=erlang:system_flag(multi_scheduling,block_normal),
        C=nd_error(fun()->erts_debug:get_internal_state({node_and_dist_references,blocked}) end),
        enabled=erlang:system_flag(multi_scheduling,unblock_normal),
        {Disabled,A,B,C,erlang:system_info(multi_scheduling)} end);
scenario(nd_nested) -> nd_enabled(fun()->
    blocked=erlang:system_flag(multi_scheduling,block),
    nd_snapshot(),blocked=erlang:system_info(multi_scheduling),
    blocked=erlang:system_flag(multi_scheduling,block),
    nd_snapshot(),blocked=erlang:system_flag(multi_scheduling,unblock),
    enabled=erlang:system_flag(multi_scheduling,unblock),true end);
scenario(nd_heap) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),put(nd_roots,{P,P,Q,R,#{P=>{Q,R}}}),
    Refs=nd_node(Epoch),
    Positive=nd_count(heap,Refs)>0,
    Owner=lists:keymember({process,self()},1,Refs),
    erase(nd_roots),{Positive,Owner,nd_valid_snapshot()} end);
scenario(nd_ets) -> nd_ets([]);
scenario(nd_compressed) -> nd_ets([compressed]);
scenario(nd_persistent) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),K={?MODULE,nd_storage},
    Before=nd_persistent_count(Epoch),
    persistent_term:put(K,{P,Q,R}),Stored=nd_persistent_count(Epoch),
    true=persistent_term:erase(K),After=nd_wait_persistent(Epoch,100),
    {Before,Stored,After,nd_valid_snapshot()} end);
scenario(nd_timer) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),
    Before=nd_count(timer,nd_node(Epoch)),
    A=erlang:start_timer(600000,self(),{P,Q,R}),One=nd_count(timer,nd_node(Epoch)),
    B=erlang:send_after(600000,self(),{P,Q,R}),Two=nd_count(timer,nd_node(Epoch)),
    erlang:cancel_timer(A),Middle=nd_count(timer,nd_node(Epoch)),
    erlang:cancel_timer(B),After=nd_count(timer,nd_node(Epoch)),
    {Before,One,Two,Middle,After,nd_valid_snapshot()} end);
scenario(nd_mailbox) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),self()!{nd_message,P,Q,R},
    Refs=nd_node(Epoch),Live=nd_count(heap,Refs)+nd_count(signal,Refs)>0,
    receive {nd_message,P,Q,R}->ok end,{Live,nd_valid_snapshot()} end);
scenario(nd_match) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),
    M=ets:match_spec_compile([{'_',[],[{const,{P,Q,R}}]}]),put(nd_match,M),
    Live=nd_count(binary,nd_node(Epoch)),erase(nd_match),
    {Live,nd_valid_snapshot()} end);
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

%% Inspect only stable contracts: validate every raw counter, then select
%% our explicit epoch and root kind. No trimming of errors or source stacks.
nd_enabled(F) ->
    Old=erts_debug:set_internal_state(available_internal_state,true),
    try F() after erts_debug:set_internal_state(available_internal_state,Old) end.
nd_error(F) -> try F() of {{node_references,_},{dist_references,_}}->ok;V->{value,V}
    catch C:R->{C,R} end.
nd_snapshot() -> erts_debug:get_internal_state(node_and_dist_references).
nd_node(Epoch) ->
    {{node_references,N},_}=nd_snapshot(),
    case lists:keyfind(Epoch,1,N) of false->[];{_,_,Refs}->Refs end.
nd_count(Kind,Refs) -> lists:sum([N || {_,Kinds}<-Refs,{K,N}<-Kinds,K=:=Kind]).
nd_valid_snapshot() -> {{_,N},{_,D}}=nd_snapshot(),nd_valid(N) andalso nd_valid(D).
nd_valid(Rows) -> lists:all(fun({_Identity,Count,Refs})->
    is_integer(Count) andalso Count>=0 andalso
    Count=:=lists:sum([N || {_,Kinds}<-Refs,{_,N}<-Kinds]) andalso
    lists:all(fun({{Kind,_},Kinds})->is_atom(Kind) andalso
        lists:all(fun({K,N})->is_atom(K) andalso is_integer(N) andalso N>0 end,Kinds)
        end,Refs) end,Rows).
nd_identifiers() ->
    Node='nd_inventory@host',Creation=77,
    <<131,Atom/binary>>=term_to_binary(Node),
    P=binary_to_term(<<131,88,Atom/binary,12:32,0:32,Creation:32>>),
    Q=binary_to_term(<<131,89,Atom/binary,13:32,Creation:32>>),
    R=binary_to_term(<<131,90,3:16,Atom/binary,Creation:32,14:32,15:32,16:32>>),
    {{Node,Creation},P,Q,R}.
nd_ets(Options) -> nd_enabled(fun()->
    {Epoch,P,Q,R}=nd_identifiers(),T=ets:new(nd_inventory,[set|Options]),
    A=nd_count(ets,nd_node(Epoch)),ets:insert(T,{a,P,Q}),
    B=nd_count(ets,nd_node(Epoch)),ets:insert(T,{b,R}),
    C=nd_count(ets,nd_node(Epoch)),ets:delete(T,b),
    D=nd_count(ets,nd_node(Epoch)),ets:delete_all_objects(T),
    E=nd_count(ets,nd_node(Epoch)),ets:delete(T),
    {A,B,C,D,E,nd_valid_snapshot()} end).

nd_persistent_count(Epoch) ->
    case lists:keyfind({system,persistent_term},1,nd_node(Epoch)) of
        false->0;{_,Kinds}->lists:sum([N || {system,N}<-Kinds]) end.
nd_wait_persistent(Epoch,0) -> {timeout,nd_persistent_count(Epoch)};
nd_wait_persistent(Epoch,N) -> case nd_persistent_count(Epoch) of
    0->0;_->erlang:garbage_collect(),receive after 1->nd_wait_persistent(Epoch,N-1) end end.
