-module(beam_ddll_final_modes445).
-export([cases/0,capture/2,write_profile/1,observer_child/4,helper/3]).
cases()->[permanent_transition_order,reload_missing_recover,reload_wrongname_recover,reload_initfail_recover,
 force_unload_option,force_loadflag_unload,force_loadflag_reload,force_multiowner_option,
 force_loadflag_owner_death,force_reload_pending_otherowner].
capture(Path,Profile)->
 Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
 case Profile of default->Effective=Old;values->Effective=Old;_->erlang:system_flag(backtrace_depth,Profile),Effective=erlang:system_flag(backtrace_depth,Profile) end,
 try
  D=case Profile of values->default;_->Profile end,
  Rows=[{C,observe(C,Path,D)}||C<-cases()],
  {{Profile,Effective,[{C,V}||{C,{V,_}}<-Rows]},{Profile,Effective,[{C,O}||{C,{_,O}}<-Rows]}}
 after erlang:system_flag(backtrace_depth,Old) end.
write_profile([Out,Path,Profile])->
 {V,O}=capture(Path,Profile),
 ok=file:write_file(Out++"/main.etf",term_to_binary(V,[{minor_version,2}])),
 ok=file:write_file(Out++"/own.etf",term_to_binary(O,[{minor_version,2}])),halt().
observe(C,Path,D)->
 {P,R}=spawn_monitor(?MODULE,observer_child,[self(),C,Path,D]),
 receive {P,V}->receive {'DOWN',R,process,P,normal}->V;{'DOWN',R,process,P,Why}->error({reported_then_down,C,Why}) end;
 {'DOWN',R,process,P,Why}->error({observer_down,C,Why})
 after 15000->exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,error({observer_timeout,C}) end.
observer_child(Parent,C,Path,D)->
 process_flag(trap_exit,true),put(own444,[]),put(ports444,[]),put(helpers444,[]),remember({observer,self()}),
 case D of default->ok;_->erlang:system_flag(backtrace_depth,D) end,
 Value=try probe(C,Path) of X->{value,X} catch K:R:S->{exception,K,R,S} end,
 case C of permanent_transition_order->cleanup_permanent();_->cleanup() end,Own=reverse(get(own444)),Parent!{self(),{Value,Own}}.
remember(V)->put(own444,[V|get(own444)]),V.
reverse(L)->reverse(L,[]). reverse([],A)->A;reverse([H|T],A)->reverse(T,[H|A]).
load(P)->load(P,[]).
load(P,O)->remember({try_load,O,erl_ddll:try_load(P,echo_drv,O)}),last_load().
last_load()->[{try_load,_,V}|_]=get(own444),V.
unload(O)->V=erl_ddll:try_unload(echo_drv,O),remember({try_unload,O,V}),V.
mon(Name,State)->R=erl_ddll:monitor(driver,{Name,State}),remember({monitor,Name,State,R}),R.
demon(R)->V=erl_ddll:demonitor(R),remember({demonitor,R,V}),V.
opened()->P=open_port({spawn_driver,"echo_drv"},[binary]),put(ports444,[P|get(ports444)]),remember({open_port,P}),P.
closed(P)->V=port_close(P),put(ports444,remove_port(P,get(ports444))),remember({port_close,P,V}),V.
remove_port(_,[])->[];remove_port(P,[P|T])->T;remove_port(P,[H|T])->[H|remove_port(P,T)].
summary({ok,S,R}) when is_reference(R)->{ok,S,true};summary(V)->V.
ref({ok,_,R}) when is_reference(R)->R.
wait(R)->receive {Direction,R,driver,Name,Tag}=M->remember({notification,M}),{Direction,Name,Tag}
 after 5000->error(driver_notification_timeout) end.
wait_two(A,B)->[wait_one(A,B),wait_one(A,B)].
wait_one(A,B)->receive {Direction,R,driver,Name,Tag}=M when R=:=A;R=:=B->
 remember({notification,M}),{case R=:=A of true->first;false->second end,Direction,Name,Tag}
 after 5000->error(two_notifications_timeout) end.
info(Item)->V=erl_ddll:info(echo_drv,Item),remember({info,Item,V}),V.
snap(Helper)->{roles(info(processes),Helper),roles(info(awaiting_load),Helper),roles(info(awaiting_unload),Helper)}.
roles([],_) -> [];
roles([{P,N}|T],H)->[{role(P,H),N}|roles(T,H)].
role(P,H)->case P=:=self() of true->self;false->case P=:=H of true->helper;false->unexpected_owner end end.
start_helper(Path,Loads)->start_helper(Path,Loads,[]).
start_helper(Path,Loads,Options)->
 {P,R}=spawn_monitor(?MODULE,helper,[self(),Path,{Loads,Options}]),put(helpers444,[{P,R}|get(helpers444)]),
 receive {ready,P,Results}->remember({helper_ready,P,R,Results}),P
 after 5000->error(helper_start_timeout) end.
