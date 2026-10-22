-module(beam_ets_info420).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[fields_set,fields_ordered,fields_bag,fields_duplicate,missing_one,missing_two,bad_type_one,bad_type_two,bad_item,private_fields,renamed,heir_self,memory_empty,memory_growth,memory_tombstone,fixed_nested,fixed_ordered,stats_empty,stats_count,stats_deleted,binary_empty,binary_small,binary_large_properties,binary_bad_table,superuser_protocol,superuser_bad,raw_ordered,raw_hash,raw_fixed_dead,raw_bad_key,raw_missing,binary_key_missing,binary_key_small,binary_key_badtable,plain_reference_type,deleted_tid].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
with(Options,Fun)->T=ets:new(info420,[named_table|Options]),try Fun(T) after ets:delete(T) end.
summary(T,Owner)->[{K,case K of owner->V=:=Owner;id->V=:=ets:whereis(T);memory->is_integer(V) andalso V>0;_->V end}||{K,V}<-ets:info(T)].
fixed(T)->case ets:info(T,safe_fixed) of false->false;{{A,B,C},L}->{is_integer(A) andalso is_integer(B) andalso is_integer(C),L=:=[{self(),2}]} end.
raw(T)->raw(T,erts_internal:ets_raw_first(T),[]).
raw(_T,'$end_of_table',A)->lists:sort(A);
raw(T,K,A)->raw(T,erts_internal:ets_raw_next(T,K),[K|A]).
perform(fields_set)->with([],fun(T)->summary(T,self()) end);
perform(fields_ordered)->with([ordered_set],fun(T)->summary(T,self()) end);
perform(fields_bag)->with([bag],fun(T)->summary(T,self()) end);
perform(fields_duplicate)->with([duplicate_bag],fun(T)->summary(T,self()) end);
perform(missing_one)->ets:info(info420_missing);
perform(missing_two)->ets:info(info420_missing,bad);
perform(bad_type_one)->ets:info(42);
perform(bad_type_two)->ets:info(42,size);
perform(bad_item)->with([],fun(T)->ets:info(T,bad) end);
perform(private_fields)->P=self(),{Pid,Ref}=spawn_monitor(fun()->T=ets:new(info420,[named_table,private]),P!{ready,T},receive stop->ok end end),receive {ready,T}->try summary(T,Pid) after Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->ok end end end;
perform(renamed)->T=ets:new(info420,[named_table]),Id=ets:whereis(T),ets:rename(T,info420_renamed),try {ets:info(T),ets:info(info420_renamed,name),ets:info(info420_renamed,id)=:=Id} after ets:delete(info420_renamed) end;
perform(heir_self)->with([{heir,self(),gift}],fun(T)->ets:info(T,heir)=:=self() end);
perform(memory_empty)->with([],fun(T)->is_integer(ets:info(T,memory)) andalso ets:info(T,memory)>0 end);
perform(memory_growth)->with([],fun(T)->M=ets:info(T,memory),true=ets:insert(T,{key,<<0:524288>>}),ets:info(T,memory)>M end);
perform(memory_tombstone)->with([],fun(T)->true=ets:insert(T,{key,<<0:524288>>}),true=ets:safe_fixtable(T,true),true=ets:delete(T,key),M=ets:info(T,memory),true=ets:safe_fixtable(T,false),ets:info(T,memory)<M end);
perform(fixed_nested)->with([],fun(T)->A=ets:info(T,fixed),true=ets:safe_fixtable(T,true),true=ets:safe_fixtable(T,true),B=fixed(T),{Time,List}=ets:info(T,safe_fixed_monotonic_time),C=is_integer(Time) andalso List=:=[{self(),2}],true=ets:safe_fixtable(T,false),true=ets:safe_fixtable(T,false),{A,B,C,ets:info(T,fixed),ets:info(T,safe_fixed)} end);
perform(fixed_ordered)->with([ordered_set],fun(T)->true=ets:safe_fixtable(T,true),A=ets:info(T,fixed),true=ets:safe_fixtable(T,false),{A,ets:info(T,fixed)} end);
perform(stats_empty)->with([],fun(T)->ets:info(T,stats) end);
perform(stats_count)->with([],fun(T)->true=ets:insert(T,[{1,a},{2,b},{3,c}]),{N,Avg,Real,Expected,Min,Max,Kept,Locks}=ets:info(T,stats),{N>0,Avg*N=:=3.0,Real>=0,Expected>=0,Min=:=0,Max>=1,Kept=:=0,Locks>=1} end);
perform(stats_deleted)->with([],fun(T)->true=ets:insert(T,{1,a}),true=ets:safe_fixtable(T,true),true=ets:delete(T,1),S=ets:info(T,stats),true=ets:safe_fixtable(T,false),element(7,S) end);
perform(binary_empty)->with([],fun(T)->ets:info(T,binary) end);
perform(binary_small)->with([],fun(T)->true=ets:insert(T,{key,<<1,2,3>>}),ets:info(T,binary) end);
perform(binary_large_properties)->with([],fun(T)->true=ets:insert(T,{key,<<0:8192>>}),L=ets:info(T,binary),lists:all(fun({Address,Size,Refs})->is_integer(Address) andalso Address>0 andalso Size>=1024 andalso Refs>=1 end,L) end);
perform(binary_bad_table)->ets:info(info420_missing,binary);
perform(superuser_protocol)->P=self(),{Pid,Ref}=spawn_monitor(fun()->T=ets:new(info420,[named_table,private]),true=ets:insert(T,{key,value}),P!{ready,T},receive stop->ok end end),receive {ready,T}->try A=try ets:lookup(T,key),false catch error:badarg->true end,ok=erts_internal:ets_super_user(true),B=ets:lookup(T,key),ok=erts_internal:ets_super_user(false),C=try ets:lookup(T,key),false catch error:badarg->true end,{A,B,C} after erts_internal:ets_super_user(false),Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->ok end end end;
perform(superuser_bad)->erts_internal:ets_super_user(bad);
perform(raw_ordered)->with([ordered_set],fun(T)->true=ets:insert(T,[{3,c},{1,a},{2,b}]),raw(T) end);
perform(raw_hash)->with([],fun(T)->true=ets:insert(T,[{3,c},{1,a},{2,b}]),raw(T) end);
perform(raw_fixed_dead)->with([],fun(T)->true=ets:insert(T,[{1,a},{2,b}]),true=ets:safe_fixtable(T,true),true=ets:delete(T,1),A=raw(T),true=ets:safe_fixtable(T,false),{A,raw(T)} end);
perform(raw_bad_key)->with([],fun(T)->erts_internal:ets_raw_next(T,99) end);
perform(raw_missing)->erts_internal:ets_raw_first(info420_missing);
perform(binary_key_missing)->with([],fun(T)->erts_internal:ets_lookup_binary_info(T,99) end);
perform(binary_key_small)->with([],fun(T)->true=ets:insert(T,{key,<<1,2,3>>}),erts_internal:ets_lookup_binary_info(T,key) end);
perform(binary_key_badtable)->erts_internal:ets_lookup_binary_info(info420_missing,key);
perform(plain_reference_type)->try ets:info(make_ref()),false catch error:badarg->true end;
perform(deleted_tid)->T=ets:new(info420,[]),true=ets:delete(T),ets:info(T).
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
