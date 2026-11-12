-module(rpc_port_cases).
-export([write/1,worker/3,scenario_worker/2,identity/1,fail/1,send/2,delay/1,scenario/1,stdout/1]).

write(Dir) ->
    OriginalRex=whereis(rex),unregister(rex),{ok,_}=rpc:start(),
    Rows=[{F,A,observe(F,A)} || {F,A}<-cases()],
    ok=file:write_file(filename:join(Dir,"original.etf"),term_to_binary(Rows)),
    ok=file:write_file(filename:join(Dir,"exports.etf"),term_to_binary(rpc:module_info(exports)--[{module_info,0},{module_info,1}])),
    Effects=[{N,observe_scenario(N)} || N<-[async,parallel,pmap,cast,broadcast,server,callbacks,block,features,cnode,observer,lifecycle,stdout,proxy,pinfo,multi_server,serialization,upstream_off_heap]],
    ok=file:write_file(filename:join(Dir,"effects.etf"),term_to_binary(Effects)),
    Last=whereis(rex),R=monitor(process,Last),stopped=rpc:stop(),
    receive {'DOWN',R,process,Last,_}->ok end,register(rex,OriginalRex),ok.

identity(X)->X.
fail(throw)->throw(thrown);
fail(throw_exit)->throw({'EXIT',thrown});
fail(exit)->exit(exited);
fail(error)->error(broken).
send(P,M)->P!M,ok.
delay(T)->receive after T->ok end.

capture(F,A)->try apply(rpc,F,A) of
    [R1|R2]=K when F=:=async_call,is_reference(R1),is_reference(R2),R1=/=R2 ->
        {ok,{request_id,two_distinct_references,rpc:yield(K)}};
    V->{ok,V} catch C:R:S->{C,R,stack(S)} end.
worker(F,A,Parent)->Parent!{self(),capture(F,A)}.
observe(F,A)->P=spawn(?MODULE,worker,[F,A,self()]),receive {P,V}->V after 5000->error({timeout,F,A}) end.
scenario_worker(N,Parent)->Parent!{self(),try scenario(N) of V->{ok,V} catch C:R:S->{C,R,S} end}.
observe_scenario(N)->P=spawn(?MODULE,scenario_worker,[N,self()]),receive {P,V}->V after 5000->error({timeout,N}) end.