ask(P,Command)->R=make_ref(),P!{command,self(),R,Command},
 receive {reply,P,R,V}->remember({helper_reply,P,Command,V}),V after 5000->error(helper_reply_timeout) end.
stop_helper(P,How)->
 {P,R}=find_helper(P,get(helpers444)),case How of kill->exit(P,kill);stop->P!stop end,
 receive {'DOWN',R,process,P,Why}=M->remember({helper_down,M}),put(helpers444,remove_helper(P,get(helpers444))),Why
 after 5000->error(helper_down_timeout) end.
find_helper(P,[{P,R}|_])->{P,R};find_helper(P,[_|T])->find_helper(P,T).
remove_helper(_,[])->[];remove_helper(P,[{P,_}|T])->T;remove_helper(P,[H|T])->[H|remove_helper(P,T)].
helper(Parent,Path,{Loads,Options})->Results=helper_load(Path,Loads,Options),Parent!{ready,self(),Results},helper_loop().
helper_load(_,0,_)->[];helper_load(P,N,O)->[erl_ddll:try_load(P,echo_drv,O)|helper_load(P,N-1,O)].
helper_loop()->receive
 {command,Parent,R,{monitor,State}}->V=erl_ddll:monitor(driver,{echo_drv,State}),Parent!{reply,self(),R,V},helper_loop();
 {command,Parent,R,{await,Ref}}->receive {_,Ref,driver,_,_}=V->Parent!{reply,self(),R,V} after 5000->error(helper_notification_timeout) end,helper_loop();
 stop->ok
 end.
probe(permanent_transition_order,P)->
 Loaded=per_load(P,[]),Port=open_named("permanent_drv445"),
 Reload=per_load(P,[{reload,pending_driver},{monitor,pending_driver}]),
 A=per_monitor(permanent_drv445,loaded),B=per_monitor(permanent_drv445,unloaded),C=per_monitor(permanent_drv445,unloaded_only),
 Control=port_control(Port,445,<<>>),remember({permanent_control,Control}),
 Ordered=collect_labels([{reload_request,ref(Reload)},{loaded_monitor,A},{unloaded_monitor,B},{only_monitor,C}]),
 State=per_info(),Follow={per_load(P,[]),per_load(P,[{reload,pending_driver}]),per_unload()},
 M1=per_monitor("permanent_drv445",loaded),N1=wait(M1),M2=per_monitor(permanent_drv445,unloaded),N2=wait(M2),
 M3=per_monitor("permanent_drv445",unloaded_only),N3=wait(M3),BadName=probe_binary_monitor_name(),Dem={demon(A),demon(B),demon(C)},
 closed(Port),{Loaded,summary(Reload),Control,Ordered,State,Follow,[N1,N2,N3],BadName,Dem,per_info()};
probe(reload_missing_recover,P)->reload_failure(P,"missing");
probe(reload_wrongname_recover,P)->reload_failure(P,"wrong");
probe(reload_initfail_recover,P)->reload_failure(P,"initfail");
probe(force_unload_option,P)->force_unload(P,[],[kill_ports,{monitor,pending_driver}]);
probe(force_loadflag_unload,P)->force_unload(P,[{driver_options,[kill_ports]}],[{monitor,pending_driver}]);
probe(force_loadflag_reload,P)->
 load(P,[{driver_options,[kill_ports]}]),Port=opened(),
 R=load(P,[{driver_options,[kill_ports]},{reload,pending_driver},{monitor,pending_driver}]),
 Events=port_driver_events(Port,ref(R)),{summary(R),Events,snap(none)};
probe(force_multiowner_option,P)->
 load(P),H=start_helper(P,1),Port=opened(),
 R=unload([kill_ports,{monitor,pending}]),Echo=echo(Port),Before=snap(H),
 Down=stop_helper(H,stop),After=snap(H),Count=info(port_count),closed(Port),
 {summary(R),Echo,Before,Down,After,Count,wait(ref(R))};
probe(force_loadflag_owner_death,P)->
 Options=[{driver_options,[kill_ports]}],load(P,Options),H=start_helper(P,1,Options),Port=opened(),
 R=mon(echo_drv,unloaded_only),U=unload([]),Echo=echo(Port),Down=stop_helper(H,stop),
 {U,Echo,Down,port_driver_events(Port,R)};
