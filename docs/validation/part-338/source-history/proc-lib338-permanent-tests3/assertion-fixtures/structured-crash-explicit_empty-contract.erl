-module(oracle_proc_lifecycle).
-export([run/0,target/0,awake/1,fail/2,log/2,adding_handler/1,removing_handler/1,changing_config/3]).
run()->
    Parent=self(),
    Leader=group_leader(),
    ok=logger:add_handler(lifecycle_observation,oracle_proc_lifecycle,#{level=>all,filter_default=>log,config=>#{owner=>self()}}),
    try
        C=proc_lib:spawn(oracle_proc_lifecycle,fail,[self(),explicit_empty]),
        R=monitor(process,C),
        receive {ready,C}->ok after 5000->exit(no_ready) end,
        C!crash,
        Event=receive {captured_log,#{level:=error,msg:={report,#{label:={proc_lib,crash}}}}=E}->E after 5000->exit(no_report) end,
        ExitReason=receive {'DOWN',R,process,C,D}->D after 5000->exit(no_down) end,
        #{msg:={report,#{report:=[Own,Neighbours]}},meta:=Meta}=Event,
        {pid,C}=lists:keyfind(pid,1,Own),
        {process_label,worker_label}=lists:keyfind(process_label,1,Own),
        {dictionary,Dict}=lists:keyfind(dictionary,1,Own),
        {kept,42}=lists:keyfind(kept,1,Dict),
        false=lists:keyfind('$ancestors',1,Dict),
        false=lists:keyfind('$initial_call',1,Dict),
        false=lists:keyfind('$process_label',1,Dict),
        [otp,sasl]=maps:get(domain,Meta),
        #{title:="CRASH REPORT"}=maps:get(logger_formatter,Meta),
        #{tag:=error_report,type:=crash_report}=maps:get(error_logger,Meta),
        true=is_function(maps:get(report_cb,Meta),2),
        []=Neighbours,
        {error_info,{Class,Reason,Trace}}=lists:keyfind(error_info,1,Own),
        verify_mode(explicit_empty,Class,Reason,Trace,ExitReason,Own),
        [initial_call,pid,registered_name,process_label,error_info,ancestors,message_queue_len,messages,links,dictionary,trap_exit,status,heap_size,stack_size,reductions]=[element(1,Field)||Field<-Own],
        {ancestors,[Parent]}=lists:keyfind(ancestors,1,Own),
        {registered_name,[]}=lists:keyfind(registered_name,1,Own),
        {links,[]}=lists:keyfind(links,1,Own),
        {trap_exit,false}=lists:keyfind(trap_exit,1,Own),
        {messages,[]}=lists:keyfind(messages,1,Own),
        {message_queue_len,0}=lists:keyfind(message_queue_len,1,Own),
        {status,running}=lists:keyfind(status,1,Own),
        {heap_size,Heap}=lists:keyfind(heap_size,1,Own),true=is_integer(Heap),true=Heap>=0,
        {stack_size,StackSize}=lists:keyfind(stack_size,1,Own),true=is_integer(StackSize),true=StackSize>=0,
        {reductions,Reductions}=lists:keyfind(reductions,1,Own),true=is_integer(Reductions),true=Reductions>=0,
        #{pid:=C,gl:=Leader,mfa:={proc_lib,crash_report,4},time:=Time,file:=File,line:=Line}=Meta,
        true=is_integer(Time),true=is_integer(Line),true=Line>0,
        true=lists:suffix("proc_lib.erl",File),
        {proc_lib,report_cb,2}=erlang:fun_info_mfa(maps:get(report_cb,Meta)),
        true=is_list(Trace),
        lists:foreach(fun({M,F,A,Location})->true=is_atom(M),true=is_atom(F),true=(is_integer(A) orelse is_list(A)),true=is_list(Location) end,Trace),
        lists:foreach(fun(SuppressedReason)->
            {Silent,SilentRef}=spawn_monitor(fun()->proc_lib:init_p(Parent,[],fun()->exit(SuppressedReason) end) end),
            receive {'DOWN',SilentRef,process,Silent,SuppressedReason}->ok after 5000->exit(no_suppressed_down) end,
            receive {captured_log,#{meta:=#{pid:=Silent}}}->exit(unexpected_suppressed_report) after 0->ok end
        end,[normal,shutdown,{shutdown,details}]),
        ok
    after
        logger:remove_handler(lifecycle_observation)
    end
.

target()->{get('$ancestors'),get('$initial_call')}.
awake(P)->receive wake->P!{resumed,self(),get('$initial_call'),get('$process_label')} end.
fail(P,Mode)->proc_lib:set_label(worker_label),put(kept,42),P!{ready,self()},receive crash->crash(Mode) end.
crash(error)->erlang:error(boom);
crash(error_normal)->erlang:error(normal);
crash(throw)->throw(thrown);
crash(exit)->exit(custom_exit);
crash(explicit_empty)->erlang:raise(error,boom,[]);
crash(explicit_user)->erlang:raise(error,boom,[{sample,user,0,[]}]);
crash(cleared)->erase('$initial_call'),put('$ancestors',corrupt),erlang:error(boom).
log(Event,#{config:=#{owner:=Owner}})->Owner!{captured_log,Event},ok.
adding_handler(Config)->{ok,Config}.
removing_handler(_Config)->ok.
changing_config(_Operation,_Old,New)->{ok,New}.
trace_mfas(Trace)->[{M,F,arity(A)}||{M,F,A,_}<-Trace].
arity(A) when is_integer(A)->A;
arity(A)->length(A).

verify_mode(explicit_empty,error,boom,[],{boom,[]},_)->ok;
verify_mode(explicit_user,error,boom,[{sample,user,0,[]}],{boom,[{sample,user,0,[]}]},_)->ok;
verify_mode(cleared,error,boom,Trace,{boom,Trace},Own)->false=lists:keyfind(initial_call,1,Own),{ancestors,[]}=lists:keyfind(ancestors,1,Own),ok;
verify_mode(error,error,boom,Trace,{boom,Trace},Own)->initial(Own);
verify_mode(error_normal,error,normal,Trace,{normal,Trace},Own)->initial(Own);
verify_mode(throw,throw,thrown,Trace,{{nocatch,thrown},Trace},Own)->initial(Own);
verify_mode(exit,exit,custom_exit,_Trace,custom_exit,Own)->initial(Own).
initial(Own)->{initial_call,{oracle_proc_lifecycle,fail,['Argument__1','Argument__2']}}=lists:keyfind(initial_call,1,Own),ok.
