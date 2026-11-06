-module(gen_server_port_cases).
-export([write/1,observe/2,capture/2,worker/3,scenario/1,init/1,handle_call/3,handle_cast/2,handle_info/2,handle_continue/2,terminate/2,code_change/3,identity/1,enter_child/1]).
write(Dir)->
    _=erl_features:keywords(),
    Features=[{{erl_features,K},persistent_term:get({erl_features,K})} || K<-[feature_specs,enabled_features,keywords,init_done]],
    ok=file:write_file(filename:join(Dir,"feature-bootstrap.etf"),term_to_binary(Features)),
    ok=file:write_file(filename:join(Dir,"exports.etf"),term_to_binary(gen_server:module_info(exports)--[{module_info,0},{module_info,1}])),
    ok=file:write_file(filename:join(Dir,"original.etf"),term_to_binary([{F,A,observe(F,A)} || {F,A}<-cases()])),
    Names=[basic,requests,collections,system,named,monitor,init_results,continue,stop_reply,throws,callback_stack,hibernate,enter_loop,sys_terminate_once,sys_error_once,multi_call,abcast,optional,status,sys_log,init_error_stack,terminate_error_stack,call_error_stack,info_error_stack,continue_error_stack,wait_request,check_request,collection_wait,collection_check,via_name,remote_multi,parent_exit] ++ [{upstream,N} || N<-[calling_self,get_state,replace_state,reply_by_alias_with_payload]] ++
          [{action,A} || A<-[0,infinity,{continue,next},hibernate,{timeout,0,tick},{hibernate,0,tick},{timeout,1,tick},{hibernate,1,tick},{timeout,infinity,tick},{timeout,0,tick,{abs,true}},{timeout,2,tick,[{abs,false}]}]] ++
          [{bad_return,V} || V<-[bad,{},[],{reply,one},{stop},{reply,x,y,bad},{noreply,y,bad},{stop,x,y,z,extra}]] ++
          [{bad_action,A} || A<-[bad,-1,{timeout,-1,tick},{timeout,0,tick,bad},{timeout,1,tick,[{abs,bad}]},{hibernate,1,tick,[{abs,true},bad]}]] ++
          [{init_action,A} || A<-[0,infinity,hibernate,{continue,next},{timeout,0,tick},{hibernate,0,tick}]] ++
          [{callback_raise,C} || C<-[error,exit,throw]],
    ok=file:write_file(filename:join(Dir,"effects.etf"),term_to_binary([{N,scenario(N)} || N<-Names])),ok.
