-module(erpc_port_cases).
-export([write/1,capture/2,worker/3,scenario/1,scenario_worker/2,value/1,fail/2,sleep_value/2]).

write(Directory) ->
    Rows=[{F,A,observe(F,A)} || {F,A}<-cases()],
    ok=file:write_file(filename:join(Directory,"original.etf"),term_to_binary(Rows)),
    ok=file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(erpc:module_info(exports)--[{module_info,0},{module_info,1}])),
    Effects=[{N,observe_scenario(N)} || N<-[requests,collections,collection_errors,timeout,wait_timeout,check,cast,multicall,multicast,internal,trim,funs,invalid_collection,duplicate,spoof,collection_timeout,invalid_multicall,response_errors]],
    ok=file:write_file(filename:join(Directory,"effects.etf"),term_to_binary(Effects)),ok.

worker(F,A,P) -> P!{self(),capture(F,A)}.
observe(F,A) -> P=spawn(?MODULE,worker,[F,A,self()]),receive {P,V}->V after 10000->error({observation_timeout,F,A}) end.
scenario_worker(N,P) -> P!{self(),capture_scenario(N)}.
observe_scenario(N) -> P=spawn(?MODULE,scenario_worker,[N,self()]),receive {P,V}->V after 10000->error({scenario_timeout,N}) end.
capture(F,A) ->
    try apply(erpc,F,A) of V -> {ok,project(V)} catch C:R:S -> {C,project(R),project(S)} end.
capture_scenario(N) ->
    try scenario(N) of V -> {ok,project(V)} catch C:R:S -> {C,project(R),project(S)} end.
project(V) when is_reference(V)->reference;
project(V) when is_pid(V)->pid;
project(V) when is_function(V)->function;
project(V) when is_map(V)->maps:from_list([{project(K),project(X)} || {K,X}<-maps:to_list(V)]);
project(V) when is_tuple(V)->list_to_tuple([project(X)||X<-tuple_to_list(V)]);
project([H|T])->[project(H)|project(T)];
project(V)->V.

