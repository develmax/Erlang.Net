-module(gen_statem_port_cases).
-export([write/1,capture/2,observe/2,worker/3,scenario/1,init/1,callback_mode/0,handle_event/4,
         idle/3,active/3,terminate/3,code_change/4,identity/1,enter_child/1]).

write(Dir) ->
    _=erl_features:keywords(),
    FeatureState=[{{erl_features,K},persistent_term:get({erl_features,K})} || K<-[feature_specs,enabled_features,keywords,init_done]],
    ok=file:write_file(filename:join(Dir,"feature-bootstrap.etf"),term_to_binary(FeatureState)),
    Exports=gen_statem:module_info(exports) -- [{module_info,0},{module_info,1}],
    ok=file:write_file(filename:join(Dir,"exports.etf"),term_to_binary(Exports)),
    Cases=cases(),
    ok=file:write_file(filename:join(Dir,"original.etf"),term_to_binary([{F,A,observe(F,A)} || {F,A} <- Cases])),
    Names=[basic,state_functions,state_enter,postpone,next_event,zero_order,timeout_update,
           timeout_cancel,state_timeout,event_timeout,repeat,requests,collections,system,
           callback_stack,monitor,named,init_results,throws,hibernate,stop_reply,
           enter_loop,absolute_timer,real_timer,format_status,cross_module,sys_terminate_once,sys_error_once] ++
          [{bad_return,V} || V<-[bad,{},[],{next_state,a},{keep_state},{stop,bad,one,extra}]] ++
          [{bad_action,A} || A<-[bad,{reply,bad,x},{next_event,bad,x},pop_callback_module,
                                {postpone,bad},{hibernate,bad},{state_timeout,-1,x},
                                {{timeout,n},1,x,{abs,bad}},{state_timeout,0,x,[bad]}]] ++
          [{bad_mode,M} || M<-[[],bad,[state_enter],[state_functions,bad]]] ++
          [{enter_action,A} || A<-[postpone,{postpone,true},{next_event,internal,x},
                                 {change_callback_module,?MODULE},{push_callback_module,?MODULE}]] ++
          [{callback_raise,C} || C<-[error,exit,throw]] ++
          [{sequence,M,[E1,E2]} || M<-[state_functions,handle_event_function,[state_functions,state_enter],[handle_event_function,state_enter]],
              E1<-sequence_events(),E2<-sequence_events()],
    ok=file:write_file(filename:join(Dir,"effects.etf"),term_to_binary([{N,scenario(N)} || N<-Names])),ok.

capture(F,A) ->
    try apply(gen_statem,F,A) of V->{ok,V}
    catch C:R:S->{C,R,S}
    end.

observe(F,A) ->
    {P,M}=spawn_monitor(?MODULE,worker,[self(),F,A]),
    receive {P,V}->erlang:demonitor(M,[flush]),V;
            {'DOWN',M,process,P,R}->{worker_failed,R}
    after 5000->exit(observer_timeout)
    end.
worker(P,F,A) -> P!{self(),capture(F,A)}.