observe(F,A)-> {P,M}=spawn_monitor(?MODULE,worker,[self(),F,A]),receive {P,V}->demonitor(M,[flush]),V;{'DOWN',M,process,P,R}->{worker_failed,R} after 5000->exit(observer_timeout) end.
worker(P,F,A)->P!{self(),capture(F,A)}.
capture(F,A)->try apply(gen_server,F,A) of V->{ok,V} catch C:R:S->{C,R,S} end.
identity(X)->X.
cases()->
 [{behaviour_info,[callbacks]},{behaviour_info,[optional_callbacks]},{behaviour_info,[other]},
  {call,[{via,gen_server_port_second,{throw,broken}},request,0]},{call,[{via,gen_server_port_second,{throw,{ok,accepted}}},request,0]},{call,[{via,gen_server_port_second,{throw,{'EXIT',broken}}},request,0]},
  {reqids_new,[]},{reqids_size,[#{}]},{reqids_to_list,[#{}]},
  {check_response,[hello,#{},true]},{wait_response,[#{},0,true]},{receive_response,[#{},0,true]}] ++
 [{F,A} || F<-[start,start_link,start_monitor],A<-[[42,[],[]],[?MODULE,[],[bad|tail]],[?MODULE,[],bad],[name,?MODULE,[],[]],[{},42,[],[]]]] ++
 [{F,[V]} || F<-[reqids_size,reqids_to_list],V<-[bad,[],42,{x},#{a=>b}]] ++
 [{F,[bad,0]} || F<-[wait_response,receive_response]] ++ [{F,[bad,0,true]} || F<-[wait_response,receive_response]] ++
 [{reqids_add,[bad,label,#{}]},{check_response,[bad,bad]},{reply,[bad,x]},
  {system_continue,[parent,[],bad]},{system_continue,[parent,[],[bad,state,infinity,[]]]},{system_terminate,[normal,parent,[],bad]},{system_terminate,[normal,parent,[],[bad,state,[],infinity]]},{system_code_change,[bad,module,old,extra]},{system_get_state,[bad]},{system_get_state,[[bad,hello,one,two]]},
  {system_replace_state,[fun identity/1,[bad,hello,one,two]]},
  {system_replace_state,[bad,[bad,hello,one,two]]},{system_replace_state,[fun()->zero end,[bad,hello,one,two]]},
  {format_status,[normal,bad]}] ++
 [{cast,[V,event]} || V<-[bad_name,42,[],{},{a,42},{via,42,name},{via,missing_registry,name},{global,missing_name}]] ++
 [{enter_loop,A} || A<-[[42,[],idle],[?MODULE,bad,idle],[?MODULE,[],idle],[?MODULE,[],idle,bad],[?MODULE,[],idle,self(),infinity]]] ++
 [{multi_call,A} || A<-[[42,x],[bad,name,x],[[bad,42],name,x,0],[[one|tail],name,x,0],[[],name,x],[[],name,x,infinity],[[],name,x,-1]]] ++
 [{abcast,A} || A<-[[42,x],[[],name,x],[[42],name,x],[[one|tail],name,x]]] ++
 [{call,[missing_server,request,T]} || T<-[0,-1,bad,infinity]] ++
 [{format_log,[R,O]} || R<-[report(),#{label=>{gen_server,no_handle_info},module=>?MODULE,message=>[a,b,c]}],O<-[#{},#{single_line=>true},#{depth=>3},#{single_line=>true,depth=>3},#{encoding=>latin1},#{chars_limit=>100},#{single_line=>bad},#{depth=>bad},#{single_line=>true,depth=>0}]] ++
 [{format_log,[report()]}] ++ [{format_log,A} || A<-[[bad],[#{}],[bad,#{}],[#{},#{}],[report(),bad]]].
report()->#{label=>{gen_server,terminate},name=>server,last_message=>ping,state=>{idle,[one,two,three]},log=>[first,second],reason=>broken,client_info=>undefined,process_label=>label}.
start(S)->{ok,P}=gen_server:start(?MODULE,S,[]),P.
stop(P)->gen_server:stop(P).
get(P)->gen_server:call(P,get).
recv()->receive X->X after 1000->timeout end.
scenario_core({upstream,N})->apply(gen_server_SUITE,N,[[]]),ok;
scenario_core(status)->{ok,P}=gen_server:start({local,gen_server_port_status},?MODULE,state,[]),{status,P,_,Status}=sys:get_status(P),stop(P),normalize(normalize(lists:last(Status),self()),P,server);
scenario_core(sys_log)->P=start(zero),ok=sys:log(P,true),ok=gen_server:cast(P,{set,one}),one=sys:get_state(P),Log=sys:log(P,get),stop(P),Log;
scenario_core(init_error_stack)->gen_server:start(?MODULE,{raise,error,broken},[]);
scenario_core(terminate_error_stack)->P=start({terminate_raise,error,broken}),M=monitor(process,P),_=catch stop(P),receive {'DOWN',M,process,P,R}->R after 1000->timeout end;
scenario_core(call_error_stack)->P=start(state),M=monitor(process,P),_=catch gen_server:call(P,{raise,error,broken}),receive {'DOWN',M,process,P,R}->R after 1000->timeout end;
scenario_core(info_error_stack)->P=start(state),M=monitor(process,P),P!{raise,error,broken},receive {'DOWN',M,process,P,R}->R after 1000->timeout end;
scenario_core(continue_error_stack)->P=start(state),M=monitor(process,P),gen_server:cast(P,{action,{continue,{raise,error,broken}}}),receive {'DOWN',M,process,P,R}->R after 1000->timeout end;
scenario_core(wait_request)->P=start(state),R=gen_server:send_request(P,get),V=gen_server:wait_response(R,1000),stop(P),V;
scenario_core(check_request)->P=start(state),R=gen_server:send_request(P,get),V=gen_server:check_response(recv(),R),stop(P),V;
scenario_core(collection_wait)->P=start(state),C=gen_server:send_request(P,get,label,gen_server:reqids_new()),{{reply,V},L,C1}=gen_server:wait_response(C,1000,false),stop(P),{V,L,gen_server:reqids_size(C1),[Label || {_R,Label}<-gen_server:reqids_to_list(C1)]};
scenario_core(collection_check)->P=start(state),C=gen_server:send_request(P,get,label,gen_server:reqids_new()),{{reply,V},L,C1}=gen_server:check_response(recv(),C,true),stop(P),{V,L,gen_server:reqids_size(C1)};
scenario_core(via_name)->N={via,gen_server_port_second,gen_server_port_via},{ok,P}=gen_server:start(N,?MODULE,zero,[]),ok=gen_server:cast(N,{set,one}),V=gen_server:call(N,get),ok=gen_server:stop(N),{V,is_process_alive(P),whereis(gen_server_port_via)};
scenario_core(remote_multi)->gen_server:multi_call(['missing@nohost'],missing_server,get,1);
scenario_core(parent_exit)->Owner=self(),{Parent,PM}=spawn_monitor(fun()->{ok,P}=gen_server:start_link(?MODULE,{trap,Owner},[]),Owner!{ready,P},receive finish->exit(parent_shutdown) end end),
    receive {ready,P}->M=monitor(process,P),Parent!finish,T=recv_terminated(),R=receive {'DOWN',M,process,P,Why}->Why after 1000->timeout end,receive {'DOWN',PM,process,Parent,_}->ok after 1000->timeout end,{T,R} after 1000->timeout end;
scenario_core(basic)->P=start(0),A=get(P),ok=gen_server:cast(P,{set,3}),B=get(P),P!{set,5},C=get(P),stop(P),{A,B,C};
scenario_core(requests)->P=start(state),R=gen_server:send_request(P,get),V=gen_server:receive_response(R,1000),stop(P),V;
scenario_core(collections)->P=start(state),C=gen_server:send_request(P,get,label,gen_server:reqids_new()),{{reply,V},L,C1}=gen_server:receive_response(C,1000,true),stop(P),{V,L,gen_server:reqids_size(C1)};
scenario_core(system)->P=start(1),A=sys:get_state(P),2=sys:replace_state(P,fun(X)->X+1 end),ok=sys:suspend(P),ok=sys:change_code(P,?MODULE,old,extra),ok=sys:resume(P),B=get(P),stop(P),{A,B};
scenario_core(named)->{ok,P}=gen_server:start({local,gen_server_port_named},?MODULE,one,[]),A=gen_server:start({local,gen_server_port_named},?MODULE,two,[]),B=gen_server:call(gen_server_port_named,get),stop(P),{element(1,A),element(1,element(2,A)),B};
scenario_core(monitor)->{ok,{P,M}}=gen_server:start_monitor(?MODULE,one,[]),stop(P),receive {'DOWN',M,process,P,R}->R after 1000->timeout end;
scenario_core(init_results)->[gen_server:start(?MODULE,{init,V},[]) || V<-[ignore,{stop,stopped},{error,failed},bad]];
scenario_core(continue)->P=start({init,{ok,zero,{continue,next}}}),A=get(P),stop(P),A;
scenario_core(stop_reply)->P=start({owner,self(),state}),M=monitor(process,P),V=gen_server:call(P,{stop_reply,bye}),T=recv(),receive {'DOWN',M,process,P,R}->{V,T,R} after 1000->timeout end;
scenario_core(throws)->P=start(state),V=gen_server:call(P,{throw,{reply,thrown,state}}),stop(P),V;
scenario_core(callback_stack)->P=start(state),M=monitor(process,P),gen_server:cast(P,{raise,error,broken}),receive {'DOWN',M,process,P,R}->R after 1000->timeout end;
scenario_core(hibernate)->P=start(state),gen_server:cast(P,{action,hibernate}),A=get(P),stop(P),A;
scenario_core(enter_loop)->P=proc_lib:spawn(?MODULE,enter_child,[self()]),receive {ready,P}->A=get(P),stop(P),A after 1000->timeout end;
scenario_core(sys_terminate_once)->P=start({owner,self(),state}),ok=sys:terminate(P,normal),{recv(),receive X->X after 0->empty end};
scenario_core(sys_error_once)->P=start({owner,self(),state}),ok=sys:suspend(P),ok=sys:resume(P),gen_server:cast(P,{raise,exit,broken}),{recv(),receive X->X after 0->empty end};
scenario_core(multi_call)->{ok,P}=gen_server:start({local,gen_server_port_multi},?MODULE,state,[]),{Rs,Bad}=gen_server:multi_call([node()],gen_server_port_multi,get,1000),stop(P),{[V || {_,V}<-Rs],Bad};
scenario_core(abcast)->{ok,P}=gen_server:start({local,gen_server_port_multi},?MODULE,zero,[]),abcast=gen_server:abcast([node()],gen_server_port_multi,{set,one}),V=get(P),stop(P),V;
scenario_core(optional)->{ok,P}=gen_server:start(gen_server_port_second,state,[]),P!hello,V=get(P),stop(P),V;
scenario_core({action,A})->P=start({owner,self(),state}),gen_server:cast(P,{action,A}),
    T=case A of {_,N,_} when is_integer(N)->recv(); {_,N,_,_} when is_integer(N)->recv(); _->none end,
    V=get(P),stop(P),{V,T,flush()};
scenario_core({bad_action,A})->P=start({owner,self(),state}),M=monitor(process,P),gen_server:cast(P,{action,A}),T=recv(),normalize(receive {'DOWN',M,process,P,R}->{T,R} after 1000->timeout end,self());
scenario_core({bad_return,V})->P=start({owner,self(),state}),M=monitor(process,P),gen_server:cast(P,{return,V}),T=recv(),receive {'DOWN',M,process,P,R}->{T,R} after 1000->timeout end;
scenario_core({init_action,A})->P=start({init,{ok,state,A}}),V=get(P),stop(P),V;
scenario_core({callback_raise,C})->P=start(state),M=monitor(process,P),gen_server:cast(P,{raise,C,broken}),receive {'DOWN',M,process,P,R}->R after 1000->timeout end.
flush()->receive X->[X|flush()] after 0->[] end.
enter_child(Owner)->Owner!{ready,self()},gen_server:enter_loop(?MODULE,[],entered).
init({trap,Owner})->process_flag(trap_exit,true),{ok,{owner,Owner,state}};
init({raise,C,R})->erlang:C(R);
init({init,V})->V;
init(S)->{ok,S}.
handle_call({raise,C,R},_,_)->erlang:C(R);
handle_call(get,_,{owner,_,V}=S)->{reply,V,S};
handle_call(get,_,S)->{reply,S,S};
handle_call({throw,V},_,_)->throw(V);
handle_call({stop_reply,V},_,S)->{stop,normal,V,S}.
handle_cast({set,N},_)->{noreply,N};
handle_cast({action,A},S)->{noreply,S,A};
handle_cast({return,V},_)->V;
handle_cast({raise,C,R},_)->erlang:C(R).
handle_info({raise,C,R},_)->erlang:C(R);
handle_info({set,N},_)->{noreply,N};
handle_info(tick,{owner,P,S})->P!tick,{noreply,{owner,P,S}};
handle_info(_I,S)->{noreply,S}.
handle_continue({raise,C,R},_)->erlang:C(R);
handle_continue(next,_)->{noreply,continued}.
terminate(_,{terminate_raise,C,R})->erlang:C(R);
terminate(R,{owner,P,_})->P!{terminated,R},ok;
terminate(_,_)->ok.
code_change(_,S,_)->{ok,{changed,S}}.

normalize(T,Owner) when is_pid(T), T=:=Owner -> owner;
normalize(T,Owner) when is_tuple(T) -> list_to_tuple([normalize(X,Owner) || X<-tuple_to_list(T)]);
normalize([H|T],Owner)->[normalize(H,Owner)|normalize(T,Owner)];
normalize(T,_)->T.

scenario(N)->
    Owner=self(),{P,M}=spawn_monitor(fun()->V=scenario_core(N),Owner!{self(),V},
        [exit(L,kill) || L<-element(2,process_info(self(),links))] end),
    receive {P,V}->demonitor(M,[flush]),V; {'DOWN',M,process,P,R}->{effect_failed,R} after 5000->exit(effect_timeout) end.
normalize(T,Identity,Marker) when is_pid(T),T=:=Identity->Marker;
normalize(T,Identity,Marker) when is_tuple(T)->list_to_tuple([normalize(X,Identity,Marker) || X<-tuple_to_list(T)]);
normalize([H|T],Identity,Marker)->[normalize(H,Identity,Marker)|normalize(T,Identity,Marker)];
normalize(T,_,_)->T.

recv_terminated()->receive {terminated,_}=T->T after 1000->timeout end.
