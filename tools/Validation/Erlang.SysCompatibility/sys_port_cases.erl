-module(sys_port_cases).
-export([write/1,observe/1,loop/4,system_continue/3,system_terminate/4,system_get_state/1,system_replace_state/2,system_code_change/4,format_status/2,format/3,process_format/3]).

write(Dir) ->
    _=erl_features:keywords(),
    Features=[{Key,persistent_term:get({erl_features,Key})} || Key<-[init_done,enabled_features,keywords,feature_specs]],
    ok=file:write_file(filename:join(Dir,"features.etf"),term_to_binary(Features)),
    Cases = [{fun_call,C} || C<-[tail_badfun,non_tail_badfun,tail_badarity,non_tail_badarity]] ++ [{status_error,M} || M<-[status_error,status_throw,status_exit]] ++ [api_variants,bad_state_reply] ++
        [{state_error,M,F} || {M,F}<-[{bad_get,get_state},{throw_get,get_state},{error_get,get_state},{exit_get,get_state},
            {bad_replace,replace_state},{throw_replace,replace_state},{error_replace,replace_state},{exit_replace,replace_state}]] ++
        [{timeout,F,A} || {F,A}<-[{suspend,[]},{resume,[]},{get_status,[]},{get_state,[]},{replace_state,[fun(X)->X end]},
            {change_code,[?MODULE,old,extra]},{terminate,[normal]},{log,[true]},{trace,[true]},{statistics,[get]},
            {log_to_file,[false]},{no_debug,[]},{install,[{id,fun format/3,state}]},{remove,[id]}]] ++ [{file_log,Dir},{file_options,Dir}] ++ [{behaviour,B} || B<-[gen_event,gen_statem]] ++
        [{api,F,A} || {F,A}<-[{debug_options,[bad]},{debug_options,[[trace|bad]]},{get_debug,[key,bad,default]},
            {get_log,[[{log,bad}]]},{handle_debug,[[bad],fun format/3,extra,event]},
            {handle_debug,[[{trace,true}],bad,extra,event]},
            {handle_debug,[[{statistics,bad}],fun format/3,extra,{in,message}]},
            {handle_debug,[[{log,[2.5]}],fun format/3,extra,event]}]] ++ [{upstream,C} || C<-[log,stats,suspend,install,special_process]] ++ [hibernate,trace_print,parent_exit] ++ [{api,behaviour_info,[callbacks]},{api,behaviour_info,[optional_callbacks]},{api,behaviour_info,[bad]},
        {api,get_debug,[1.0,[{1,value}],default]},{api,get_debug,[key,[{key,value}],default]},
        {api,print_log,[[]]}] ++ [{options, O} || O <- [[],[trace],[log],[{log,1}],[{log,2}],[{log,100}],[log,{log,2}],[{log,2},log],[trace,statistics,log],[unknown,{log,0},{log,-1},{log,1.0}],[{install,{id,fun format/3,state}}]]] ++
        [{ring,N,Count} || N <- [1,2,3,10,16], Count <- [0,1,2,3,10,17,50]] ++
        [{callback,Mode} || Mode <- [normal,done,throw_done,throw_state,error,exit]] ++
        [{system,Mode} || Mode <- [normal,bad_get,throw_get,error_get,exit_get,bad_replace,throw_replace,error_replace,exit_replace,code_ok,code_throw_ok,code_bad,code_error,code_exit]] ++
        [{public,F,A} || {F,A} <- [{suspend,[missing]},{resume,[missing,0]},{get_state,[missing]},{replace_state,[missing,fun(X)->X end]},
            {change_code,[missing,?MODULE,v,extra]},{terminate,[missing,normal]}, {no_debug,[missing]},
            {log,[missing,true]},{trace,[missing,false,0]},{statistics,[missing,get]},{log_to_file,[missing,false]},
            {install,[missing,bad]},{remove,[missing,id]}, {suspend,[missing,bad_timeout]}]],
    Rows = [{C,observe(C)} || C <- Cases],
    ok=file:write_file(filename:join(Dir,"original.etf"),term_to_binary(Rows)),
    ok=file:write_file(filename:join(Dir,"exports.etf"),term_to_binary([E || E={F,_} <- sys:module_info(exports),F=/=module_info])),
    io:format("~p sys observations~n",[length(Rows)]),ok.

observe(Case) ->
    Parent=self(), Tag=make_ref(),
    spawn(fun() ->
        Result=try run(Case) of Value -> {ok, clean(Value)}
            catch Class:Reason:Stack -> {exception,Class,clean(Reason),clean(Stack)} end,
        Parent!{Tag,Result}
    end),
    receive {Tag,Result}->Result after 10000->error(observer_timeout) end.

run({fun_call,C})->sys_behaviour_cases:run(C);
run({status_error,Mode})->
    Parent=self(),P=spawn(fun()->loop(Parent,[],{Mode,0},false) end),
    try sys:get_status(P) after exit(P,kill) end;
