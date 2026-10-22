-module(beam_ets_index420).
-export([run/0,run_stacks/0,worker/2,capture/1,fixer/2,unfixer/2,probe/2]).
cases()->[ordered_empty,ordered_slots,hash_partition,set_nested,bag_dead_key,duplicate_bag_dead_key,unfix_other,fixer_down,delete_fixed,invalid_flag,invalid_table,precedence_type,precedence_access,bad_slot_negative,bad_slot_float,bad_slot_large,tree_beyond,hash_beyond,fix_bad_arity,slot_bad_arity].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
cleanup(T)->try ets:delete(T) catch error:badarg->ok end.
new(Kind)->ets:new(index_alpha420,[named_table,public,Kind]).
invalid_next(T)->try ets:next(T,42),false catch error:badarg->true end.
partition(T)->lists:sort(lists:append([ets:slot(T,I)||I<-lists:seq(0,255)])).
nested(Kind)->T=new(Kind),try true=ets:insert(T,{42,a}),true=ets:safe_fixtable(T,true),true=ets:safe_fixtable(T,true),true=ets:delete(T,42),A=ets:next(T,42),true=ets:safe_fixtable(T,false),B=ets:next(T,42),true=ets:safe_fixtable(T,false),{A,B,invalid_next(T)} after cleanup(T) end.
perform(ordered_empty)->T=new(ordered_set),try {ets:slot(T,0),ets:safe_fixtable(T,true),ets:safe_fixtable(T,false),ets:safe_fixtable(T,false)} after cleanup(T) end;
perform(ordered_slots)->T=new(ordered_set),try true=ets:insert(T,[{3,c},{1,a},{2,b}]),{ets:slot(T,0),ets:slot(T,1),ets:slot(T,2),ets:slot(T,3)} after cleanup(T) end;
perform(hash_partition)->T=new(set),try Rows=[{1,a},{2,b},{42,c}],true=ets:insert(T,Rows),{partition(T)=:=lists:sort(Rows),ets:slot(T,256)} after cleanup(T) end;
perform(set_nested)->nested(set);
perform(bag_dead_key)->dead_key(bag);
perform(duplicate_bag_dead_key)->dead_key(duplicate_bag);
perform(unfix_other)->T=new(set),try true=ets:insert(T,{42,a}),true=ets:safe_fixtable(T,true),true=ets:delete(T,42),{Pid,Ref}=spawn_monitor(?MODULE,unfixer,[self(),T]),receive {unfixed,true}->receive {'DOWN',Ref,process,Pid,normal}->ok end end,A=ets:next(T,42),true=ets:safe_fixtable(T,false),{A,invalid_next(T)} after cleanup(T) end;
perform(fixer_down)->T=new(set),try true=ets:insert(T,{42,a}),{Pid,Ref}=spawn_monitor(?MODULE,fixer,[self(),T]),receive fixed->true=ets:delete(T,42),A=ets:next(T,42),Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->{A,invalid_next(T)} end end after cleanup(T) end;
perform(delete_fixed)->T=new(set),true=ets:safe_fixtable(T,true),{ets:delete(T),invalid_next(T)};
perform(invalid_flag)->T=new(set),try ets:safe_fixtable(T,invalid) after cleanup(T) end;
perform(invalid_table)->ets:safe_fixtable(missing_index420,true);
perform(precedence_type)->ets:safe_fixtable({},invalid);
perform(precedence_access)->T=ets:new(index_alpha420,[named_table,private]),try {Pid,Ref}=spawn_monitor(?MODULE,probe,[self(),T]),receive {probed,Value}->receive {'DOWN',Ref,process,Pid,normal}->Value end end after cleanup(T) end;
perform(bad_slot_negative)->T=new(set),try ets:slot(T,-1) after cleanup(T) end;
perform(bad_slot_float)->T=new(set),try ets:slot(T,1.0) after cleanup(T) end;
perform(bad_slot_large)->T=new(set),try ets:slot(T,1 bsl 70) after cleanup(T) end;
perform(tree_beyond)->T=new(ordered_set),try ets:slot(T,1) after cleanup(T) end;
perform(hash_beyond)->T=new(set),try ets:slot(T,257) after cleanup(T) end;
perform(fix_bad_arity)->erlang:apply(ets,safe_fixtable,[bad]);
perform(slot_bad_arity)->erlang:apply(ets,slot,[bad]).
dead_key(Kind)->T=new(Kind),try true=ets:insert(T,[{42,a},{42,b}]),true=ets:safe_fixtable(T,true),true=ets:delete(T,42),Result={ets:lookup(T,42),ets:member(T,42),partition(T),ets:next(T,42)},true=ets:safe_fixtable(T,false),{Result,invalid_next(T)} after cleanup(T) end.
fixer(Parent,T)->true=ets:safe_fixtable(T,true),Parent!fixed,receive stop->ok end.
unfixer(Parent,T)->Parent!{unfixed,ets:safe_fixtable(T,false)}.
probe(Parent,T)->Result=try {value,ets:safe_fixtable(T,invalid)} catch C:R:S->{exception,C,R,S} end,Parent!{probed,Result}.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
