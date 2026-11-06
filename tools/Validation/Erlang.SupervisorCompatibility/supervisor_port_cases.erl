-module(supervisor_port_cases).
-export([write/1,capture/2,observe/2,worker/3,scenario/1,init/1,start_worker/3,child_init/4,child_loop/3]).

write(Dir) ->
    _=erl_features:keywords(),
    Features=[{{erl_features,K},persistent_term:get({erl_features,K})} || K<-[feature_specs,enabled_features,keywords,init_done]],
    save(Dir,"feature-bootstrap.etf",Features),
    save(Dir,"exports.etf",supervisor:module_info(exports)--[{module_info,0},{module_info,1}]),
    save(Dir,"original.etf",[{F,A,observe(F,A)} || {F,A}<-cases()]),
    Names=[{upstream,F} || F<-[sup_start_normal,sup_start_ignore_init,sup_start_ignore_child,
          sup_start_ignore_temporary_child,sup_start_ignore_temporary_child_start_child,
          sup_start_ignore_temporary_child_start_child_simple,sup_start_ignore_permanent_child_start_child_simple,
          sup_start_error_return,sup_start_fail,sup_start_child_returns_error,sup_start_restart_child_returns_error,
          sup_start_child_returns_error_simple,sup_start_map,sup_start_map_simple,sup_start_map_faulty_specs,
          child_specs,child_specs_map,sup_flags,extra_return]] ++
          [basic,dynamic,upgrade,system,named,callback_module,init_results,shutdown_parallel,unlinked,hibernate,
           startup_rollback,retry,retry_all,retry_rest,retry_simple,extra_return,temporary_arguments,exact_ids,shutdown_barrier] ++
          [{order,S} || S<-[one_for_one,one_for_all,rest_for_one]] ++
          [{restart,S,R,E} || S<-[one_for_one,one_for_all,rest_for_one,simple_one_for_one],
             R<-[permanent,transient,temporary],E<-[normal,shutdown,{shutdown,detail},broken]] ++
          [{significant,A,R,E} || A<-[any_significant,all_significant],R<-[transient,temporary],E<-[normal,broken]] ++
          [{shutdown,T} || T<-[0,10,infinity,brutal_kill]] ++
          [{intensity,S} || S<-[one_for_one,one_for_all,rest_for_one,simple_one_for_one]],
    save(Dir,"effects.etf",[{N,begin V=scenario(N),case N of {upstream,_}->ok=V;_->ok end,V end} || N<-Names]),ok.
save(D,F,V) -> ok=file:write_file(filename:join(D,F),term_to_binary(V)).

capture(F,A) -> try apply(supervisor,F,A) of V->{ok,canonical(V)} catch C:R:S->{C,canonical(R),S} end.
observe(F,A) ->
    {P,M}=spawn_monitor(?MODULE,worker,[self(),F,A]),
    receive {P,V}->erlang:demonitor(M,[flush]),V; {'DOWN',M,process,P,R}->{worker_failed,R}
    after 5000->exit(observer_timeout) end.
worker(P,F,A) -> P!{self(),capture(F,A)}.
canonical(V) when is_pid(V) -> pid;
canonical(V) when is_reference(V) -> reference;
canonical(V) when is_tuple(V) -> list_to_tuple([canonical(X)||X<-tuple_to_list(V)]);
canonical(V) when is_map(V) -> maps:from_list([{canonical(K),canonical(X)}||{K,X}<-maps:to_list(V)]);
canonical([H|T]) -> [canonical(H)|canonical(T)];
canonical(V) -> V.