run(api_variants)->
    Parent=self(),P=spawn(fun()->loop(Parent,[],{normal,0},false) end),
    try
        ok=sys:suspend(P,1000), {status,P,{module,?MODULE},[_,suspended,Parent,_,_]}=sys:get_status(P,1000),
        {normal,0}=sys:get_state(P,infinity),{normal,1}=sys:replace_state(P,fun({M,N})->{M,N+1} end,1000),
        ok=sys:change_code(P,?MODULE,old,extra,1000),ok=sys:resume(P,1000),
        ok=sys:log(P,true,1000),{ok,[]}=sys:log(P,get,1000),ok=sys:log(P,print,1000),
        ok=sys:trace(P,true,1000),ok=sys:trace(P,false,1000),
        ok=sys:statistics(P,true,1000),{ok,_}=sys:statistics(P,get,1000),ok=sys:statistics(P,false,1000),
        ok=sys:log_to_file(P,false,1000),ok=sys:install(P,{id,fun format/3,state},1000),
        ok=sys:remove(P,id,1000),ok=sys:no_debug(P,1000),ok=sys:terminate(P,normal,1000),ok
    after exit(P,kill) end;
run({timeout,F,A})->
    P=spawn(fun()->receive stop->ok end end),
    try apply(sys,F,[P|A]++[0]) after exit(P,kill) end;
run({state_error,Mode,Function})->
    Parent=self(),P=spawn(fun()->loop(Parent,[],{Mode,0},false) end),
    try case Function of get_state->sys:get_state(P);replace_state->sys:replace_state(P,fun(X)->X end) end
    after exit(P,kill) end;
run(bad_state_reply)->
    P=spawn(fun()->receive {system,From,_}->gen:reply(From,bad) end end),
    try sys:get_state(P) after exit(P,kill) end;
run({file_log,Dir})->
    Path=filename:join(Dir,"sys-log-"++integer_to_list(erlang:unique_integer([positive]))++".txt"),
    Parent=self(),P=spawn(fun()->loop(Parent,[],{normal,0},false) end),
    try
        ok=sys:log_to_file(P,Path),P!{event,{in,hello}},
        {normal,0}=sys:get_state(P),ok=sys:log_to_file(P,false),
        {ok,Bytes}=file:read_file(Path),
        ok=sys:log_to_file(P,Path),P!{event,{in,second}},
        {normal,0}=sys:get_state(P),ok=sys:no_debug(P),
        {ok,Second}=file:read_file(Path),
        {error,open_file}=sys:log_to_file(P,filename:join(Path,"bad")),
        ok=sys:terminate(P,normal),{Bytes,Second}
    after exit(P,kill),file:delete(Path) end;
run({file_options,Dir})->
    Path=filename:join(Dir,"sys-options-"++integer_to_list(erlang:unique_integer([positive]))++".txt"),
    try
        D=sys:debug_options([{log_to_file,Path}]),
        D=sys:handle_debug(D,fun process_format/3,extra,{in,hello}),
        Fd=sys:get_debug(log_to_file,D,missing),ok=file:close(Fd),
        {ok,Bytes}=file:read_file(Path),
        []=sys:debug_options([{log_to_file,filename:join(Path,"bad")}]),Bytes
    after file:delete(Path) end;
run({behaviour,B})->sys_behaviour_cases:run(B);
run({upstream,Case})->
    Sink=spawn(fun io_sink/0), Old=group_leader(), true=group_leader(Sink,self()),
    try apply(sys_SUITE,Case,[[]]) after group_leader(Old,self()),exit(Sink,kill) end;
run(trace_print)->
    Parent=self(), F=fun(Device,E,X)->Parent!{format,Device,E,X},ok end,
    D=sys:debug_options([trace,{log,2}]),
    D1=sys:handle_debug(D,F,extra,event),
    ok=sys:print_log(D1),
    [receive X={format,standard_io,event,extra}->X after 1000->timeout end || _<-lists:seq(1,2)];
run(hibernate)->
    Parent=self(), P=spawn(fun()->process_flag(trap_exit,true),loop(Parent,[],{normal,0},true) end),
    try ok=sys:suspend(P), {normal,0}=sys:get_state(P), ok=sys:resume(P), ok=sys:terminate(P,normal),ok after exit(P,kill) end;
run(parent_exit)->
    Parent=self(), {P,Ref}=spawn_monitor(fun()->process_flag(trap_exit,true),loop(Parent,[],{normal,0},false) end),
    ok=sys:suspend(P),P!{'EXIT',Parent,parent_reason},
    receive {'DOWN',Ref,process,P,R}->R after 2000->timeout end;
run({options,Options}) -> clean_debug(sys:debug_options(Options));
run({ring,N,Count}) ->
    D0=sys:debug_options([{log,N}]),
    D=lists:foldl(fun(I,Acc)->sys:handle_debug(Acc,fun format/3,{extra,I},{in,I}) end,D0,lists:seq(1,Count)),
    {sys:get_log(D),clean_debug(D),sys:get_debug(missing,D,default)};
