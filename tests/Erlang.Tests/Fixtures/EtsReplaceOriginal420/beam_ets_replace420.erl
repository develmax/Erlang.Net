-module(beam_ets_replace420).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[empty,identity,variable,literal_key,constant_tuple,tuple_key,list_key,element_key,ordered_numeric,duplicate_bag,keypos_two,nested_failure,first_clause,no_match,nil_spec,bag_kind_first,bad_spec,empty_body,several_bodies,wrong_tuple,changed_key,const_variable,wildcard_key,map_key,function_key,wrong_element,invalid_unmatched_clause,missing_table_first,bad_table_type,private_access].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
replace(Options,Rows,Spec)->T=ets:new(replace420,[named_table|Options]),try true=ets:insert(T,Rows),N=ets:select_replace(T,Spec),{N,lists:sort(ets:tab2list(T))} after ets:delete(T) end.
spec(Head,Body)->[{Head,[],[Body]}].
perform(empty)->replace([],[],spec({'$1','_'},{{'$1',changed}}));
perform(identity)->replace([],[{1,a},{2,b}],spec('_','$_'));
perform(variable)->replace([],[{1,a},{2,b}],spec({'$1','_'},{{'$1',changed}}));
perform(literal_key)->replace([],[{1,a},{2,b}],spec({1,'_'},{{1,changed}}));
perform(constant_tuple)->replace([],[{1,a}],spec({1,'_'},{const,{1,changed}}));
perform(tuple_key)->replace([], [{{1,a},old}],spec({{1,'$1'},'_'},{{{{1,'$1'}},changed}}));
perform(list_key)->replace([], [{[1,a],old}],spec({[1,'$1'],'_'},{{[1,'$1'],changed}}));
perform(element_key)->replace([],[{1,a},{2,b}],spec({'$1','_'},{{{element,1,'$_'},changed}}));
perform(ordered_numeric)->replace([ordered_set],[{1.0,a}],spec({1,'_'},{{1,changed}}));
perform(duplicate_bag)->replace([duplicate_bag],[{1,a},{1,a},{1,b}],spec({'$1','_'},{{'$1',changed}}));
perform(keypos_two)->replace([{keypos,2}],[{a,1},{b,2}],spec({'_','$1'},{{changed,'$1'}}));
perform(nested_failure)->replace([],[{1,a}],spec({'$1','_'},{{'$1',{'div',1,0}}}));
perform(first_clause)->replace([],[{1,a},{2,b}],[{{1,'_'},[],[{{1,first}}]},{{'$1','_'},[],[{{'$1',second}}]}]);
perform(no_match)->replace([],[{1,a}],spec({2,'_'},{{2,changed}}));
perform(nil_spec)->replace([],[{1,a}],[]);
perform(bag_kind_first)->replace([bag],[{1,a}],bad);
perform(bad_spec)->replace([],[{1,a}],bad);
perform(empty_body)->replace([],[{1,a}],[{{'$1','_'},[],[]}]);
perform(several_bodies)->replace([],[{1,a}],[{{'$1','_'},[],['$_','$_']}]);
perform(wrong_tuple)->replace([],[{1,a}],spec({'$1','_'},ok));
perform(changed_key)->replace([],[{1,a}],spec({'$1','_'},{{2,changed}}));
perform(const_variable)->replace([],[{1,a}],spec({'$1','_'},{const,{'$1',changed}}));
perform(wildcard_key)->replace([],[{1,a}],spec({'_','_'},{{'_',changed}}));
perform(map_key)->replace([],[{#{key=>1,extra=>2},a}],spec({#{key=>1},'_'},{{#{key=>1},changed}}));
perform(function_key)->replace([],[{1,a}],spec({'$1','_'},{{{'+','$1',0},changed}}));
perform(wrong_element)->replace([],[{1,a}],spec({'$1','_'},{{{element,2,'$_'},changed}}));
perform(invalid_unmatched_clause)->replace([],[{1,a}],[{{1,'_'},[],['$_']},{{99,'_'},[],[{{100,changed}}]}]);
perform(missing_table_first)->ets:select_replace(replace420_missing,bad);
perform(bad_table_type)->ets:select_replace(42,bad);
perform(private_access)->Parent=self(),{Pid,Ref}=spawn_monitor(fun()->T=ets:new(replace420,[named_table,private]),Parent!{ready,T},receive stop->ok end end),receive {ready,T}->try ets:select_replace(T,bad) after Pid!stop,receive {'DOWN',Ref,process,Pid,normal}->ok end end end.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
