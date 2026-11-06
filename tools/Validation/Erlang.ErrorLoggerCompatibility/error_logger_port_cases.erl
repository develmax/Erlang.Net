%% Same observer BEAM runs on official OTP and both .NET backends.
-module(error_logger_port_cases).
-export([write/1, boot/0, cases/0, worker/2, run/1, init/1, handle_event/2,
         handle_call/2, handle_info/2, terminate/2, log/2, adding_handler/1, removing_handler/1]).

boot() ->
    {ok,AC}=application_controller:start({application,kernel,[{vsn,"test"},{env,[]}]}),unlink(AC),
    {ok,LS}=logger_server:start_link(),unlink(LS),
    {ok,LP}=logger_sup:start_link(),unlink(LP),
    {ok,FS}=file_server:start_link(),unlink(FS),
    ok.

cases() ->
    [callback_storage, tty_default, lifecycle, handlers, absent, depth, depth_invalid, depth_invalid_limit, callback_config, ignore,
     raw, emulator, report_callback, report_callback2, report_callback_bad,
     report_callback_bad2, report_callback_crash, report_callback_crash2,
     report_callback_priority, report_callback_invalid, missing_notify_metadata,
     tty, logfile, no_logfile, own_format, own_bad_format, own_map, own_list,
     filtered, off_heap] ++
    [{message,F,V,A} || F<-[error_msg,warning_msg,info_msg,format],
      {V,A}<-[{"hello",[]},{"~p",[value]},{{bad,format},atom},{42,42},{<<"hello">>,[]},{"~p",[1|tail]}]] ++
    [{message1,F,V} || F<-[error_msg,warning_msg,info_msg], V<-["hello",{bad,format},42,[]]] ++
    [{report,F,T,V} || F<-[error_report,warning_report,info_report],T<-[standard,custom,undefined,42],V<-[#{a=>1},[{a,1}],"text",{a,b},[]]] ++
    [{error_info,V} || V<-["hello",[],[[]],[["x"],"y"],"\t",[0],{a,b},42,[1|tail]]] ++
    [{invalid,F,A} || {F,A}<-[{tty,[bad]},{logfile,[bad]},
      {add_report_handler,[42]},{add_report_handler,[{},[]]},
      {delete_report_handler,[42]},{adding_handler,[#{}]},
      {removing_handler,[#{}]},{log,[#{},#{}]}]].

write(Dir) ->
    _=logger:remove_handler(default),_=logger:remove_handler(simple),ok=logger:set_primary_config(level,all),
    Rows=[{C,observe(C)} || C<-cases()],
    ok=file:write_file(filename:join(Dir,"original.etf"),term_to_binary(Rows)),
    Exports=lists:sort(error_logger:module_info(exports)--[{module_info,0},{module_info,1}]),
    ok=file:write_file(filename:join(Dir,"exports.etf"),term_to_binary(Exports)),
    io:format("~p observations~n",[length(Rows)]),ok.

observe(C) ->
    Parent=self(),{P,M}=spawn_monitor(fun()->worker(C,Parent) end),
    receive {P,R}->receive {'DOWN',M,process,P,normal}->R end;
      {'DOWN',M,process,P,R}->{worker_died,R}
    after 15000->exit(P,kill),timeout end.
worker(C,Parent) -> Parent!{self(),run(C)}.
run(C) ->
    cleanup(),
    try norm({ok,scenario(C)})
    catch K:R:S -> norm({K,R,S})
    after cleanup() end.
cleanup() -> catch logger:remove_handler(default),catch logger:remove_handler(error_logger_tty_true),catch logger:remove_handler(error_logger),catch error_logger:stop(),
    catch application:unset_env(kernel,error_logger_format_depth),
    catch logger:remove_handler(error_logger_port_capture),ok.

capture() -> ok=error_logger:add_report_handler(?MODULE,self()).
events() -> gen_event:call(error_logger,?MODULE,events).
scenario({invalid,F,A}) -> apply(error_logger,F,A);
scenario({message,F,V,A}) -> capture(),R=apply(error_logger,F,[V,A]),{R,events()};
scenario({message1,F,V}) -> capture(),R=apply(error_logger,F,[V]),{R,events()};
scenario({report,F,standard,V}) -> capture(),R=apply(error_logger,F,[V]),{R,events()};
scenario({report,F,T,V}) -> capture(),R=apply(error_logger,F,[T,V]),{R,events()};
scenario({error_info,V}) -> capture(),R=error_logger:error_info(V),{R,events()};
scenario(lifecycle) ->
    ok=error_logger:start(),P=whereis(error_logger),ok=error_logger:start(),
    {error,{already_started,P}}=error_logger:start_link(),
    ok=error_logger:stop(),{is_pid(P),whereis(error_logger),error_logger:stop()};
scenario(handlers) ->
    Before=error_logger:which_report_handlers(),ok=error_logger:add_report_handler(?MODULE),
    During=error_logger:which_report_handlers(),R=error_logger:delete_report_handler(?MODULE),
    {Before,During,R,error_logger:which_report_handlers(),error_logger:delete_report_handler(?MODULE)};
scenario(absent) -> {error_logger:which_report_handlers(),error_logger:delete_report_handler(?MODULE),error_logger:stop()};
scenario(off_heap) -> capture(),process_info(whereis(error_logger),message_queue_data);
scenario(depth) ->
    [begin application:set_env(kernel,error_logger_format_depth,D),
       {D,error_logger:get_format_depth(),error_logger:limit_term(nest(20))} end || D<-[unlimited,-1,0,1,9,10,11,20,100000000000000000000]];
scenario(depth_invalid) -> application:set_env(kernel,error_logger_format_depth,bad),error_logger:get_format_depth();
scenario(depth_invalid_limit) -> application:set_env(kernel,error_logger_format_depth,bad),error_logger:limit_term({a,b});
scenario(callback_config) -> C=#{id=>error_logger,custom=>value},
    R=error_logger:adding_handler(C),{R,error_logger:removing_handler(C),whereis(error_logger)};
scenario(ignore) -> [error_logger:log(E,ignored)||E<-[#{level=>error,msg=>{string,"ignored"},meta=>#{}},
    #{level=>warning,msg=>{report,#{}},meta=>#{}},#{level=>notice,msg=>bad,meta=>#{error_logger=>#{tag=>anything}}}]];
scenario(raw) -> capture(),R=error_logger:log(event(error,{"~p",[raw]},#{tag=>error}),#{}),{R,events()};
scenario(emulator) -> capture(),R=error_logger:log(event(error,{"~p",[emulator]},#{tag=>error,emulator=>true}),#{}),{R,events()};
scenario(missing_notify_metadata) -> error_logger:log(#{level=>error,msg=>{"x",[]},meta=>#{error_logger=>#{tag=>error}}},#{});
scenario(report_callback_invalid) -> callback(42);
scenario(report_callback) -> callback(fun(_)->{"~p",[converted]} end);
scenario(report_callback2) -> callback(fun(_,Opts)->io_lib:format("~p",[Opts]) end);
scenario(report_callback_bad) -> callback(fun(_)->bad end);
scenario(report_callback_bad2) -> callback(fun(_,_)->bad end);
scenario(report_callback_crash) -> callback(fun(_)->error(callback_crash) end);
scenario(report_callback_crash2) -> callback(fun(_,_)->throw(callback_throw) end);
scenario(report_callback_priority) -> capture(),
    E=event(error,{report,#{a=>1}},#{tag=>error,report_cb=>fun(_)->{"priority",[]} end}),
    #{meta:=M}=E,R=error_logger:log(E#{meta=>M#{report_cb=>fun(_)->{"wrong",[]} end}},#{}),{R,events()};
scenario(tty) ->
    R1=error_logger:tty(false),R2=error_logger:tty(false),
    R3=error_logger:tty(true),R4=error_logger:tty(true),
    {R1,R2,R3,R4,case logger:get_handler_config(error_logger_tty_true) of
      {ok,C}->maps:without([id,module,filters],C); E->E end};
scenario(no_logfile) -> {error_logger:logfile(filename),error_logger:logfile(close)};
scenario(logfile) ->
    File="artifacts/error-logger-port-output.log",_=file:delete(File),
    R=error_logger:logfile({open,File}),Dup=error_logger:logfile({open,File}),
    Name=error_logger:logfile(filename),ok=error_logger:error_msg("file marker ~p~n",[42]),
    _=gen_event:which_handlers(error_logger),Close=error_logger:logfile(close),
    Read=file:read_file(File),_=file:delete(File),
    case Read of {ok,Bin}->{R,Dup,Name,Close,binary:match(Bin,<<"file marker 42">>)=/=nomatch,error_logger:logfile(filename)}; _->{file_read_failed,R,Dup,Name,Close,Read} end;
scenario(filtered) ->
    ok=logger:set_primary_config(level,emergency),R=error_logger:info_report(#{a=>1}),
    _=logger:remove_handler(default),_=logger:remove_handler(simple),ok=logger:set_primary_config(level,all),R;
scenario(own_format) -> own_callback("~p",[value]);
scenario(own_bad_format) -> own_callback("~p",[]);
scenario(own_map) -> own_report(#{a=>1});
scenario(own_list) -> own_report([{a,1}]);
scenario(callback_storage) ->
    ok=logger:add_handler(error_logger_port_capture,?MODULE,#{config=>#{owner=>self()}}),
    ok=error_logger:error_report(#{stored=>value}),
    receive {captured,{report,Report},#{report_cb:=Cb}} ->
      T=ets:new(callback_storage,[]),true=ets:insert(T,{callback,Cb}),
      [{callback,Stored}]=ets:lookup(T,callback),Restored=binary_to_term(term_to_binary(Stored)),
      R={Stored(Report),Restored(Report),Cb=:=Stored,Stored=:=Restored,
         [erlang:fun_info(Stored,K)||K<-[module,name,arity,env]]},ets:delete(T),R
    after 1000->missing end;
scenario(tty_default) ->
    ok=logger:add_handler(default,logger_std_h,#{config=>#{type=>standard_io}}),
    ok=error_logger:tty(false),ok=error_logger:tty(false),
    {ok,#{filters:=Off}}=logger:get_handler_config(default),
    ok=error_logger:tty(true),{ok,#{filters:=On}}=logger:get_handler_config(default),
    {lists:keymember(error_logger_tty_false,1,Off),lists:keymember(error_logger_tty_false,1,On)}.


own_callback(F,A) ->
    logger:add_handler(error_logger_port_capture,?MODULE,#{config=>#{owner=>self()}}),
    ok=error_logger:error_msg(F,A),receive {captured,Msg,Meta}->
      #{report_cb:=Cb}=Meta,{Msg,Cb(element(2,Msg)),maps:without([pid,gl,time,report_cb],Meta)} after 1000->missing end.
own_report(R) ->
    logger:add_handler(error_logger_port_capture,?MODULE,#{config=>#{owner=>self()}}),
    ok=error_logger:error_report(R),receive {captured,Msg,Meta}->
      #{report_cb:=Cb}=Meta,{Msg,Cb(element(2,Msg)),maps:without([pid,gl,time,report_cb],Meta)} after 1000->missing end.
callback(Cb) -> capture(),E=event(error,{report,#{a=>1}},#{tag=>error,report_cb=>Cb}),
    R=error_logger:log(E,#{}),{R,events()}.
event(Level,Msg,Legacy) -> #{level=>Level,msg=>Msg,meta=>#{pid=>self(),gl=>group_leader(),error_logger=>Legacy}}.
nest(0)->leaf;nest(N)->{a,nest(N-1)}.
norm(P) when is_pid(P) -> if P=:=self()->self;true->case P=:=group_leader() of true->leader;false->pid end end;
norm(T) when is_tuple(T)->list_to_tuple([norm(X)||X<-tuple_to_list(T)]);
norm(M) when is_map(M)->maps:from_list([{norm(K),norm(V)}||{K,V}<-maps:to_list(M)]);
norm([H|T])->[norm(H)|norm(T)];norm(X)->X.
init(Owner) -> {ok,{Owner,[]}}.
handle_event(E,{Owner,Es})->{ok,{Owner,[E|Es]}}.
handle_call(events,{Owner,Es})->{ok,lists:reverse(Es),{Owner,[]}}.
handle_info(_I,S)->{ok,S}.
terminate(_R,_S)->deleted.
adding_handler(C)->{ok,C}.
removing_handler(_)->ok.
log(#{msg:=Msg,meta:=Meta},#{config:=#{owner:=P}})->P!{captured,Msg,Meta},ok.
