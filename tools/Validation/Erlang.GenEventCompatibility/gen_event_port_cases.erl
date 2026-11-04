%% Focused gen_event differential observer. Same BEAM executes on OTP and .NET.
-module(gen_event_port_cases).
-export([write/1, run/1, worker/2, cases/0, init/1, handle_event/2, handle_call/2,
         handle_info/2, terminate/2, code_change/3, format_status/1]).

api_cases() ->
    [{api,F,Args} || {F,Args} <- [
        {start,[bad]},{start,[bad,[]]}, {start_link,[bad]}, {start_link,[bad,[]]},
        {start_monitor,[bad]}, {start_monitor,[bad,[]]},
        {notify,[42,event]}, {which_handlers,[missing_manager]},
        {call,[missing_manager,handler,query]}, {call,[missing_manager,handler,query,0]},
        {send_request,[{via,42,name},handler,query]}, {send_request,[{via,42,name},handler,query,label,#{}]},
        {wait_response,[bad,0]}, {wait_response,[bad,0,true]},
        {receive_response,[bad,0]}, {receive_response,[bad,0,true]},
        {check_response,[unrelated,bad]}, {check_response,[unrelated,bad,true]},
        {reqids_size,[bad]}, {reqids_add,[bad,label,#{}]}, {reqids_to_list,[bad]},
        {system_continue,[parent,[],bad]}, {system_terminate,[reason,parent,[],bad]},
        {system_code_change,[bad,module,old,extra]}, {system_get_state,[bad]}, {system_replace_state,[bad,bad]}
    ]].

extra_cases() -> [optional_info, optional_terminate, optional_code_change, missing_init, init_crash, init_exit, call_crash, call_exit, event_crash, info_crash,
                  terminate_crash, terminate_throw, behaviour_callbacks, behaviour_optional, behaviour_invalid,
                  upstream_add, upstream_delete, upstream_swap, upstream_swap_sup,
                  upstream_notify, upstream_sync_notify, upstream_call, upstream_info,
                  upstream_get_state, upstream_replace_state].

cases() ->
    [basic, duplicates, init_error, init_throw, init_bad,
     call_bad, call_throw, call_remove, event_bad, event_remove,
     info, swap, swap_missing, swap_failure, callback_swap,
     callback_swap_hibernate, call_swap, supervised_delete,
     supervised_remove, supervised_swap, supervised_shutdown,
     supervisor_exit, get_state, replace_state, replace_bad,
     code_change, code_change_bad, status, debug_log,
     request_wait, request_receive, request_check,
     request_collection_wait, request_collection_receive,
     request_collection_check, request_error,
     local_name, monitor, linked, hibernate, auto_hibernate,
     invalid_start, invalid_swap, missing_handler,
     reqids, get_modules, format_warning, format_crash,
     format_single, format_depth, format_latin1, format_limit,
     legacy_warning, legacy_crash] ++ extra_cases() ++ api_cases().

write(Dir) ->
    logger:set_primary_config(level, emergency),
    Rows = [{C, observe(C)} || C <- cases()],
    ok = file:write_file(filename:join(Dir,"original.etf"),term_to_binary(Rows)),
    Exports = lists:sort(gen_event:module_info(exports) -- [{module_info,0},{module_info,1}]),
    ok = file:write_file(filename:join(Dir,"exports.etf"),term_to_binary(Exports)),
    io:format("~p cases~n",[length(Rows)]), ok.

observe(C) ->
    Parent = self(),
    {Pid,Ref} = spawn_monitor(fun() -> worker(C,Parent) end),
    receive
        {Pid,Result} -> receive {'DOWN',Ref,process,Pid,normal} -> Result end;
        {'DOWN',Ref,process,Pid,Why} -> {worker_died,Why}
    after 10000 -> exit(Pid,kill), timeout end.

worker(C,Parent) -> Parent ! {self(),run(C)}.

run(C) ->
    try scenario(C) of R -> {ok,R}
    catch Class:Reason:Stack -> {Class,Reason,Stack} end.

manager(F) ->
    {ok,P} = gen_event:start(),
    try F(P) after catch gen_event:stop(P) end.

add(P) -> gen_event:add_handler(P,?MODULE,0).
state(P) -> gen_event:call(P,?MODULE,get).
drain() -> receive {observed, X} -> [X|drain()] after 0 -> [] end.
exit_message() -> receive {gen_event_EXIT,H,R} -> {H,R} after 1000 -> missing_exit end.

scenario(optional_info) -> manager(fun(P)->
    ok=gen_event:add_handler(P,gen_event_no_optional,0),P!hello,
    {gen_event:call(P,gen_event_no_optional,get),gen_event:which_handlers(P)} end);
scenario(optional_terminate) -> manager(fun(P)->
    ok=gen_event:add_handler(P,gen_event_no_optional,0),gen_event:delete_handler(P,gen_event_no_optional,bye) end);
scenario(optional_code_change) -> manager(fun(P)->
    ok=gen_event:add_handler(P,gen_event_no_optional,0),ok=sys:suspend(P),
    R=sys:change_code(P,gen_event_no_optional,old,extra),ok=sys:resume(P),
    {R,gen_event:call(P,gen_event_no_optional,get)} end);
scenario(missing_init) -> manager(fun(P)-> gen_event:add_handler(P,gen_event_missing_module,[]) end);
scenario({api,F,Args}) -> apply(gen_event,F,Args);
scenario(basic) -> manager(fun(P) ->
    ok=add(P), ok=gen_event:notify(P,1), ok=gen_event:sync_notify(P,2),
    {gen_event:which_handlers(P),state(P),gen_event:delete_handler(P,?MODULE,delete),gen_event:which_handlers(P)} end);
scenario(duplicates) -> manager(fun(P) ->
    ok=add(P),ok=add(P),ok=gen_event:add_handler(P,{?MODULE,2},10),
    Before=gen_event:which_handlers(P), ok=gen_event:sync_notify(P,3),
    A=gen_event:delete_handler(P,?MODULE,delete),
    {Before,A,gen_event:which_handlers(P),state(P)} end);
scenario(C) when C=:=init_error; C=:=init_throw; C=:=init_bad -> manager(fun(P) ->
    {gen_event:add_handler(P,?MODULE,C),gen_event:which_handlers(P)} end);
scenario(C) when C=:=call_bad; C=:=call_throw; C=:=call_remove -> manager(fun(P) ->
    ok=add(P),{gen_event:call(P,?MODULE,C),gen_event:which_handlers(P)} end);
scenario(C) when C=:=event_bad; C=:=event_remove -> manager(fun(P) ->
    ok=add(P),ok=gen_event:sync_notify(P,C),gen_event:which_handlers(P) end);
scenario(info) -> manager(fun(P) -> ok=add(P),P!{increment,7},state(P) end);
scenario(swap) -> manager(fun(P) -> ok=add(P),
    R=gen_event:swap_handler(P,{?MODULE,old},{{?MODULE,new},8}),
    {R,gen_event:which_handlers(P),gen_event:call(P,{?MODULE,new},get)} end);
scenario(swap_missing) -> manager(fun(P) ->
    R=gen_event:swap_handler(P,{missing,old},{?MODULE,8}),{R,state(P)} end);
scenario(swap_failure) -> manager(fun(P) -> ok=add(P),
    R=gen_event:swap_handler(P,{?MODULE,old},{?MODULE,init_bad}),{R,gen_event:which_handlers(P)} end);
scenario(C) when C=:=callback_swap; C=:=callback_swap_hibernate -> manager(fun(P) ->
    ok=add(P),ok=gen_event:sync_notify(P,C),
    {gen_event:which_handlers(P),sys:get_state(P)} end);
scenario(call_swap) -> manager(fun(P) -> ok=add(P),
    R=gen_event:call(P,?MODULE,call_swap),{R,gen_event:which_handlers(P),sys:get_state(P)} end);
scenario(supervised_delete) -> manager(fun(P) ->
    ok=gen_event:add_sup_handler(P,?MODULE,0),R=gen_event:delete_handler(P,?MODULE,delete),{R,exit_message()} end);
scenario(supervised_remove) -> manager(fun(P) ->
    ok=gen_event:add_sup_handler(P,?MODULE,0),ok=gen_event:sync_notify(P,event_remove),exit_message() end);
scenario(supervised_swap) -> manager(fun(P) ->
    ok=gen_event:add_sup_handler(P,?MODULE,0),
    ok=gen_event:swap_sup_handler(P,{?MODULE,old},{{?MODULE,new},8}),
    {H,{swapped,New,Sup}}=exit_message(),
    {H,New,Sup=:=self(),gen_event:call(P,{?MODULE,new},get)} end);
scenario(supervised_shutdown) ->
    {ok,P}=gen_event:start(),ok=gen_event:add_sup_handler(P,?MODULE,0),
    ok=gen_event:stop(P),exit_message();
scenario(supervisor_exit) -> manager(fun(P) ->
    Parent=self(),Sup=spawn(fun() -> ok=gen_event:add_sup_handler(P,?MODULE,{watch,Parent}),
        Parent!ready,receive stop -> exit(bye) end end),
    receive ready -> ok end,Ref=monitor(process,Sup),Sup!stop,
    receive {'DOWN',Ref,process,Sup,bye} -> ok end,
    receive {observed,{terminated,{stop,bye}}} -> ok after 1000 -> error(no_terminate) end,
    gen_event:which_handlers(P) end);
scenario(get_state) -> manager(fun(P) -> ok=add(P),ok=gen_event:add_handler(P,{?MODULE,x},4),sys:get_state(P) end);
scenario(replace_state) -> manager(fun(P) -> ok=add(P),
    R=sys:replace_state(P,fun({M,I,S})->{M,I,S+9} end),{R,state(P)} end);
scenario(replace_bad) -> manager(fun(P) -> ok=add(P),
    R=sys:replace_state(P,fun(_)->wrong end),{R,state(P)} end);
scenario(code_change) -> manager(fun(P) -> ok=add(P),ok=sys:suspend(P),
    R=sys:change_code(P,?MODULE,old,6),ok=sys:resume(P),{R,state(P)} end);
scenario(code_change_bad) -> manager(fun(P) -> ok=add(P),ok=sys:suspend(P),
    R=sys:change_code(P,?MODULE,old,bad),ok=sys:resume(P),{R,state(P)} end);
scenario(status) -> {ok,P}=gen_event:start({local,gen_event_port_status}), try ok=add(P),
    {status,P,{module,gen_event},[_Dict,running,Parent,_Debug,Data]}=sys:get_status(P),
    [Header,{data,Fields},Items]=Data,
    {is_pid(Parent),[Header,{data,lists:keyreplace("Parent",1,Fields,{"Parent",is_pid(Parent)})},Items]} after gen_event:stop(P) end;
scenario(debug_log) -> manager(fun(P) ->
    ok=sys:log(P,true),ok=add(P),ok=gen_event:sync_notify(P,1),
    {ok,Log}=sys:log(P,get),length(Log) end);
scenario(C) when C=:=request_wait;C=:=request_receive;C=:=request_check -> manager(fun(P) ->
    ok=add(P),R=gen_event:send_request(P,?MODULE,get),true=is_reference(R),
    case C of request_wait -> gen_event:wait_response(R,1000);
        request_receive -> gen_event:receive_response(R,1000);
        request_check -> receive Msg -> gen_event:check_response(Msg,R) after 1000 -> timeout end end end);
scenario(C) when C=:=request_collection_wait;C=:=request_collection_receive;C=:=request_collection_check -> manager(fun(P) ->
    ok=add(P),R=gen_event:send_request(P,?MODULE,get,label,gen_event:reqids_new()),
    Reply=case C of request_collection_wait -> gen_event:wait_response(R,1000,true);
        request_collection_receive -> gen_event:receive_response(R,1000,true);
        request_collection_check -> receive Msg -> gen_event:check_response(Msg,R,true) after 1000 -> timeout end end,
    {V,L,New}=Reply,{V,L,gen_event:reqids_size(New)} end);
scenario(request_error) -> manager(fun(P) ->
    R=gen_event:send_request(P,missing,get),gen_event:receive_response(R,1000) end);
scenario(local_name) ->
    {ok,P}=gen_event:start({local,gen_event_port_name},[]),
    try {ok=add(P),gen_event:which_handlers(gen_event_port_name),
         case gen_event:start({local,gen_event_port_name}) of {error,{already_started,P}} -> duplicate end}
    after gen_event:stop(P) end;
scenario(monitor) ->
    {ok,{P,Ref}}=gen_event:start_monitor(),ok=gen_event:stop(P),
    receive {'DOWN',Ref,process,P,normal} -> ok after 1000 -> missing end;
scenario(linked) -> {ok,P}=gen_event:start_link([]),ok=add(P),gen_event:stop(P,normal,1000);
scenario(hibernate) -> manager(fun(P) -> ok=gen_event:add_handler(P,?MODULE,hibernate),state(P) end);
scenario(auto_hibernate) ->
    {ok,P}=gen_event:start([{hibernate_after,0}]),try ok=add(P),state(P) after gen_event:stop(P) end;
scenario(invalid_start) -> gen_event:start(bad);
scenario(invalid_swap) -> gen_event:swap_handler(bad,bad,bad);
scenario(missing_handler) -> manager(fun(P) ->
    {gen_event:call(P,missing,get),gen_event:delete_handler(P,missing,bye)} end);
scenario(reqids) ->
    Empty=gen_event:reqids_new(),Ref=make_ref(),
    Col=gen_event:reqids_add(Ref,label,Empty),
    {[{Ref,label}]=:=gen_event:reqids_to_list(Col),gen_event:reqids_size(Col),
     gen_event:wait_response(Empty,0,true),gen_event:check_response(unrelated,Ref)};
scenario(get_modules) -> manager(fun(P) -> ok=add(P),ok=add(P),
    {ok,R}=gen:call(P,self(),get_modules,infinity),R end);
scenario(C) when C=:=format_warning;C=:=format_crash;C=:=format_single;C=:=format_depth;
                 C=:=format_latin1;C=:=format_limit;C=:=legacy_warning;C=:=legacy_crash ->
    Warning=#{label=>{gen_event,no_handle_info},module=>example,message=>[1,2,{a,b}]},
    Crash=#{label=>{gen_event,terminate},handler=>example,name=>named,last_message=>hello,
        state=>[1,2,{a,b}],reason=>{'EXIT',broken},process_label=>label},
    case C of
        legacy_warning -> gen_event:format_log(Warning);
        legacy_crash -> gen_event:format_log(Crash);
        format_warning -> lists:flatten(gen_event:format_log(Warning,#{}));
        format_single -> lists:flatten(gen_event:format_log(Crash,#{single_line=>true}));
        format_depth -> lists:flatten(gen_event:format_log(Crash,#{depth=>2}));
        format_latin1 -> lists:flatten(gen_event:format_log(Crash,#{encoding=>latin1}));
        format_limit -> lists:flatten(gen_event:format_log(Crash,#{chars_limit=>40}));
        _ -> lists:flatten(gen_event:format_log(Crash,#{})) end;
scenario(C) when C=:=init_crash;C=:=init_exit -> manager(fun(P)-> gen_event:add_handler(P,?MODULE,C) end);
scenario(C) when C=:=call_crash;C=:=call_exit -> manager(fun(P)-> ok=add(P),gen_event:call(P,?MODULE,C) end);
scenario(C) when C=:=event_crash;C=:=info_crash -> manager(fun(P)->
    ok=gen_event:add_sup_handler(P,?MODULE,0),
    case C of event_crash -> gen_event:sync_notify(P,C); info_crash -> P!C end,exit_message() end);
scenario(C) when C=:=terminate_crash;C=:=terminate_throw -> manager(fun(P)-> ok=add(P),gen_event:delete_handler(P,?MODULE,C) end);
scenario(behaviour_callbacks) -> gen_event:behaviour_info(callbacks);
scenario(behaviour_optional) -> gen_event:behaviour_info(optional_callbacks);
scenario(behaviour_invalid) -> gen_event:behaviour_info(other);
scenario(C) ->
    Test=case C of upstream_add->add_handler; upstream_delete->delete_handler;
        upstream_swap->swap_handler; upstream_swap_sup->swap_sup_handler;
        upstream_notify->notify; upstream_sync_notify->sync_notify;
        upstream_call->call; upstream_info->info;
        upstream_get_state->get_state; upstream_replace_state->replace_state end,
    try apply(gen_event_SUITE,Test,[[]]) after catch gen_event:stop(my_dummy_handler) end.

init(init_crash) -> erlang:error(crashed);
init(init_exit) -> exit(exited);
init(init_error) -> {error,denied};
init(init_throw) -> throw(thrown);
init(init_bad) -> unexpected;
init(hibernate) -> {ok,0,hibernate};
init({hibernate,_}) -> {ok,0,hibernate};
init({init_bad,_}) -> unexpected;
init({watch,Pid}) -> {ok,{watch,Pid}};
init({N,Transfer}) -> {ok,{N,Transfer}};
init(N) -> {ok,N}.
handle_event(event_crash,_) -> erlang:error(crashed);
handle_event(event_remove,_) -> remove_handler;
handle_event(event_bad,_) -> unexpected;
handle_event(callback_swap,S) -> {swap_handler,old,S,{?MODULE,new},8};
handle_event(callback_swap_hibernate,S) -> {swap_handler,old,S,{?MODULE,new},hibernate};
handle_event(N,S) -> {ok,S+N}.
handle_info(info_crash,_) -> erlang:error(crashed);
handle_info({increment,N},S) -> {ok,S+N};
handle_info(_,S) -> {ok,S}.
handle_call(call_crash,_) -> erlang:error(crashed);
handle_call(call_exit,_) -> exit(exited);
handle_call(get,S) -> {ok,S,S};
handle_call(call_bad,_) -> unexpected;
handle_call(call_throw,_) -> throw(thrown);
handle_call(call_remove,_) -> {remove_handler,removed};
handle_call(call_swap,S) -> {swap_handler,replaced,old,S,{?MODULE,new},8}.
terminate(terminate_crash,_) -> erlang:error(crashed);
terminate(terminate_throw,_) -> throw(thrown);
terminate(Why,{watch,Pid}) -> Pid!{observed,{terminated,Why}},ok;
terminate(Why,S) -> {terminated,Why,S}.
code_change(_,_,bad) -> unexpected;
code_change(_,S,N) -> {ok,S+N}.
format_status(Status) -> Status.