cases() ->
    Bad=[bad,42,[],{},<<>>,1.0],
    Times=[-1,1.0,bad,[],{},16#100000000,{abs,bad},#{timeout=>bad},#{always_spawn=>bad}],
    Targets=[{bad,abs,[-2]},{erlang,abs,[-2]},{42,abs,[]},{erlang,42,[]},{erlang,abs,bad},{erlang,abs,[x|bad]}],
    Calls=[{call,[nonode@nohost,M,F,A,T]} || {M,F,A}<-Targets,T<-[infinity,1000,#{always_spawn=>true},#{},#{timeout=>infinity,always_spawn=>false}]],
    Invalid=[{F,[V,bad]} || F<-[call,cast,send_request,multicast,multicall],V<-Bad] ++
            [{F,[nonode@nohost,erlang,abs,[-2],T]} || F<-[call,multicall],T<-Times] ++
            [{F,[V]} || F<-[receive_response,wait_response,reqids_size,reqids_to_list],V<-Bad] ++
            [{F,[V,T]} || F<-[receive_response,wait_response],V<-Bad,T<-[infinity,0,bad]] ++
            [{check_response,[message,V]} || V<-Bad] ++
            [{F,[#{},T,D]} || F<-[receive_response,wait_response],T<-[0,infinity,bad,-1],D<-[true,false,bad]] ++
            [{check_response,[message,#{},D]} || D<-[true,false,bad]] ++
            [{F,[N,erlang,abs,[-1]]} || F<-[cast,send_request,multicast],N<-[42,[],[nonode@nohost|bad]]] ++
            [{reqids_add,[V,label,#{a=>b}]} || V<-Bad] ++
            [{reqids_to_list,[M]} || M<-[#{a=>b},#{a=>[b|c]},#{a=>[]},#{}]],
    Internal=[{is_arg_error,[R,m,f,A]} || R<-[badarg,system_limit],A<-[[],[x|bad],bad,lists:duplicate(256,x)]] ++
             [{trim_stack,[S,m,f,A]} || S<-[[],[{x,y,0,[]}],[{erpc,execute_call,3,[]}],[{m,f,[],[]},{erpc,execute_cast,3,[]}]],A<-[[],[x],bad]],
    MoreTargets=[{erlang,error,[reason]},{erlang,exit,[reason]},{erlang,throw,[reason]},
                 {?MODULE,fail,[error,reason]},{?MODULE,fail,[exit,reason]},{?MODULE,fail,[throw,reason]},
                 {erlang,abs,[bad]}, {erlang,abs,[]}, {erlang,abs,lists:duplicate(256,x)}],
    More=[{call,[node(),M,F,A,T]}||{M,F,A}<-MoreTargets,T<-[infinity,1000,#{always_spawn=>true}]] ++
         [{execute_call,[M,F,A]}||{M,F,A}<-MoreTargets] ++
         [{execute_call,[token,M,F,A]}||{M,F,A}<-MoreTargets] ++
         [{execute_cast,[M,F,A]}||{M,F,A}<-MoreTargets] ++
         [{call_result,[Type,request,result,R]}||Type<-[down,spawn_reply,bad],R<-[normal,noconnection,{result,return,value},{result,throw,value},{result,exit,value},{result,error,value,[]},{result,error,{erpc,system_limit}}]] ++
         [{trim_stack,[S,m,f,A]}||S<-[bad,42,[x|bad],[{erpc,execute_call}|bad],[{x,y,1,[]},{erpc,execute_cast}|bad]],A<-[[],bad]] ++
         [{call,[node(),Fun,T]}||Fun<-[fun()->value end,fun value/1,bad],T<-[infinity,#{always_spawn=>true},bad]] ++
         [{call,[node(),M,F,A,{abs,T}]}||{M,F,A}<-lists:sublist(Targets,2),T<-[0,1 bsl 100]],
    Calls++Invalid++Internal++More++[{reqids_new,[]}].

value(V)->V.
fail(throw,R)->throw(R);
fail(exit,R)->exit(R);
fail(error,R)->error(R).
sleep_value(T,V)->receive after T->V end.

scenario(response_errors)->
    R=make_ref(),Q=make_ref(),C=#{Q=>[R|label]},
    Checks=[capture(check_response,[{'DOWN',Q,process,self(),Reason},C,Delete]) ||
            Reason<-[normal,noconnection,{R,return,value},{R,throw,reason},{R,exit,reason},{R,error,reason,[]}],Delete<-[true,false]],
    Checks++[capture(check_response,[{spawn_reply,Q,error,notsup},C,true]),
             capture(check_response,[{'DOWN',Q,process,self(),normal},#{Q=>bad},false]),
             capture(receive_response,[#{Q=>bad},0,true]),
             capture(call_result,[timeout,Q,R,ignored])];
scenario(requests)->
    Rs=[erpc:send_request(node(),?MODULE,value,[X])||X<-lists:seq(1,8)],
    [erpc:receive_response(R,1000)||R<-Rs];
scenario(collections)->
    C0=erpc:reqids_new(),R1=erpc:send_request(node(),?MODULE,value,[first]),
    C1=erpc:reqids_add(R1,one,C0), C2=erpc:send_request(node(),?MODULE,value,[second],two,C1),
    L=lists:sort([{is_reference(hd(R)),is_reference(tl(R)),Label}||{R,Label}<-erpc:reqids_to_list(C2)]),
    {A,LA,C3}=erpc:receive_response(C2,1000,true),
    {{response,B},LB,C4}=erpc:wait_response(C3,1000,true),
    {erpc:reqids_size(C2),L,lists:sort([{A,LA},{B,LB}]),erpc:reqids_size(C4),erpc:wait_response(C4,0,true),erpc:check_response(foo,C4,false)};
scenario(collection_errors)->
    C=erpc:send_request(node(),?MODULE,fail,[throw,reason],label,#{}),
    try erpc:receive_response(C,1000,true) catch throw:{R,L,NC}->{R,L,map_size(NC)} end;
scenario(timeout)->
    R=erpc:send_request(node(),?MODULE,sleep_value,[100,late]),
    A=try erpc:receive_response(R,0) catch C:E->{C,E} end,
    receive after 150->ok end,
    {A,erpc:wait_response(R,0),process_info(self(),message_queue_len)};
scenario(wait_timeout)->
    R=erpc:send_request(node(),?MODULE,sleep_value,[20,late]),
    {erpc:wait_response(R),erpc:receive_response(R,1000)};
scenario(check)->
    R=erpc:send_request(node(),?MODULE,value,[answer]),
    A=erpc:check_response(unrelated,R),
    receive M->{A,erpc:check_response(M,R)} after 1000->error(missing_response) end;
scenario(cast)->
    P=self(),ok=erpc:cast(node(),erlang,send,[P,delivered]),
    receive delivered->ok after 1000->error(missing_cast) end;
scenario(multicall)->
    [erpc:multicall([node(),node(),missing@localhost],?MODULE,value,[answer],O)||O<-[1000,infinity,#{timeout=>1000,always_spawn=>true}]];
scenario(multicast)->
    P=self(),ok=erpc:multicast([node(),node()],erlang,send,[P,delivered]),
    receive delivered->ok after 1000->error(first) end,
    receive delivered->ok after 1000->error(second) end;
scenario(internal)->
    R=make_ref(),Q=make_ref(),
    [try erpc:call_result(down,Q,R,Reason) of V->{ok,V} catch C:E->{C,E} end || Reason<-[{R,return,yes},{R,throw,no},{R,exit,bye},{R,error,why,[]},{R,error,{erpc,system_limit}},noconnection,killed]];
scenario(funs)->
    Self=self(), F=fun()->self() end,
    [erpc:call(node(),F)=:=Self,
     erpc:call(node(),F,#{always_spawn=>true})=/=Self,
     begin R=erpc:send_request(node(),fun()->value end),erpc:receive_response(R) end,
     begin C=erpc:send_request(node(),fun()->value end,label,#{}), {V,L,NC}=erpc:receive_response(C,1000,true),{V,L,map_size(NC)} end,
     erpc:multicall([node(),node()],fun()->value end),
     erpc:multicast([],bad)];
scenario(invalid_collection)->
    Q=make_ref(),R=make_ref(),
    [try apply(erpc,F,A) of V->{ok,V} catch C:E->{C,project(E)} end || {F,A}<-[
        {reqids_to_list,[#{Q=>[R|label]}]},
        {reqids_to_list,[#{Q=>[bad|label]}]},
        {receive_response,[#{Q=>bad},0,true]},
        {check_response,[{'DOWN',Q,process,self(),normal},#{Q=>bad},true]},
        {check_response,[other,#{Q=>bad},true]},
        {wait_response,[#{Q=>bad},0,false]}]];
scenario(duplicate)->
    R=erpc:send_request(node(),?MODULE,value,[answer]), C=erpc:reqids_add(R,label,#{}),
    A=try erpc:reqids_add(R,other,C) catch X:Y->{X,Y} end,
    {A,erpc:receive_response(R,1000),map_size(C)};
scenario(spoof)->
    R=make_ref(),Q=make_ref(),C=#{Q=>[R|label]},
    [erpc:check_response({'DOWN',Q,process,self(),{R,return,value}},[R|Q]),
     erpc:check_response({'DOWN',Q,process,self(),{R,return,value}},C,true),
     erpc:check_response({'DOWN',Q,process,self(),{R,return,value}},C,false),
     erpc:check_response({spawn_reply,Q,error,notsup},[make_ref()|make_ref()])];
scenario(collection_timeout)->
    self()!unrelated,
    C=erpc:send_request(node(),?MODULE,sleep_value,[100,late],label,#{}),
    A=try erpc:receive_response(C,0,true) catch X:Y->{X,Y} end,
    receive after 150->ok end,
    receive unrelated->ok end,
    {A,process_info(self(),message_queue_len)};
scenario(invalid_multicall)->
    self()!unrelated,
    A=try erpc:multicall([node(),node()|bad],?MODULE,sleep_value,[50,late],1000) catch X:Y->{X,Y} end,
    receive after 100->ok end,
    receive unrelated->ok end,
    {A,process_info(self(),message_queue_len)};
scenario(trim)->
    [erpc:trim_stack(S,m,f,A)||S<-[[],[{x,y,1,[]}],[{x,y,1,[]},{erpc,execute_call,4,[]}],[{m,f,1,[]},{erpc,execute_cast,3,[]}]],A<-[[],[a],[a,b]]].