run({callback,Mode}) ->
    F=fun(State,Event,Extra) -> case Mode of normal->{State,Event,Extra};done->done;throw_done->throw(done);throw_state->throw(next);error->error(broken);exit->exit(broken) end end,
    D=sys:debug_options([{install,{id,F,initial}},{install,{F,initial}}]),
    D1=sys:handle_debug(D,fun format/3,extra,{in,hello}),
    clean_debug(D1);
run({public,F,A}) -> apply(sys,F,A);
run({api,F,A}) -> apply(sys,F,A);
run({system,Mode}) ->
    Parent=self(), P=spawn(fun()->process_flag(trap_exit,true),loop(Parent,[],{Mode,0},false) end),
    try
        R1=catch_state(fun()->sys:get_state(P) end),
        R2=catch_state(fun()->sys:replace_state(P,fun({_M,N})->{Mode,N+1} end) end),
        ok=sys:suspend(P), ok=sys:suspend(P),
        {status,P,{module,?MODULE},[_,suspended,Parent,_,_]}=sys:get_status(P),
        Change=sys:change_code(P,?MODULE,old,extra),
        Queued=make_ref(), P!{probe,self(),Queued},
        none=receive {Queued,_}->bad after 0->none end,
        ok=sys:resume(P),
        Probe=receive {Queued,X}->X after 2000->timeout end,
        ok=sys:log(P,{true,3}),
        P!{event,{in,a}}, P!{event,{out,b,self()}}, P!{event,{in,c}}, P!{event,{in,d}},
        {ok,Log}=sys:log(P,get),
        ok=sys:trace(P,false), ok=sys:statistics(P,true),
        P!{event,{in,a}}, P!{event,{out,b,self()}}, P!{event,other},
        {ok,Stats}=sys:statistics(P,get),
        Counts=[lists:keyfind(messages_in,1,Stats),lists:keyfind(messages_out,1,Stats)],
        ok=sys:statistics(P,false),{ok,no_statistics}=sys:statistics(P,get),
        ok=sys:no_debug(P),
        Unknown=gen:call(P,system,{what,is,this}),
        RunningCode=sys:change_code(P,?MODULE,v,x),
        ok=sys:terminate(P,normal),
        {R1,R2,Change,Probe,clean(Log),Counts,Unknown,RunningCode}
    after exit(P,kill) end.

catch_state(F) -> try F() of X->{ok,X} catch C:R->{C,R} end.
loop(Parent,Debug,Misc,Hib) ->
    receive
        {system,From,Msg}->sys:handle_system_msg(Msg,From,Parent,?MODULE,Debug,Misc,Hib);
        {event,E}->D=sys:handle_debug(Debug,fun process_format/3,extra,E),loop(Parent,D,Misc,Hib);
        {probe,From,Ref}->From!{Ref,Misc},loop(Parent,Debug,Misc,Hib);
        {'EXIT',Parent,Reason}->exit(Reason)
    end.
system_continue(P,D,M)->loop(P,D,M,false).
system_terminate(R,_,_,_)->exit(R).
system_get_state({bad_get,_})->bad;
system_get_state({throw_get,_})->throw(broken);
system_get_state({error_get,_})->error(broken);
system_get_state({exit_get,_})->exit(broken);
system_get_state(M)->{ok,M}.
system_replace_state(_, {bad_replace,_})->bad;
system_replace_state(_, {throw_replace,_})->throw(broken);
system_replace_state(_, {error_replace,_})->error(broken);
system_replace_state(_, {exit_replace,_})->exit(broken);
system_replace_state(F,M)->N=F(M),{ok,N,N}.
system_code_change({code_throw_ok,N},_,_,_)->throw({ok,{code_throw_ok,N+10}});
system_code_change({code_bad,_},_,_,_)->bad;
system_code_change({code_error,_},_,_,_)->error(broken);
system_code_change({code_exit,_},_,_,_)->exit(broken);
system_code_change({Mode,N},_,_,_)->{ok,{Mode,N+10}}.
format_status(normal,[_,_,_,_,{status_error,_}])->error(broken);
format_status(normal,[_,_,_,_,{status_throw,_}])->throw(broken);
format_status(normal,[_,_,_,_,{status_exit,_}])->exit(broken);
format_status(normal,[_,_,_,_,M])->M.
format(Device,Event,Extra)->Device!{formatted,Event,Extra},ok.
clean_debug(D)->[case X of {statistics,{Time,{reductions,Reds},In,Out}} when is_tuple(Time),is_integer(Reds)->{statistics,{time,reductions,In,Out}};_->clean(X) end || X<-D].
clean(X) when is_pid(X)->pid;
clean(X) when is_reference(X)->ref;
clean(X) when is_function(X)->function;
clean(X) when is_tuple(X)->list_to_tuple([clean(I)||I<-tuple_to_list(X)]);
clean([H|T])->[clean(H)|clean(T)];
clean(X)->X.

io_sink()->
    receive
        {io_request,From,ReplyAs,_Request}->From!{io_reply,ReplyAs,ok},io_sink()
    end.

process_format(Device,Event,Extra)->io:format(Device,"~tp~n",[{Event,Extra}]).