cases() ->
    [{behaviour_info,[callbacks]},{behaviour_info,[optional_callbacks]},{behaviour_info,[other]},
     {reqids_new,[]},{reqids_size,[#{}]},{reqids_to_list,[#{}]},
     {check_response,[hello,#{} ,true]},{wait_response,[#{},0,true]},
     {receive_response,[#{},0,true]},{reply,[[]]}] ++
    [{F,A} || F<-[start,start_link,start_monitor],
      A<-[[42,[],[]],[?MODULE,[],bad],[name,?MODULE,[],[]],[{},42,[],[]]]] ++
    [{F,[V]} || F<-[reqids_size,reqids_to_list],V<-[bad,[],42,{x},#{a=>b}]] ++
    [{F,[bad,0]} || F<-[wait_response,receive_response]] ++
    [{F,[bad,0,true]} || F<-[wait_response,receive_response]] ++
    [{reqids_add,[bad,label,#{}]},{check_response,[bad,bad]},{reply,[bad]},
     {call,[bad,request,{unknown,10}]}] ++
    [{format_log,[report(),O]} || O<-[#{},#{single_line=>true},#{depth=>3},
                                   #{single_line=>true,depth=>3},#{encoding=>latin1},#{chars_limit=>100}]] ++
    [{format_log,[report()]}] ++
    [{cast,[V,event]} || V<-[bad_name,42,[],{}, {a,42},{via,42,name},{via,missing_registry,name},{global,missing_name}]] ++
    [{enter_loop,A} || A<-[[42,[],idle,[]],[?MODULE,[],idle,[]],[?MODULE,[],idle,[],[]],[?MODULE,[],idle,[],self(),[]]]] ++
    [{F,A} || {F,A}<-[{system_get_state,[bad]},{system_get_state,[{p,bad}]},
        {system_get_state,[{p,{state,hello,[],#{},[],false}}]},
        {system_replace_state,[bad,{p,{state,hello,[],#{},[],false}}]},
        {system_replace_state,[fun identity/1,{p,{state,hello,[],#{},[],false}}]}]] ++
    [{format_log,A} || A<-[[bad],[#{}],[bad,#{}],[#{},#{}],[report(),bad],
                          [report(),#{single_line=>true,depth=>bad}]]] ++
    [{call,[bad,request,T]} || T<-[-1,bad,{dirty_timeout,-1},{clean_timeout,bad}]].

report() -> #{label=>{gen_statem,terminate},name=>machine,queue=>[{cast,ping},{internal,next}],
              postponed=>[{cast,later}],modules=>[?MODULE],callback_mode=>handle_event_function,
              state_enter=>true,state=>{idle,[one,two,three]},timeouts=>{1,[{state_timeout,expired}]},
              log=>[first,second],reason=>{exit,broken,[]},client_info=>undefined,process_label=>label}.

start() -> start(handle_event_function,[]).
start(Mode,Actions) -> {ok,P}=gen_statem:start(?MODULE,{Mode,idle,[],Actions},[]),P.
get(P) -> gen_statem:call(P,get,1000).
done(P) -> V=get(P),ok=gen_statem:stop(P),V.
cast(P,C) -> gen_statem:cast(P,C).
actions(P,A) -> cast(P,{actions,A}).
identity(X) -> X.

scenario(basic) -> P=start(),cast(P,one),P!two,done(P);
scenario(state_functions) -> P=start(state_functions,[]),cast(P,one),cast(P,{change,active,[]}),done(P);
scenario(state_enter) -> P=start([handle_event_function,state_enter],[]),cast(P,{change,active,[]}),done(P);
scenario(postpone) -> P=start(),cast(P,{later,one}),cast(P,{later,two}),cast(P,{change,active,[]}),done(P);
scenario(next_event) -> P=start(),actions(P,[{next_event,internal,first},{next_event,internal,second}]),cast(P,external),done(P);
scenario(zero_order) ->
    P=start(handle_event_function,[{{timeout,a},0,a},{{timeout,b},0,b},{next_event,internal,inserted}]),
    done(P);
scenario(timeout_update) ->
    P=start(handle_event_function,[{{timeout,a},update,updated},{{timeout,a},0,ignored}]),
    done(P);
scenario(timeout_cancel) -> P=start(),actions(P,[{{timeout,a},0,wrong},{{timeout,a},cancel}]),done(P);
scenario(state_timeout) ->
    P=start(handle_event_function,[{state_timeout,0,wrong},{next_event,internal,{change,active,[]}}]),done(P);
scenario(event_timeout) ->
    P=start(handle_event_function,[{timeout,0,wrong},{next_event,internal,first}]),done(P);
scenario(repeat) -> P=start([handle_event_function,state_enter],[]),cast(P,repeat),done(P);
scenario(requests) ->
    P=start(),R=gen_statem:send_request(P,get),{reply,V}=gen_statem:receive_response(R,1000),
    ok=gen_statem:stop(P),V;
scenario(collections) ->
    P=start(),C=gen_statem:send_request(P,get,label,gen_statem:reqids_new()),
    {V,label,C1}=gen_statem:receive_response(C,1000,true),ok=gen_statem:stop(P),{V,gen_statem:reqids_size(C1)};
scenario(system) ->
    P=start(),{idle,[]}=sys:get_state(P),ok=sys:suspend(P),
    {active,[replaced]}=sys:replace_state(P,fun(_)->{active,[replaced]} end),
    ok=sys:change_code(P,?MODULE,old,extra),ok=sys:resume(P),done(P);
scenario(callback_stack) -> P=start(),actions(P,[{push_callback_module,?MODULE},pop_callback_module]),done(P);
scenario(monitor) ->
    {ok,{P,M}}=gen_statem:start_monitor(?MODULE,{handle_event_function,idle,[],[]},[]),
    ok=gen_statem:stop(P),receive {'DOWN',M,process,P,normal}->{is_pid(P),is_reference(M)} after 1000->missing end;
scenario(named) ->
    {ok,P}=gen_statem:start({local,statem_port_test},?MODULE,{handle_event_function,idle,[],[]},[]),
    {error,{already_started,P}}=gen_statem:start({local,statem_port_test},?MODULE,[],[]),
    cast(statem_port_test,named),done(P);
scenario(init_results) ->
    [gen_statem:start(?MODULE,A,[]) || A<-[ignore,{stop,normal},{error,declined},{throw,ignore}]];
scenario(throws) ->
    P=start(),cast(P,{raw,{throw,{keep_state,[thrown]}}}),done(P);
scenario(hibernate) -> P=start(handle_event_function,[hibernate]),actions(P,[hibernate,{next_event,internal,wakeup}]),done(P);
scenario(stop_reply) ->
    P=start(),gen_statem:call(P,stop_reply,1000);
scenario(enter_loop) ->
    {ok,P}=proc_lib:start(?MODULE,enter_child,[self()]),done(P);
scenario(real_timer) ->
    P=start(handle_event_function,[{{timeout,real},1,{notify,self(),expired}}]),
    receive {timer,expired}->done(P) after 1000->exit(timer_missing) end;
scenario(absolute_timer) ->
    Now=erlang:monotonic_time(millisecond),
    P=start(handle_event_function,[{{timeout,absolute},Now-1,{notify,self(),expired},[{abs,true}]}]),
    receive {timer,expired}->done(P) after 1000->exit(timer_missing) end;
scenario(format_status) ->
    P=start(),{status,P,_,[_,running,_,_,Status]}=sys:get_status(P),
    [{header,_},{data,Data},{data,[{"State",{idle,[]}}]}]=Status,
    V=[K || {K,_}<-Data],ok=gen_statem:stop(P),V;
scenario({bad_return,V}) -> crash(handle_event_function,[],{raw,V});
scenario({bad_action,A}) -> crash(handle_event_function,[],{actions,A});
scenario({bad_mode,M}) -> crash(M,[],none);
scenario({enter_action,A}) -> crash([handle_event_function,state_enter],[],{set_enter,A});
scenario(cross_module) ->
    P=start(),actions(P,{push_callback_module,gen_statem_port_second}),cast(P,{change,active,[]}),
    actions(P,pop_callback_module),cast(P,restored),done(P);
scenario(sys_terminate_once) ->
    P=start(),cast(P,{termination_owner,self()}),_=sys:get_state(P),_=sys:get_state(P),
    ok=gen_statem:stop(P),{take_termination(),take_extra_termination()};
scenario(sys_error_once) ->
    P=start(),M=monitor(process,P),cast(P,{termination_owner,self()}),
    ok=sys:suspend(P),ok=sys:resume(P),cast(P,{raise,error}),
    R=receive {'DOWN',M,process,P,Why}->Why after 1000->exit(down_missing) end,
    {R,take_termination(),take_extra_termination()};
scenario({callback_raise,C}) -> crash(handle_event_function,[],{raise,C});
scenario({sequence,M,Events}) ->
    P=start(M,[]),[cast(P,E) || E<-Events],done(P).

sequence_events() ->
    [one,{change,active,[]},{change,idle,[]},{later,x},repeat,
     {actions,[{next_event,internal,n}]},
     {actions,[{{timeout,g},0,t},{{timeout,h},update,u}]},
     {actions,[{timeout,0,t},{next_event,internal,cancel}]}].

take_termination() -> receive {terminated,R,S,D}->{R,S,D} after 1000->missing end.
take_extra_termination() -> receive {terminated,_,_,_}->duplicate after 0->once end.

crash(Mode,Actions,Message) ->
    {ok,{P,M}}=gen_statem:start_monitor(?MODULE,{Mode,idle,[],Actions},[]),
    case Message of none->ok; _->cast(P,Message) end,
    receive {'DOWN',M,process,P,R}->R after 1000->exit(crash_missing) end.

enter_child(Parent) ->
    put(mode,handle_event_function),proc_lib:init_ack(Parent,{ok,self()}),
    gen_statem:enter_loop(?MODULE,[],idle,[]).

init(ignore) -> ignore;
init({stop,R}) -> {stop,R};
init({error,R}) -> {error,R};
init({throw,R}) -> throw(R);
init({Mode,State,Data,Actions}) -> put(mode,Mode),{ok,State,Data,Actions}.
callback_mode() -> erlang:get(mode).
idle(T,C,D) -> handle_event(T,C,idle,D).
active(T,C,D) -> handle_event(T,C,active,D).
handle_event({call,F},get,S,D) -> {keep_state_and_data,[{reply,F,{S,D}}]};
handle_event({call,F},stop_reply,_S,_D) -> {stop_and_reply,normal,{reply,F,stopped}};
handle_event({call,F},{wait_for,N},_S,D) when length(D)>=N -> {keep_state_and_data,{reply,F,ok}};
handle_event({call,_},{wait_for,_},_S,_D) -> {keep_state_and_data,postpone};
handle_event(enter,_Old,_S,{enter_action,A,D}) -> {keep_state,D,A};
handle_event(enter,Old,S,D) -> {keep_state,D++[{enter,Old,S}]};
handle_event(_,{set_enter,A},_S,D) -> {repeat_state,{enter_action,A,D}};
handle_event(_,{termination_owner,P},_,_) -> put(termination_owner,P),keep_state_and_data;
handle_event(T,{notify,P,C},_S,D) -> P!{timer,C},{keep_state,D++[{T,C}]};
handle_event(_,repeat,_S,D) -> {repeat_state,D};
handle_event(_,{raw,{throw,V}},_,_) -> throw(V);
handle_event(_,{raise,C},_,_) -> erlang:C(raised);
handle_event(_,{raw,V},_,_) -> V;
handle_event(_,{actions,A},_,_) -> {keep_state_and_data,A};
handle_event(_,{change,S,A},_,D) -> {next_state,S,D,A};
handle_event(_,{later,_},idle,_) -> {keep_state_and_data,postpone};
handle_event(T,C,_S,D) -> {keep_state,D++[{T,C}]}.
terminate(R,S,D) -> case erlang:get(termination_owner) of undefined->ok; P->P!{terminated,R,S,D},ok end.
code_change(old,S,D,extra) -> {ok,S,D++[upgraded]}.
