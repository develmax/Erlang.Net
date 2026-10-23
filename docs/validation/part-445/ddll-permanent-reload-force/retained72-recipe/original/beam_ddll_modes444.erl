-module(beam_ddll_modes444).
-export([cases/0,capture/1,capture_stacks/1,write_values/1,write_stacks/1,observer_child/4,helper/3]).
cases()->[duplicate_ref_unload,duplicate_ref_reload_pending_driver,foreign_owner_unload_reload,
 unload_monitor_pending,unload_monitor_pending_driver,unload_cancel_regular_vs_only,
 loaded_monitor_during_unload,normal_load_during_reload,reload_repeat_while_pending,
 reload_cancel_by_final_unloads,foreign_demonitor_cannot_remove,monitor_owner_death].
capture(Path)->split([{C,observe(C,Path,default)} || C<-cases()]).
split(Rows)->{[{C,V} || {C,{V,_}}<-Rows],[{C,O} || {C,{_,O}}<-Rows]}.
capture_stacks(Path)->
 Profiles=[profile(Path,D)||D<-[default,0,1,2,8]],
 {[{D,E,V} || {D,E,{V,_}}<-Profiles],[{D,E,O} || {D,E,{_,O}}<-Profiles]}.
profile(Path,default)->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
 {default,Old,split([{C,observe(C,Path,default)}||C<-cases()])};
profile(Path,D)->Old=erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),
 try {D,Effective,split([{C,observe(C,Path,D)}||C<-cases()])} after erlang:system_flag(backtrace_depth,Old) end.
observe(C,Path,D)->
 {P,R}=spawn_monitor(?MODULE,observer_child,[self(),C,Path,D]),
 receive {P,V}->receive {'DOWN',R,process,P,normal}->V;{'DOWN',R,process,P,Why}->error({reported_then_down,C,Why}) end;
 {'DOWN',R,process,P,Why}->error({observer_down,C,Why})
 after 15000->exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,error({observer_timeout,C}) end.
observer_child(Parent,C,Path,D)->
 process_flag(trap_exit,true),put(own444,[]),put(ports444,[]),put(helpers444,[]),remember({observer,self()}),
 case D of default->ok;_->erlang:system_flag(backtrace_depth,D) end,
 Value=try probe(C,Path) of X->{value,X} catch K:R:S->{exception,K,R,S} end,
 cleanup(),Own=reverse(get(own444)),Parent!{self(),{Value,Own}}.
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
start_helper(Path,Loads)->
 {P,R}=spawn_monitor(?MODULE,helper,[self(),Path,Loads]),put(helpers444,[{P,R}|get(helpers444)]),
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
helper(Parent,Path,Loads)->Results=helper_load(Path,Loads),Parent!{ready,self(),Results},helper_loop().
helper_load(_,0)->[];helper_load(P,N)->[erl_ddll:try_load(P,echo_drv,[])|helper_load(P,N-1)].
helper_loop()->receive
 {command,Parent,R,{monitor,State}}->V=erl_ddll:monitor(driver,{echo_drv,State}),Parent!{reply,self(),R,V},helper_loop();
 {command,Parent,R,{await,Ref}}->receive {_,Ref,driver,_,_}=V->Parent!{reply,self(),R,V} after 5000->error(helper_notification_timeout) end,helper_loop();
 stop->ok
 end.
probe(duplicate_ref_unload,P)->
 load(P),load(P),Port=opened(),First=unload([]),S=snap(none),Last=unload([{monitor,pending_driver}]),
 closed(Port),{First,S,summary(Last),wait(ref(Last))};
probe(duplicate_ref_reload_pending_driver,P)->load(P),load(P),{load(P,[{reload,pending_driver}]),snap(none)};
probe(foreign_owner_unload_reload,P)->
 H=start_helper(P,1),{unload([]),load(P,[{reload,pending}]),load(P,[{reload,pending_driver}]),snap(H)};
probe(unload_monitor_pending,P)->
 load(P),load(P),Port=opened(),First=unload([{monitor,pending}]),S=snap(none),Last=unload([]),
 closed(Port),{summary(First),S,Last,wait(ref(First))};
probe(unload_monitor_pending_driver,P)->
 load(P),load(P),Port=opened(),First=unload([{monitor,pending_driver}]),S=snap(none),Last=unload([{monitor,pending_driver}]),
 closed(Port),{summary(First),S,summary(Last),wait(ref(Last))};
probe(unload_cancel_regular_vs_only,P)->
 load(P),Port=opened(),R=mon(echo_drv,unloaded),Only=mon(echo_drv,unloaded_only),U=unload([]),L=load(P),
 Cancel=wait(R),S=snap(none),Final=unload([]),closed(Port),{U,L,Cancel,S,Final,wait(Only)};
probe(loaded_monitor_during_unload,P)->
 load(P),opened(),U=unload([]),R=mon("echo_drv",loaded),{U,wait(R),snap(none)};
probe(normal_load_during_reload,P)->
 load(P),Port=opened(),A=load(P,[{reload,pending_driver},{monitor,pending_driver}]),B=load(P),
 S=snap(none),closed(Port),Notifications=wait_two(ref(A),ref(B)),{summary(A),summary(B),S,Notifications,snap(none)};
probe(reload_repeat_while_pending,P)->
 load(P),Port=opened(),A=load(P,[{reload,pending_driver},{monitor,pending_driver}]),
 B=load(P,[{reload,pending}]),C=load(P,[{reload,pending_driver}]),S=snap(none),
 closed(Port),{summary(A),B,C,S,wait(ref(A))};
probe(reload_cancel_by_final_unloads,P)->
 load(P),Port=opened(),A=load(P,[{reload,pending_driver},{monitor,pending}]),U=unload([{monitor,pending_driver}]),
 Cancel=wait(ref(A)),S=snap(none),closed(Port),{summary(A),summary(U),Cancel,S,wait(ref(U))};
probe(foreign_demonitor_cannot_remove,P)->
 load(P),Port=opened(),H=start_helper(P,0),R=ask(H,{monitor,unloaded}),Dem=demon(R),S=snap(H),U=unload([]),closed(Port),
 Message=ask(H,{await,R}),remember({foreign_notification,Message}),{Dir,R,driver,Name,Tag}=Message,
 Down=stop_helper(H,stop),{Dem,S,U,{Dir,Name,Tag},Down};
probe(monitor_owner_death,P)->
 load(P),Port=opened(),H=start_helper(P,0),R=ask(H,{monitor,unloaded}),Before=snap(H),
 Down=stop_helper(H,kill),After=snap(H),U=unload([{monitor,pending_driver}]),closed(Port),
 {is_reference(R),Before,Down,After,summary(U),wait(ref(U))}.
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
write_values([Out,Path])->{V,O}=capture(Path),
 ok=file:write_file(Out++"/values.etf",term_to_binary(V,[{minor_version,2}])),
 ok=file:write_file(Out++"/own-values.etf",term_to_binary(O,[{minor_version,2}])),halt().
write_stacks([Out,Path])->{V,O}=capture_stacks(Path),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(V,[{minor_version,2}])),
 ok=file:write_file(Out++"/own-stacks.etf",term_to_binary(O,[{minor_version,2}])),halt().
