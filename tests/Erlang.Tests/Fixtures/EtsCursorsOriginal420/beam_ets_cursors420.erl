-module(beam_ets_cursors420).
-export([run/0,run_stacks/0,worker/2,capture/1,probe/2]).
cases()->[eot_aliases,select_chunks,match_chunks,match_object_chunks,reverse_chunks,bound_select,partial_select,hash_overshoot,raw_hash_zero,raw_hash_negative,raw_hash_buffered,raw_hash_seven,raw_hash_improper_zero,raw_tree_zero,raw_tree_negative,raw_tree_reverse,raw_tree_got,raw_tree_end,named_cursor_identity,empty_spec,bad_non_tuple,bad_empty_tuple,missing_table_first,bad_table_type,bad_limit_zero,bad_limit_negative,bad_limit_big,bad_program,hash_bad_arity,hash_bad_slot,hash_bad_chunk,hash_bad_rest,hash_bad_count,hash_bad_magic,tree_bad_arity,tree_bad_chunk,tree_bad_reverse,tree_bad_rest,tree_bad_count,tree_bad_magic,private_precedence].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
cleanup(T)->try ets:delete(T) catch error:badarg->ok end.
new(Kind)->ets:new(cursor_alpha420,[named_table,public,Kind]).
rows()->[{1,a},{2,b},{3,c}].
ms()->[{'_',[],['$_']}].
program()->ets:match_spec_compile(ms()).
with(Kind,Fun)->T=new(Kind),try true=ets:insert(T,rows()),Fun(T) after cleanup(T) end.
consume('$end_of_table',_)->[];
consume({Batch,Next},Alias)->[Batch|consume(erlang:apply(ets,Alias,[Next]),Alias)].
perform(eot_aliases)->{ets:select('$end_of_table'),ets:select_reverse('$end_of_table'),ets:match('$end_of_table'),ets:match_object('$end_of_table')};
perform(select_chunks)->with(ordered_set,fun(T)->consume(ets:select(T,ms(),2),select) end);
perform(match_chunks)->with(ordered_set,fun(T)->consume(ets:match(T,{'$1','_'},2),match_object) end);
perform(match_object_chunks)->with(ordered_set,fun(T)->consume(ets:match_object(T,{'_','_'},2),match) end);
perform(reverse_chunks)->with(ordered_set,fun(T)->consume(ets:select_reverse(T,ms(),2),select_reverse) end);
perform(bound_select)->with(ordered_set,fun(T)->ets:select(T,[{{2,'_'},[],['$_']}],1) end);
perform(partial_select)->T=new(ordered_set),try true=ets:insert(T,[{{a,1},x},{{a,2},y},{{b,1},z}]),consume(ets:select(T,[{{{a,'_'},'_'},[],['$_']}],1),select) after cleanup(T) end;
perform(hash_overshoot)->T=new(duplicate_bag),try true=ets:insert(T,[{a,1},{a,2},{a,3}]),consume(ets:select(T,ms(),1),select_reverse) after cleanup(T) end;
perform(raw_hash_zero)->with(set,fun(T)->lists:sort(ets:select({T,0,0,program(),[marker],1})) end);
perform(raw_hash_negative)->with(set,fun(T)->{ets:select({T,-2,1,program(),[marker],1}),ets:match({T,-1,1,program(),[],0})} end);
perform(raw_hash_buffered)->with(set,fun(T)->consume(ets:select({T,-1,1,program(),[a,b,c],3}),select) end);
perform(raw_hash_seven)->with(set,fun(T)->ets:select_reverse({T,-1,1,program(),[marker],1,2}) end);
perform(raw_hash_improper_zero)->with(set,fun(T)->ets:match_object({T,-1,0,program(),[a|tail],0}) end);
perform(raw_tree_zero)->with(ordered_set,fun(T)->ets:select({T,2,[],0,program(),[marker],0,0}) end);
perform(raw_tree_negative)->with(ordered_set,fun(T)->consume(ets:select({T,0,[],-1,program(),[],0,0}),select) end);
perform(raw_tree_reverse)->with(ordered_set,fun(T)->consume(ets:match({T,4,[],2,program(),[],1,0}),select_reverse) end);
perform(raw_tree_got)->with(ordered_set,fun(T)->consume(ets:match_object({T,0,[],1,program(),[marker],0,1}),select) end);
perform(raw_tree_end)->T=new(ordered_set),try true=ets:insert(T,[{{a,1},x},{{a,2},y},{{b,1},z}]),consume(ets:select({T,{a,0},{a,'_'},5,program(),[],0,0}),select) after cleanup(T) end;
perform(named_cursor_identity)->T=new(ordered_set),try true=ets:insert(T,rows()),{_,C}=ets:select(T,ms(),1),Tree={tuple_size(C),element(1,C)=:=T,element(2,C),element(3,C),element(4,C),ets:is_compiled_ms(element(5,C)),element(6,C),element(7,C),element(8,C)},cleanup(T),U=new(duplicate_bag),true=ets:insert(U,[{a,1},{a,2}]),{_,H}=ets:select(U,ms(),1),Hash={tuple_size(H),element(1,H)=:=ets:whereis(U),element(2,H),element(3,H),ets:is_compiled_ms(element(4,H)),element(5,H),element(6,H)},{Tree,Hash} after cleanup(cursor_alpha420) end;
perform(empty_spec)->with(ordered_set,fun(T)->{ets:select(T,[],1),ets:select_reverse(T,[],1)} end);
perform(bad_non_tuple)->ets:select(bad);
perform(bad_empty_tuple)->ets:select({});
perform(missing_table_first)->ets:select({missing_cursor420});
perform(bad_table_type)->ets:match_object({{},bad});
perform(bad_limit_zero)->with(set,fun(T)->ets:select(T,ms(),0) end);
perform(bad_limit_negative)->with(set,fun(T)->ets:select_reverse(T,ms(),-1) end);
perform(bad_limit_big)->with(set,fun(T)->ets:match(T,'_',1 bsl 70) end);
perform(bad_program)->with(set,fun(T)->ets:select(T,bad,1) end);
perform(hash_bad_arity)->with(set,fun(T)->ets:select({T,0,1}) end);
perform(hash_bad_slot)->with(set,fun(T)->ets:match({T,bad,1,bad,[],0}) end);
perform(hash_bad_chunk)->with(set,fun(T)->ets:match_object({T,0,-1,bad,[],0}) end);
perform(hash_bad_rest)->with(set,fun(T)->ets:select_reverse({T,0,1,bad,bad,0}) end);
perform(hash_bad_count)->with(set,fun(T)->ets:select({T,0,1,bad,[],-1}) end);
perform(hash_bad_magic)->with(set,fun(T)->ets:select({T,0,1,bad,[],0}) end);
perform(tree_bad_arity)->with(ordered_set,fun(T)->ets:select({T,0,1,bad,[],0}) end);
perform(tree_bad_chunk)->with(ordered_set,fun(T)->ets:match({T,0,[],bad,bad,[],0,0}) end);
perform(tree_bad_reverse)->with(ordered_set,fun(T)->ets:match_object({T,0,[],1,bad,[],bad,0}) end);
perform(tree_bad_rest)->with(ordered_set,fun(T)->ets:select_reverse({T,0,[],1,bad,bad,0,0}) end);
perform(tree_bad_count)->with(ordered_set,fun(T)->ets:select({T,0,[],1,bad,[],0,bad}) end);
perform(tree_bad_magic)->with(ordered_set,fun(T)->ets:select({T,0,[],1,bad,[],0,0}) end);
perform(private_precedence)->T=ets:new(cursor_alpha420,[named_table,private]),try {Pid,Ref}=spawn_monitor(?MODULE,probe,[self(),T]),receive {probed,V}->receive {'DOWN',Ref,process,Pid,normal}->V end end after cleanup(T) end.
probe(Parent,T)->Value=try {value,ets:select({T,bad})} catch C:R:S->{exception,C,R,S} end,Parent!{probed,Value}.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