cases()->
    Calls=[{F,[node(),M,N,A]} || F<-[call,block_call],{M,N,A}<-
        [{?MODULE,identity,[value]},{?MODULE,identity,[{'EXIT',returned}]},
         {?MODULE,fail,[throw]},{?MODULE,fail,[throw_exit]},{?MODULE,fail,[exit]},
         {?MODULE,fail,[error]},{erlang,abs,[-1]},{erlang,abs,[bad]},
         {missing,missing,[]},{?MODULE,identity,[]},{erlang,42,[]},{42,abs,[]},
         {erlang,abs,[x|bad]},{erlang,abs,bad}]],
    Timed=[{F,[Node,erlang,abs,[-1],T]} || F<-[call,block_call],
           Node<-[node(),'missing@host',42],T<-[infinity,0,1000,16#ffffffff,-1,bad,1.0,16#100000000,#{timeout=>1000}]],
    Multi=[{multicall,[Nodes,?MODULE,identity,[V],1000]} || Nodes<-[[],[node()],[node(),node()],['missing@host'],[node(),'missing@host']],V<-[value,{'EXIT',value}]],
    Bad=[{F,[42,erlang,abs,[]]} || F<-[call,cast,async_call]]++
        [{nb_yield,[bad]},{nb_yield,[bad,-1]},{yield,[bad]},
         {multicall,[[],erlang,abs,[]]},{multicall,[erlang,abs,[-1]]},
         {multicall,[[node()],erlang,abs,[-1]]},
         {multicall,[[42],erlang,abs,[],0]},
         {abcast,[[],42,msg]},{abcast,[[42,node()],42,msg]},
         {sbcast,[[],42,msg]},{multi_server_call,[[],echo,msg]},
         {multi_server_call,[[42,'missing@host'],echo,msg]},
         {server_call,['missing@host',echo,echo,msg]},
         {init,[bad]},{handle_call,[unknown,from,#{keep=>value}]},
         {handle_call,[stop,from,#{keep=>value}]},{handle_cast,[unknown,state]},
         {handle_info,[unknown,state]},{handle_info,[{'DOWN',ref,process,pid,normal},#{}]},
         {terminate,[reason,state]},{code_change,[old,state,extra]},
         {parallel_eval,[[]]},{pmap,[{erlang,abs},[],[]]},
         {pmap,[bad,[],[]]},{pmap,[{42,abs},[],[]]},
         {pinfo,[bad]},{pinfo,[bad,item]}],
    Calls++Timed++Multi++Bad++edges().

edges()->
    Invalid=[bad,42,{},<<>>,1.0,#{},[x|bad]],
    [{F,[N,erlang,abs,[]]} || F<-[call,block_call,cast,async_call],N<-Invalid]++
    [{F,[node(),M,N,A]} || F<-[call,block_call,cast,async_call],
       {M,N,A}<-[{42,abs,[]},{erlang,42,[]},{erlang,abs,bad},{erlang,abs,[x|bad]}]]++
    [{F,[K]} || F<-[yield,nb_yield],K<-Invalid]++
    [{nb_yield,[K,T]} || K<-[bad,{},[]],T<-[0,1000,infinity,-1,bad,1.0,16#100000000]]++
    [{multicall,[Ns,M,F,A,T]} || {Ns,M,F,A,T}<-
        [{[],42,abs,[],infinity},{[],erlang,42,[],infinity},{[],erlang,abs,bad,infinity},
         {[],erlang,abs,[],-1},{[node()],erlang,abs,[],bad},
         {[42],erlang,abs,[],0},{[node()|bad],erlang,abs,[],0}]]++
    [{parallel_eval,[V]} || V<-[bad,[bad],[{}],[{erlang,abs,[-1]}|bad]]]++
    [{pmap,[M,A,L]} || {M,A,L}<-[{{erlang,abs},[],bad},{{erlang,abs},[],[x|bad]},
       {{erlang,abs},bad,[x]},{{42,abs},[],[x]},{{erlang,42},[],[]}]]++
    [{abcast,[V,missing,msg]} || V<-[bad,[42|bad]]]++
    [{sbcast,[V,missing,msg]} || V<-[bad,[42|bad]]]++
    [{multi_server_call,[V,missing,msg]} || V<-[bad,[42|bad]]]++
    [{server_call,[N,M,tag,msg]} || {N,M}<-[{42,name},{node(),42}]]++
    [{handle_info,[{'DOWN',key,process,pid,R},S]} || R<-[normal,bad],S<-[bad,#{key=>bad},#{key=>{42,tag}},#{}]]++
    [{handle_call,[{block_call,erlang,abs,[-1],bad},from,#{}]},
     {handle_info,[{42,features_request},state]},
     {handle_info,[{42,{sbcast,rpc_absent,msg}},state]},
     {handle_info,[{42,{send,rpc_absent,msg}},state]},
     {eval_everywhere,[[],42,42,bad]},
     {eval_everywhere,[bad,erlang,abs,[]]},{eval_everywhere,[[42],erlang,abs,[]]},{multicall,[erlang,abs,[-1],1000]}].

scenario(async)->
    K=rpc:async_call(node(),?MODULE,identity,[answer]),
    A=rpc:yield(K),K2=rpc:async_call(node(),?MODULE,fail,[throw_exit]),
    B=rpc:nb_yield(K2,infinity),
    K3=rpc:async_call(node(),?MODULE,delay,[30]),
    C=rpc:nb_yield(K3,0),D=rpc:yield(K3),{A,B,C,D};
scenario(parallel)->rpc:parallel_eval([{erlang,abs,[-1]},{?MODULE,identity,[value]},{?MODULE,fail,[throw]}]);
scenario(pmap)->{rpc:pmap({erlang,abs},[],[-1,-2,-3]),catch rpc:pmap({?MODULE,identity},[],[a,{badrpc,b}])};
scenario(cast)->true=rpc:cast(node(),?MODULE,send,[self(),casted]),receive casted->ok after 1000->error(cast_timeout) end;
scenario(broadcast)->
    true=register(rpc_probe,self()),abcast=rpc:abcast([node()],rpc_probe,async),
    receive async->ok end,
    A=rpc:sbcast([node()],rpc_probe,sync),receive sync->ok end,
    B=rpc:sbcast([node()],rpc_absent,msg),
    abcast=rpc:eval_everywhere([node()],?MODULE,send,[self(),evaluated]),receive evaluated->ok end,
    unregister(rpc_probe),{A,B};
scenario(server)->
    P=spawn(fun()->true=register(rpc_echo,self()),receive {From,Msg}->From!{rpc_echo,node(),{reply,Msg}} end end),
    wait_name(rpc_echo),A=rpc:server_call(node(),rpc_echo,rpc_echo,hello),
    R=monitor(process,P),receive {'DOWN',R,_,_,_}->ok end,
    {A,rpc:server_call(node(),rpc_absent,rpc_absent,msg)};
scenario(callbacks)->
    S=#{keep=>value},
    A=rpc:handle_call({block_call,?MODULE,identity,[{'EXIT',raw}],group_leader()},from,S),
    B=rpc:handle_info({'DOWN',make_ref(),process,self(),bad},S),
    {A,B,rpc:handle_info({'DOWN',key,process,self(),normal},#{key=>from})};
scenario(block)->{rpc:block_call(node(),erlang,abs,[-3]),rpc:block_call(node(),?MODULE,identity,[{'EXIT',raw}],1000)};
scenario(features)->
    {noreply,state}=rpc:handle_info({self(),features_request},state),
    receive R->R end;
scenario(cnode)->
    S=#{}, {noreply,New}=rpc:handle_info({self(),{call,?MODULE,identity,[result],send_stdout_to_caller}},S),
    receive {rex,result}->ok end,
    {map_size(New),receive {'DOWN',_,_,_,normal}->normal after 1000->missing end};
scenario(observer)->
    {ok,S}=rpc:init([]),{P,M}=maps:get(nodes_observer,S),
    true=is_pid(P),true=is_reference(M),
    exit(P,kill),receive {'DOWN',M,process,P,killed}->ok end,
    {noreply,S2}=rpc:handle_info({'DOWN',M,process,P,killed},S),
    {P2,M2}=maps:get(nodes_observer,S2),exit(P2,kill),
    receive {'DOWN',M2,process,P2,killed}->ok end,
    {P=/=P2,M=/=M2};
scenario(lifecycle)->
    {error,{already_started,P}}=rpc:start(),true=is_pid(P),
    R=monitor(process,P),stopped=rpc:stop(),receive {'DOWN',R,process,P,_}->ok end,
    {ok,P2}=rpc:start_link(),unlink(P2),
    {rpc:block_call(node(),erlang,abs,[-9]),is_pid(P2)};
scenario(stdout)->
    [{Requests,stdout_case(Requests)} || Requests<-
       [[{put_chars,unicode,[16#410,16#1f600]}],
        [{put_chars,unicode,[16#110000]}],
        [{put_chars,unicode,[16#110000]},{put_chars,unicode,<<"after error">>}],
        [{requests,bad}],
        [{put_chars,latin1,<<65,255>>}],
        [{put_chars,unicode,?MODULE,identity,[<<"hello">>]}],
        [{put_chars,unicode,?MODULE,fail,[error]}],
        [{get_line,unicode,<<>>},{setopts,[]},getopts,{unknown,request}],
        [{requests,[{put_chars,unicode,<<"a">>},{put_chars,unicode,<<"b">>}]}],
        [{requests,[{getopts},{put_chars,unicode,<<"not sent">>}]}]]];
scenario(proxy)->
    Original=whereis(user),case Original of undefined->ok;_->unregister(user) end,
    true=register(user,self()),P=spawn(rpc,proxy_user_flush,[]),P!proxy_forwarded,
    A=receive proxy_forwarded->ok after 1000->error(proxy_timeout) end,
    exit(P,kill),unregister(user),case Original of undefined->ok;_->register(user,Original) end,A;
scenario(pinfo)->
    {rpc:pinfo(self(),message_queue_len),rpc:pinfo(self(),[registered_name,message_queue_len]),
     proplists:get_value(registered_name,rpc:pinfo(self()),missing)};
scenario(multi_server)->
    P=spawn(fun()->true=register(rpc_multi_echo,self()),receive {From,Msg}->From!{rpc_multi_echo,node(),Msg} end end),
    wait_name(rpc_multi_echo),R=monitor(process,P),A=rpc:multi_server_call([node(),42],rpc_multi_echo,hello),
    receive {'DOWN',R,_,_,_}->ok end,
    {A,rpc:multi_server_call([node()],rpc_absent,msg)};
scenario(upstream_off_heap)->rpc_SUITE:off_heap([]);
scenario(serialization)->
    Parent=self(),
    P=spawn(fun()->Parent!{first,rpc:block_call(node(),?MODULE,delay,[60])} end),
    R=monitor(process,P),receive after 10->ok end,
    A=rpc:block_call(node(),?MODULE,identity,[after_block]),
    B=receive {first,ok}->ok after 1000->missing end,
    receive {'DOWN',R,_,_,_}->ok end,{A,B}.

stdout(Requests)->
    [begin R=make_ref(),group_leader()!{io_request,self(),R,Q},
        receive {io_reply,R,V}->V end end || Q<-Requests].
stdout_case(Requests)->
    {noreply,S}=rpc:handle_info({self(),{call,?MODULE,stdout,[Requests],send_stdout_to_caller}},#{}),
    stdout_collect(S,[]).
stdout_collect(S,Acc)->receive
    {rex_stdout,B}->stdout_collect(S,[{stdout,B}|Acc]);
    {'DOWN',_,_,_,_}=D->{noreply,S2}=rpc:handle_info(D,S),stdout_collect(S2,Acc);
    {rex,R}->lists:reverse([{reply,caller_identity(R)}|Acc])
end.

wait_name(Name)->case whereis(Name) of undefined->receive after 1->wait_name(Name) end;_->ok end.

stack(S)->[case Frame of
    {M,F,A,I} when is_list(A)->{M,F,[function_argument(V)||V<-A],I};
    _->Frame end || Frame<-S].
function_argument(F) when is_function(F,1)->
    case {erlang:fun_info(F,module),erlang:fun_info(F,name)} of
        {{module,rpc},{name,'-eval_everywhere/4-fun-0-'}}->
            {rpc_function,[erlang:fun_info(F,K)||K<-[module,name,arity,env]]};
        _->F
    end;
function_argument(V)->V.

caller_identity(P) when is_pid(P),P=:=self()->rpc_observer_caller;
caller_identity(T) when is_tuple(T)->list_to_tuple([caller_identity(V)||V<-tuple_to_list(T)]);
caller_identity([H|T])->[caller_identity(H)|caller_identity(T)];
caller_identity(V)->V.