probe(force_reload_pending_otherowner,P)->
 Options=[{driver_options,[kill_ports]}],load(P,Options),H=start_helper(P,1,Options),Port=opened(),
 Refused=load(P,[{driver_options,[kill_ports]},{reload,pending_driver}]),Echo=echo(Port),
 R=load(P,[{driver_options,[kill_ports]},{reload,pending},{monitor,pending}]),
 Events=port_driver_events(Port,ref(R)),{Refused,Echo,summary(R),Events,snap(H)}.
open_named(Name)->Port=open_port({spawn_driver,Name},[binary]),put(ports444,[Port|get(ports444)]),remember({open_port,Port}),Port.
per_load(P,O)->V=erl_ddll:try_load(P,permanent_drv445,O),remember({permanent_load,O,V}),V.
per_unload()->V=erl_ddll:try_unload(permanent_drv445,[]),remember({permanent_unload,V}),V.
per_monitor(Name,Kind)->R=erl_ddll:monitor(driver,{Name,Kind}),remember({permanent_monitor,Name,Kind,R}),R.
per_info()->[{K,per_info_item(K)}||K<-[processes,awaiting_load,awaiting_unload,port_count,driver_options,linked_in_driver,permanent]].
per_info_item(K)->V=erl_ddll:info(permanent_drv445,K),remember({permanent_info,K,V}),V.
collect_labels([])->[];
collect_labels(Refs)->receive {Dir,R,driver,Name,Tag}=M->remember({notification,M}),
 {Label,Remaining}=take_label(R,Refs),[{Label,Dir,Name,Tag}|collect_labels(Remaining)]
 after 5000->error(permanent_monitor_order_timeout) end.
take_label(R,[{L,R}|T])->{L,T};
take_label(R,[H|T])->{L,Rest}=take_label(R,T),{L,[H|Rest]}.
reload_failure(P,Kind)->
 load(P),Port=opened(),R=load(P++"/"++Kind,[{reload,pending_driver},{monitor,pending_driver}]),
 Before=snap(none),closed(Port),Failure=wait(ref(R)),
 Recovery=load(P),New=opened(),Data=echo(New),closed(New),{summary(R),Before,Failure,Recovery,Data,snap(none)}.
echo(Port)->true=port_command(Port,<<45,0,255>>),
 receive {Port,{data,Data}}=M->remember({echo,M}),Data after 5000->error(echo_timeout) end.
force_unload(P,LoadOptions,UnloadOptions)->
 load(P,LoadOptions),Port=opened(),R=unload(UnloadOptions),{summary(R),port_driver_events(Port,ref(R))}.
port_driver_events(P,R)->port_driver_events(P,R,pending,pending).
port_driver_events(_,_,{port,Reason},{driver,Dir,Name,Tag})->{port_exit,Reason,driver_notification,Dir,Name,Tag};
port_driver_events(P,R,PortDone,DriverDone)->receive
 {'EXIT',P,Reason}=M when PortDone=:=pending->remember({port_exit,M}),put(ports444,remove_port(P,get(ports444))),
 port_driver_events(P,R,{port,Reason},DriverDone);
 {Dir,R,driver,Name,Tag}=M when DriverDone=:=pending->remember({notification,M}),
 port_driver_events(P,R,PortDone,{driver,Dir,Name,Tag})
 after 5000->error(force_events_timeout) end.
probe_binary_monitor_name()->
 try erl_ddll:monitor(driver,{<<"permanent_drv445">>,unloaded_only}) of
  R->remember({binary_name_monitor,R}),{value,is_reference(R),wait(R)}
 catch K:Why:Stack->remember({binary_name_monitor_error,K,Why,Stack}),{exception,K,Why,Stack} end.
cleanup_permanent()->cleanup_ports(get(ports444)),cleanup_helpers(get(helpers444)),remember({permanent_retained,per_info()}),ok.
cleanup()->
 cleanup_ports(get(ports444)),cleanup_helpers(get(helpers444)),
 R=mon(echo_drv,unloaded_only),N=cleanup_count(),cleanup_refs(N),wait(R),ok.
cleanup_ports([])->ok;cleanup_ports([P|T])->closed(P),cleanup_ports(T).
cleanup_helpers([])->ok;cleanup_helpers([{P,_}|T])->stop_helper(P,kill),cleanup_helpers(T).
cleanup_count()->
 try erl_ddll:info(echo_drv,processes) of Entries->remember({cleanup_processes,Entries}),count_self(Entries)
 catch error:badarg->remember({cleanup_processes_absent,error,badarg}),0 end.
count_self([])->0;
count_self([{Who,N}|T])->case Who=:=self() of true->N+count_self(T);false->count_self(T) end.
cleanup_refs(0)->ok;
cleanup_refs(N)->unload([]),cleanup_refs(N-1).