base() -> #{id=>child,start=>{?MODULE,start_worker,[self(),child,ignore]}}.
cases() ->
    [{behaviour_info,[X]} || X<-[callbacks,optional_callbacks,other]] ++
    [{check_childspecs,[V]} || V<-[[],[base()],[base(),base()],bad,42,#{},[bad],[#{}],[#{id=>a}]]] ++
    [{check_childspecs,[[maps:put(K,V,base())],A]} || A<-[undefined,never,any_significant,all_significant,bad],
       {K,Vs}<-[{id,[a,1,1.0,#{},[]]},{start,[bad,{a,b,[]},{a,b,[1]},{1,b,[]},{a,b,bad}]},
                 {restart,[permanent,transient,temporary,bad]}, {significant,[true,false,bad,0]},
                 {type,[worker,supervisor,bad]}, {shutdown,[0,1,5000,16#100000000,infinity,brutal_kill,-1,bad]},
                 {modules,[[],[a],[1],dynamic,bad]}],V<-Vs] ++
    [{check_childspecs,[[{a,{a,b,[]},R,S,T,M}]]} || R<-[permanent,transient,temporary,bad],S<-[0,infinity,brutal_kill],T<-[worker,supervisor],M<-[[],dynamic]] ++
    [{init,[{self,?MODULE,{F,[]}}]} || F<-[#{}, {one_for_one,1,5},bad,[],{bad,1,5},
      #{intensity=>-1},#{intensity=>16#100000000},#{period=>0},#{period=>16#100000000},#{auto_shutdown=>bad},
      #{strategy=>simple_one_for_one},#{hibernate_after=>-1},#{hibernate_after=>0},#{hibernate_after=>infinity},
      #{hibernate_after=>16#100000000},#{strategy=>simple_one_for_one,auto_shutdown=>any_significant}]] ++
    [{format_log,[report(P),O]} || P<-[true,false],O<-[#{},#{single_line=>true},#{depth=>3},#{single_line=>true,depth=>3},#{encoding=>latin1},#{chars_limit=>80}]] ++
    [{format_log,[report(P)]} || P<-[true,false]] ++
    [{handle_call,[Request,from,state(Strategy)]} || Strategy<-[one_for_one,simple_one_for_one],
         Request<-[which_children,count_children,{get_childspec,missing},{restart_child,missing},
                   {delete_child,missing},{terminate_child,missing},{which_child,missing}]] ++
    [{handle_info,[Message,state(one_for_one)]} || Message<-[{hibernate,tag},{'EXIT',not_a_pid,normal},unexpected]] ++
    [{handle_cast,[{try_again_restart,tag,missing},state(one_for_one)]},
     {terminate,[normal,state(one_for_one)]},{terminate,[normal,state(simple_one_for_one)]}] ++
    [{code_change,[old,setelement(16,state(one_for_one),Arg),extra]} || Arg<-[ignore,{return,bad},{bad,[]},
       {#{intensity=>-1},[]},{#{},[bad]},{#{},[base()]},{#{},[]}]].

state(Strategy) ->
    C={child,undefined,template,{?MODULE,start_worker,[self()]},permanent,false,5000,worker,[?MODULE]},
    Children=case Strategy of simple_one_for_one->{[template],#{template=>C}};_->{[],#{}} end,
    Dynamics=case Strategy of simple_one_for_one->{maps,#{}};_->undefined end,
    {state,{local,sup},Strategy,Children,Dynamics,1,5,[],0,0,never,infinity,false,tag,?MODULE,{#{},[]}}.

report(true) -> #{label=>{supervisor,progress},report=>[{supervisor,{local,sup}},{started,offender()}]};
report(false) -> #{label=>{supervisor,child_terminated},report=>[{supervisor,{local,sup}},{errorContext,child_terminated},{reason,{broken,[a,b,c]}},{offender,offender()}]}.
offender() -> [{pid,undefined},{id,child},{mfargs,{m,f,[a,b,c]}},{restart_type,permanent},{significant,false},{shutdown,5000},{child_type,worker}].

init(ignore) -> ignore;
init({return,V}) -> V;
init({raise,C}) -> erlang:C(broken);
init({config,T}) -> [{config,F,C}]=ets:lookup(T,config),{ok,{F,C}};
init({Flags,Children}) -> {ok,{Flags,Children}}.

spec(Id,Restart,Options) -> #{id=>Id,start=>{?MODULE,start_worker,[self(),Id,Options]},restart=>Restart,shutdown=>50}.
start_worker(_Owner,_Id,ignore) -> ignore;
start_worker(_Owner,_Id,{error,R}) -> {error,R};
start_worker(_Owner,_Id,{return,R}) -> R;
start_worker(Owner,Id,{retry,T}) ->
    N=ets:update_counter(T,Id,1,{Id,0}),
    case N of 2->{error,once}; _->start_worker(Owner,Id,normal) end;
start_worker(Owner,Id,Options) ->
    P=spawn_link(?MODULE,child_init,[self(),Owner,Id,Options]),
    receive {ready,P}->ok end,
    Owner!{started,Id,P},
    case Options of extra->{ok,P,extra}; _->{ok,P} end.
child_init(Parent,Owner,Id,Options) ->
    process_flag(trap_exit,true),
    Parent!{ready,self()},
    child_loop(Owner,Id,Options).
child_loop(Owner,Id,Options) ->
    receive
        {exit,R}->exit(R);
        {unlink,P,From}->unlink(P),From!unlinked,child_loop(Owner,Id,Options);
        {'EXIT',_,shutdown} when Options=:=hang -> child_loop(Owner,Id,Options);
        {'EXIT',_,shutdown} when Options=:=barrier -> Owner!{barrier,Id,self()},receive release->exit(shutdown) end;
        {'EXIT',_,R}->Owner!{stopped,Id,R},exit(R);
        stop->exit(normal)
    end.
start(Flags,Children) -> {ok,P}=supervisor:start_link(?MODULE,{Flags,Children}),unlink(P),P.
start_notice(Id) -> receive {started,Id,P}->P after 2000->exit({no_start,Id}) end.
drain() -> receive _ -> drain() after 0->ok end.
finish(P) -> ok=supervisor:stop(P),drain(),ok.
snapshot(P) -> [{I,is_pid(C),T,M} || {I,C,T,M}<-supervisor:which_children(P)].
await_dead(P) -> M=monitor(process,P),receive {'DOWN',M,process,P,R}->R after 2000->exit(no_down) end.
wait_change(P,Old,N) ->
    Children=supervisor:which_children(P),
    case lists:any(fun({_,C,_,_})->C=:=Old end,Children) of
        false->Children;
        true when N>0->receive after 1->wait_change(P,Old,N-1) end;
        true->exit(no_change)
    end.

scenario({upstream,F}) ->
    {P,M}=spawn_monitor(fun()->
        V=try apply(supervisor_SUITE,F,[[]]) of _->ok catch C:R:S->{C,R,S} end,
        case whereis(sup_test) of undefined->ok; Sup->catch supervisor:stop(Sup,shutdown,4000) end,
        exit({test_result,V})
    end),
    receive {'DOWN',M,process,P,{test_result,V}}->V; {'DOWN',M,process,P,R}->{failed,R} after 300000->exit(P,kill),exit({upstream_timeout,F}) end;
scenario(basic) ->
    P=start(#{},[spec(a,permanent,normal)]),_=start_notice(a),
    Before=snapshot(P),{ok,S}=supervisor:get_childspec(P,a),
    Del=supervisor:delete_child(P,a),ok=supervisor:terminate_child(P,a),
    Inactive=snapshot(P),{ok,_}=supervisor:restart_child(P,a),_=start_notice(a),
    ok=supervisor:terminate_child(P,a),ok=supervisor:delete_child(P,a),
    Missing=supervisor:restart_child(P,a),Count=supervisor:count_children(P),finish(P),
    {Before,lists:sort(maps:keys(S)),Del,Inactive,Missing,Count};
scenario(dynamic) ->
    P=start(#{strategy=>simple_one_for_one},[#{id=>template,start=>{?MODULE,start_worker,[self()]}}]),
    {ok,C}=supervisor:start_child(P,[a,normal]),_=start_notice(a),
    W=supervisor:which_child(P,C),G=supervisor:get_childspec(P,C),
    R=[supervisor:restart_child(P,C),supervisor:delete_child(P,C),supervisor:which_child(P,template)],
    ok=supervisor:terminate_child(P,C),Dead=supervisor:terminate_child(P,C),
    Count=supervisor:count_children(P),finish(P),{canonical(W),canonical(G),R,Dead,Count};
scenario({restart,Strategy,Restart,Reason}) ->
    Simple=Strategy=:=simple_one_for_one,
    Specs=case Simple of true->[#{id=>template,start=>{?MODULE,start_worker,[self()]},restart=>Restart,shutdown=>50}];
                             false->[spec(a,Restart,normal),spec(b,permanent,normal),spec(c,permanent,normal)] end,
    P=start(#{strategy=>Strategy,intensity=>10},Specs),
    Old=case Simple of true->{ok,C}=supervisor:start_child(P,[a,normal]),C; false->start_notice(a) end,
    case Simple of true->_=start_notice(a);false->_=start_notice(b),_=start_notice(c) end,
    Old!{exit,Reason},_=wait_change(P,Old,2000),
    Count=supervisor:count_children(P),Snap=snapshot(P),finish(P),{Count,Snap};
scenario({significant,Automatic,Restart,Reason}) ->
    A=(spec(a,Restart,normal))#{significant=>true}, B=(spec(b,Restart,normal))#{significant=>true},
    P=start(#{auto_shutdown=>Automatic},[A,B]),C=start_notice(a),D=start_notice(b),
    C!{exit,Reason},
    case Restart=:=transient andalso Reason=:=broken of
        true->_=wait_change(P,C,2000),finish(P),restarted;
        false->case Automatic of all_significant->_=wait_change(P,C,2000),D!{exit,normal};_->ok end, R=await_dead(P),drain(),R
    end;
scenario({intensity,Strategy}) ->
    Simple=Strategy=:=simple_one_for_one,
    S=case Simple of true->#{id=>t,start=>{?MODULE,start_worker,[self()]}};false->spec(a,permanent,normal) end,
    P=start(#{strategy=>Strategy,intensity=>0},[S]),
    C=case Simple of true->{ok,X}=supervisor:start_child(P,[a,normal]),X;false->start_notice(a) end,
    case Simple of true->_=start_notice(a);false->ok end,
    C!{exit,broken},R=await_dead(P),drain(),R;
scenario({shutdown,Timeout}) ->
    P=start(#{},[(spec(a,permanent,case Timeout of infinity->normal;_->hang end))#{shutdown=>Timeout}]),
    C=start_notice(a),ok=supervisor:terminate_child(P,a),R=await_dead(C),finish(P),R;
scenario(shutdown_parallel) ->
    P=start(#{strategy=>simple_one_for_one},[#{id=>t,start=>{?MODULE,start_worker,[self()]},shutdown=>10}]),
    Cs=[begin {ok,C}=supervisor:start_child(P,[I,hang]),_=start_notice(I),C end || I<-[a,b,c]],
    ok=supervisor:stop(P),R=[await_dead(C)||C<-Cs],drain(),R;
scenario(shutdown_barrier) ->
    Owner=self(),P=start(#{strategy=>simple_one_for_one},[#{id=>t,start=>{?MODULE,start_worker,[self()]},shutdown=>infinity}]),
    _=[begin {ok,_}=supervisor:start_child(P,[I,barrier]),_=start_notice(I) end || I<-[a,b,c]],
    spawn(fun()->R=supervisor:stop(P),Owner!{barrier_stopped,R} end),
    Cs=[receive {barrier,I,C}->{I,C} after 2000->exit(missing_parallel_shutdown) end||_<-[1,2,3]],
    [C!release||{_,C}<-Cs],receive {barrier_stopped,ok}->ok after 2000->exit(no_shutdown) end,
    drain(),lists:sort([I||{I,_}<-Cs]);
scenario({order,Strategy}) ->
    P=start(#{strategy=>Strategy,intensity=>10},[spec(a,permanent,normal),spec(b,permanent,normal),spec(c,permanent,normal)]),
    _=start_notice(a),B=start_notice(b),_=start_notice(c),B!{exit,broken},
    Expected=case Strategy of one_for_one->[b];one_for_all->[a,b,c];rest_for_one->[b,c] end,
    Started=[receive {started,I,_}->I after 2000->exit(no_restart_order) end||_<-Expected],
    Stopped=collect_stopped([]),finish(P),{Started,Stopped};
scenario(unlinked) ->
    P=start(#{},[spec(a,permanent,normal)]),C=start_notice(a),C!{unlink,P,self()},receive unlinked->ok end,
    ok=supervisor:terminate_child(P,a),R=await_dead(C),finish(P),R;
scenario(hibernate) ->
    P=start(#{hibernate_after=>0},[]),_=sys:get_state(P),receive after 5->ok end,
    S=sys:get_state(P),H=element(13,S),_=supervisor:count_children(P),finish(P),H;
scenario(upgrade) ->
    T=ets:new(config,[public]),ets:insert(T,{config,#{},[spec(a,permanent,normal)]}),
    {ok,P}=supervisor:start_link(?MODULE,{config,T}),unlink(P),C=start_notice(a),
    ets:insert(T,{config,#{intensity=>4},[spec(a,transient,normal),spec(b,temporary,normal)]}),
    ok=sys:suspend(P),ok=sys:change_code(P,supervisor,old,extra),ok=sys:resume(P),
    {ok,S}=supervisor:get_childspec(P,a),W=supervisor:which_children(P),
    Result={maps:get(restart,S),[{I,X=:=C}||{I,X,_,_}<-W]},finish(P),ets:delete(T),Result;
scenario(system) ->
    P=start(#{},[]),S=sys:get_state(P),ok=sys:suspend(P),ok=sys:resume(P),
    S=sys:replace_state(P,fun(X)->X end),finish(P),{element(1,S),tuple_size(S)};
scenario(callback_module) -> P=start(#{},[]),M=supervisor:get_callback_module(P),finish(P),M;
scenario(named) ->
    {ok,P}=supervisor:start_link({local,supervisor_port_name},?MODULE,{#{},[]}),unlink(P),
    R=supervisor:start_link({local,supervisor_port_name},?MODULE,{#{},[]}),finish(P),canonical(R);
scenario(init_results) ->
    process_flag(trap_exit,true),
    Rs=[canonical(supervisor:start_link(?MODULE,V))||V<-[ignore,{return,bad},{#{strategy=>simple_one_for_one},[]},{#{},[spec(a,temporary,ignore)]}]],
    drain(),Rs;
scenario(startup_rollback) ->
    process_flag(trap_exit,true),
    R=supervisor:start_link(?MODULE,{#{},[spec(a,permanent,normal),spec(b,permanent,{error,broken})]}),
    C=start_notice(a),_=await_dead(C),drain(),R;
scenario(Name) when Name=:=retry;Name=:=retry_all;Name=:=retry_rest;Name=:=retry_simple ->
    T=ets:new(retries,[public]),
    Strategy=case Name of retry->one_for_one;retry_all->one_for_all;retry_rest->rest_for_one;retry_simple->simple_one_for_one end,
    Simple=Strategy=:=simple_one_for_one,
    Specs=case Simple of true->[#{id=>t,start=>{?MODULE,start_worker,[self()]}}];false->[spec(a,permanent,{retry,T}),spec(b,permanent,normal)] end,
    P=start(#{strategy=>Strategy,intensity=>5},Specs),
    case Simple of true->{ok,_}=supervisor:start_child(P,[a,{retry,T}]);false->ok end,
    C=start_notice(a),C!{exit,broken},New=start_notice(a),
    Count=supervisor:count_children(P),Result={C=/=New,Count,ets:lookup(T,a)},finish(P),ets:delete(T),Result;
scenario(extra_return) -> P=start(#{},[]),{ok,C,extra}=supervisor:start_child(P,spec(a,permanent,extra)),_=start_notice(a),finish(P),is_pid(C);
scenario(temporary_arguments) -> P=start(#{},[]),{ok,_}=supervisor:start_child(P,spec(a,temporary,normal)),_=start_notice(a),{ok,S}=supervisor:get_childspec(P,a),finish(P),element(3,maps:get(start,S));
scenario(exact_ids) -> P=start(#{},[]),{ok,_}=supervisor:start_child(P,spec(1,permanent,ignore)),{ok,_}=supervisor:start_child(P,spec(1.0,permanent,ignore)),S=supervisor:count_children(P),finish(P),S.

collect_stopped(Acc) -> receive {stopped,I,shutdown}->collect_stopped(Acc++[I]) after 0->Acc end.
