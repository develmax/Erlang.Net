-module(logger_port_cases).
-export([cases/0, observe/1, capture/1, write/1, log/2, adding_handler/1, removing_handler/1, changing_config/3, filter_config/1]).

cases() ->
    [{compare,A,B} || A <- [none,emergency,alert,critical,error,warning,notice,info,debug,all],
                      B <- [none,emergency,alert,critical,error,warning,notice,info,debug,all]] ++
    [{report,R} || R <- [#{}, [], "abc", ["ab",[99]], [{x,"hello"},{y,[]}],
                        #{a=>1,b=>"str"}, [a,1,2], [1|x], atom, <<"str">>,
                        [{label,x},{report,#{a=>1}}], [{a,[16#1f600]}], [{a,<<"bin">>}]]] ++
    [metadata, defaults, levels, configuration, proxy, handlers, event, lazy, suppressed,
     filters, formatter, application, api_overloads, init_success, handlers_environment,
     application_loaded, pretty, reconfigure_success, init_config, reconfigure_config, configuration_errors, stack_filter] ++
    [{upstream,F} || F <- [start_stop,compare_levels,format_report,process_metadata,config_sanity_check]] ++
    [{bad,F,Args} || {F,Args} <- [{compare_levels,[bad,info]}, {compare_levels,[info,123]},
        {set_process_metadata,[bad]}, {update_process_metadata,[[]]},
        {get_module_level,[123]}, {allow,[info,123]},
        {set_primary_config,[#{level=>bad}]}, {set_primary_config,[bad]},
        {update_primary_config,[#{unknown=>1}]}, {set_proxy_config,[bad]},
        {set_module_level,[[a,1],info]}, {unset_module_level,[123]},
        {update_formatter_config,[missing,bad]}, {i,[123]}, {add_handlers,[[bad]]}]] ++
    [invalid_message].

write(Directory) ->
    [logger:remove_handler(Id) || Id <- logger:get_handler_ids()],
    ok=logger:set_primary_config(#{}),
    Rows = [{C,capture(C)} || C <- cases()],
    ok = file:write_file(filename:join(Directory,"cases.etf"),term_to_binary(Rows)),
    ok = file:write_file(filename:join(Directory,"exports.etf"),term_to_binary(
        lists:sort([{F,A} || {F,A} <- logger:module_info(exports), F =/= module_info]))).

observe(Case) ->
    try {value,run(Case)} catch C:R:S -> {exception,C,R,S} end.

capture(Case) ->
    Parent=self(),Ref=make_ref(),
    spawn(fun() -> Parent ! {Ref,observe(Case)} end),
    receive {Ref,Result} -> Result after 20000 -> error({case_timeout,Case}) end.

run({compare,A,B}) -> logger:compare_levels(A,B);
%% maps:to_list/1 has unspecified order. Compare exact format fragments paired
%% with their original key/value arguments; list reports remain ordered.
run({report,R}) when is_map(R) ->
    {map_report,report_associations(logger:format_report(R)),report_associations(logger:format_otp_report(R))};
run({report,R}) -> {logger:format_report(R),logger:format_otp_report(R)};
run({upstream,F}) ->
    Old=logger:get_primary_config(),
    Config=logger_SUITE:init_per_testcase(F,[]),
    try apply(logger_SUITE,F,[Config])
    after logger_SUITE:end_per_testcase(F,Config),logger:set_primary_config(Old),logger:unset_module_level() end;
run({bad,F,Args}) -> apply(logger,F,Args);
run(invalid_message) ->
    Old=logger:get_primary_config(),
    ok=logger:set_primary_config(level,info),
    ok=logger:set_process_metadata(#{time=>42}),
    try logger:info(not_a_message)
    catch error:function_clause:Stack -> {function_clause,invalid_message_stack(Stack)}
    after logger:set_primary_config(Old) end;
run(metadata) ->
    logger:unset_process_metadata(),
    A=logger:get_process_metadata(),
    ok=logger:set_process_metadata(#{a=>1,x=>old}),
    ok=logger:update_process_metadata(#{x=>new,b=>2}),
    B=logger:get_process_metadata(), ok=logger:unset_process_metadata(),
    {A,B,logger:get_process_metadata()};
run(defaults) ->
    M=logger:add_default_metadata(#{pid=>fixed,time=>42,gl=>group_leader()}),
    true=(maps:get(gl,M)=:=group_leader()),
    N=logger:add_default_metadata(#{}),
    {maps:remove(gl,M),maps:get(pid,N)=:=self(),maps:get(gl,N)=:=group_leader(),is_integer(maps:get(time,N)),is_integer(logger:timestamp())};
run(levels) ->
    ok=logger:unset_module_level(),
    ok=logger:set_module_level([logger_test_a,logger_test_b],debug),
    A=logger:get_module_level([logger_test_b]),
    ok=logger:unset_module_level(logger_test_a),
    B=logger:get_module_level(),ok=logger:unset_module_level(),{A,B};
run(configuration) ->
    Old=logger:get_primary_config(),
    try
        ok=logger:set_primary_config(#{level=>warning,metadata=>#{a=>1}}),
        A=logger:get_primary_config(),
        ok=logger:update_primary_config(#{metadata=>#{b=>2}}),
        ok=logger:set_primary_config(level,debug),
        {A,logger:get_primary_config()}
    after logger:set_primary_config(Old) end;
run(proxy) ->
    Old=logger:get_proxy_config(),
    try
        ok=logger:set_proxy_config(#{sync_mode_qlen=>20}), A=logger:get_proxy_config(),
        ok=logger:update_proxy_config(#{sync_mode_qlen=>30}), {A,logger:get_proxy_config()}
    after logger:set_proxy_config(Old) end;
run(handlers) ->
    with_handler(fun() ->
        A=logger:get_handler_config(logger_port_test),
        ok=logger:set_handler_config(logger_port_test,level,warning),
        ok=logger:update_handler_config(logger_port_test,config,#{b=>2}),
        B=logger:get_handler_config(logger_port_test),
        ok=logger:update_handler_config(logger_port_test,#{level=>debug}),
        {A,B,logger:get_handler_config(logger_port_test)}
    end);
run(event) -> events(fun() ->
    ok=logger:set_primary_config(metadata,#{a=>primary,x=>primary}),
    ok=logger:set_process_metadata(#{b=>process,x=>process}),
    logger:macro_log(#{c=>location,x=>location},notice,"~p",[hello],meta()),
    [apply(logger,L,[#{report=>L},meta()]) || L <- [emergency,alert,critical,error,warning,notice,info,debug]],
    logger:log(info,<<"binary">>,meta())
end);
run(api_overloads) -> events(fun() ->
    [begin
        apply(logger,L,["one"]),
        apply(logger,L,["~p",[two]]),
        apply(logger,L,["~p",[three],meta()])
     end || L <- [emergency,alert,critical,error,warning,notice,info,debug]],
    logger:log(info,"two"),logger:log(info,"~p",[three]),logger:log(info,"~p",[four],meta()),
    logger:macro_log(#{},info,"macro3"),logger:macro_log(#{},info,"macro4",meta()),
    logger:macro_log(#{},info,"~p",[macro4format]),
    logger:macro_log(#{},info,fun(_) -> "macro5lazy" end,x,meta()),
    true=logger:allow(info,?MODULE)
end);
run(lazy) -> events(fun() ->
    logger:info(fun(_) -> {#{hello=>world},#{x=>lazy}} end,unused,meta()),
    logger:info(fun(_) -> ignore end,unused,meta()),
    logger:info(fun(_) -> wrong end,unused,meta()),
    logger:info(fun(_) -> error(boom) end,unused,meta()),
    logger:info(fun(_) -> {"bad group leader",#{gl=>invalid}} end,unused,meta())
end);
run(suppressed) ->
    Old=logger:get_primary_config(),
    try
        ok=logger:set_primary_config(level,none),
        logger:debug(fun(_) -> error(must_not_run) end,x),
        logger:log(info,invalid_message,invalid_metadata)
    after logger:set_primary_config(Old) end;
run(filters) -> events(fun() ->
    ok=logger:add_primary_filter(logger_port_filter,{fun(E,_) -> E#{meta=>(maps:get(meta,E))#{filtered=>true}} end,x}),
    logger:notice("kept",meta()),
    ok=logger:remove_primary_filter(logger_port_filter),
    ok=logger:add_handler_filter(logger_port_test,logger_port_filter,{fun(_,_) -> stop end,x}),
    logger:notice("dropped",meta()),
    ok=logger:remove_handler_filter(logger_port_test,logger_port_filter)
end);
run(formatter) -> with_handler(fun() ->
    ok=logger:update_formatter_config(logger_port_test,#{single_line=>false}),
    ok=logger:update_formatter_config(logger_port_test,depth,10),
    logger:get_handler_config(logger_port_test)
end);
run(application) -> {logger:set_application_level(logger_missing_application,debug),logger:unset_application_level(logger_missing_application)};
run(application_loaded) ->
    Spec={application,logger_port_app,[{vsn,"1"},{modules,[logger_port_a,logger_port_b]}]},
    ok=application:load(Spec),
    try
        ok=logger:set_application_level(logger_port_app,warning),
        A=logger:get_module_level([logger_port_a,logger_port_b]),
        ok=logger:unset_application_level(logger_port_app),{A,logger:get_module_level(logger_port_a)}
    after application:unload(logger_port_app) end;
run(init_success) -> with_environment(fun() ->
    application:set_env(kernel,logger,[]),application:set_env(kernel,logger_level,debug),
    application:set_env(kernel,logger_metadata,#{init=>metadata}),application:unset_env(kernel,error_logger),
    {logger:internal_init_logger(),logger:get_primary_config(),application:get_env(kernel,logger)}
end);
run(handlers_environment) ->
    Old=application:get_env(logger_port_app,logger),
    try
        application:set_env(logger_port_app,logger,[{handler,logger_port_test,?MODULE,#{config=>#{owner=>self(),a=>1}}}]),
        A=logger:add_handlers(logger_port_app),B=logger:get_handler_config(logger_port_test),
        ok=logger:remove_handler(logger_port_test),
        {A,B,logger:add_handlers([{handler,logger_port_test,?MODULE,#{}}]),logger:remove_handler(logger_port_test)}
    after
        case Old of undefined -> application:unset_env(logger_port_app,logger); {ok,V} -> application:set_env(logger_port_app,logger,V) end
    end;
run(pretty) -> with_handler(fun() ->
    capture_output(fun() -> logger:i(primary),logger:i(handlers),logger:i(proxy),logger:i(modules),logger:i(logger_port_test),logger:i() end)
end);
run(reconfigure_success) -> with_environment(fun() ->
    application:set_env(kernel,logger,[]),application:set_env(kernel,error_logger,false),
    application:set_env(kernel,logger_level,notice),application:set_env(kernel,logger_metadata,#{}),
    {logger:reconfigure(),logger:get_handler_ids(),logger:get_primary_config()}
end);
run(init_config) ->
    Old=application:get_env(kernel,logger_level),
    try application:set_env(kernel,logger_level,bad),logger:internal_init_logger()
    after restore(logger_level,Old) end;
run(reconfigure_config) ->
    Old=application:get_env(kernel,logger_level),
    try application:set_env(kernel,logger_level,bad),logger:reconfigure()
    after restore(logger_level,Old) end;
run(configuration_errors) -> events(fun() -> with_environment(fun() ->
    logger:add_handlers([bad]),
    application:set_env(kernel,logger_level,bad),logger:internal_init_logger(),
    application:set_env(kernel,logger_level,info),application:set_env(kernel,error_logger,bad),logger:internal_init_logger()
end) end);
run(stack_filter) ->
    {logger:filter_stacktrace(b,[{a,f,0,[]},{b,g,1,[]},{c,h,2,[]}]),
     logger:filter_stacktrace(z,[{a,f,0,[]}])}.

restore(Key,undefined) -> application:unset_env(kernel,Key);
restore(Key,{ok,Value}) -> application:set_env(kernel,Key,Value).
with_environment(F) ->
    Keys=[logger,error_logger,logger_level,logger_metadata,logger_default_metadata],
    Old=[{K,application:get_env(kernel,K)} || K <- Keys],
    try F() after [restore(K,V) || {K,V} <- Old] end.
meta() -> #{pid=>fixed,time=>42,gl=>group_leader(),x=>call}.
with_handler(F) ->
    logger:remove_handler(logger_port_test),
    ok=logger:add_handler(logger_port_test,?MODULE,#{config=>#{owner=>self(),a=>1}}),
    try F() after logger:remove_handler(logger_port_test) end.
events(F) ->
    Old=logger:get_primary_config(),
    try
        ok=logger:set_primary_config(#{level=>all}),logger:unset_process_metadata(),
        with_handler(fun() -> F(),collect([]) end)
    after logger:set_primary_config(Old),logger:unset_process_metadata() end.
collect(Acc) ->
    receive {logger_port_event,E} -> collect([E|Acc]) after 0 -> lists:reverse(Acc) end.
log(#{meta:=M}=E,#{config:=#{owner:=Owner}}) ->
    true=maps:get(gl,M)=:=group_leader(),
    P=case maps:get(pid,M) of Owner -> validated_owner_pid; Other -> Other end,
    T=case maps:get(time,M) of 42 -> 42; Time when is_integer(Time) -> validated_timestamp end,
    Owner ! {logger_port_event,E#{meta=>M#{gl=>validated_group_leader,pid=>P,time=>T}}},ok.
adding_handler(C) -> {ok,C}.
removing_handler(_) -> ok.
changing_config(_,_,C) -> {ok,C}.
filter_config(#{config:=C}=Config) -> Config#{config=>maps:remove(owner,C)}.

report_associations({"~tp",[[]]}=Empty) -> Empty;
report_associations({Format,Args}) ->
    lists:sort(report_associations(string:split(Format,"\n",all),Args)).
report_associations([Line|Lines],[Key,Value|Args]) ->
    [{Key,Line,Value}|report_associations(Lines,Args)];
report_associations([],[]) -> [].

capture_output(F) ->
    Old=group_leader(),Device=spawn(fun() -> io_device([]) end),
    group_leader(Device,self()),
    try F(),Device ! {take,self()},receive {output,Output} -> Output end
    after group_leader(Old,self()),exit(Device,kill) end.
io_device(Acc) ->
    receive
        {io_request,From,Ref,getopts} -> From ! {io_reply,Ref,[{encoding,unicode}]},io_device(Acc);
        {io_request,From,Ref,{put_chars,Encoding,Chars}} ->
            From ! {io_reply,Ref,ok},io_device([unicode:characters_to_binary(Chars,Encoding)|Acc]);
        {io_request,From,Ref,{put_chars,Encoding,M,F,Args}} ->
            Chars=apply(M,F,Args),From ! {io_reply,Ref,ok},io_device([unicode:characters_to_binary(Chars,Encoding)|Acc]);
        {take,From} -> From ! {output,iolist_to_binary(lists:reverse(Acc))},io_device([])
    end.

%% Compare live identities only after verifying their role in the original frame.
invalid_message_stack([{logger,do_log_allowed,[Level,Msg,#{pid:=Pid,gl:=Gl}=Meta,Tid,Config],Location}|Rest]) ->
    Pid=self(),
    Gl=group_leader(),
    Tid=ets:whereis(logger),
    [{logger,do_log_allowed,[Level,Msg,Meta#{pid=>caller,gl=>group_leader},logger_table,Config],Location}|Rest];
invalid_message_stack(Stack) -> Stack.
