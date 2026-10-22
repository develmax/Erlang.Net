-module(beam_ets_match411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [logger_handlers,application_master,application_env,ordered_values,sparse_vars,repeat_equal,repeat_unequal,wildcard_rows,whole_variable,literal_found,literal_missing,empty_table,nil_pattern,atom_pattern,map_subset,map_repeated,map_variable_key,map_wildcard_key,missing_table_bad_pattern,invalid_table_type,private_access,private_bad_pattern,protected_read,bag_values,duplicate_values,improper_list,dollar_literal,leading_zero_literal,signed_zero,integer_float_distinct,copied_fun].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D) || D<-[default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C) -> V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
setup(Opts,Rows) -> T=ets:new(ets_match411,[named_table|Opts]),true=ets:insert(T,Rows),T.
other(F) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(fun()->V=try F() of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V} end),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->case V of {value,X}->X;{caught,K,E,S}->erlang:raise(K,E,S) end end end.
probe(logger_handlers) -> setup([], [{{'$handler_config$',handler},10},{primary,20}]),ets:match(ets_match411,{{'$handler_config$','_'},'$1'});
probe(application_master) -> setup([], [{{application_master,app},42}]),ets:match(ets_match411,{{application_master,'$1'},42});
probe(application_env) -> setup([], [{{env,app,key},10}]),ets:match(ets_match411,{{env,app,'$1'},'$2'});
probe(ordered_values) -> setup([ordered_set], [{3,30},{1,10},{2,20}]),ets:match(ets_match411,{'$2','$1'});
probe(sparse_vars) -> setup([], [{10,20,10,30}]),ets:match(ets_match411,{'$7','$2','$7','$0'});
probe(repeat_equal) -> setup([], [{10,10}]),ets:match(ets_match411,{'$1','$1'});
probe(repeat_unequal) -> setup([], [{10,20}]),ets:match(ets_match411,{'$1','$1'});
probe(wildcard_rows) -> setup([ordered_set], [{2,20},{1,10}]),ets:match(ets_match411,'_');
probe(whole_variable) -> setup([], [{key,10}]),ets:match(ets_match411,'$1');
probe(literal_found) -> setup([], [{key,10}]),ets:match(ets_match411,{key,10});
probe(literal_missing) -> setup([], [{key,10}]),ets:match(ets_match411,{key,11});
probe(empty_table) -> setup([], []),ets:match(ets_match411,{'$1','$2'});
probe(nil_pattern) -> setup([], [{key,10}]),ets:match(ets_match411,[]);
probe(atom_pattern) -> setup([], [{key,10}]),ets:match(ets_match411,not_a_tuple);
probe(map_subset) -> setup([], [{key,#{a=>10,b=>20}}]),ets:match(ets_match411,{key,#{a=>'$1'}});
probe(map_repeated) -> setup([], [{key,#{a=>10,b=>10}}]),ets:match(ets_match411,{key,#{a=>'$1',b=>'$1'}});
probe(map_variable_key) -> setup([], []),ets:match(ets_match411,{key,#{'$1'=>10}});
probe(map_wildcard_key) -> setup([], []),ets:match(ets_match411,{key,#{'_'=>10}});
probe(missing_table_bad_pattern) -> ets:match(ets_missing_match411,{key,#{'$1'=>10}});
probe(invalid_table_type) -> ets:match(42,{key,#{'$1'=>10}});
probe(private_access) -> setup([private], [{key,10}]),other(fun()->ets:match(ets_match411,{key,'$1'}) end);
probe(private_bad_pattern) -> setup([private], [{key,10}]),other(fun()->ets:match(ets_match411,{key,#{'$1'=>10}}) end);
probe(protected_read) -> setup([protected], [{key,10}]),other(fun()->ets:match(ets_match411,{key,'$1'}) end);
probe(bag_values) -> setup([bag], [{key,10},{key,20},{key,10}]),ets:match(ets_match411,{key,'$1'});
probe(duplicate_values) -> setup([duplicate_bag], [{key,10},{key,10},{key,20}]),ets:match(ets_match411,{key,'$1'});
probe(improper_list) -> setup([], [{key,[10|tail]}]),ets:match(ets_match411,{key,['$1'|'$2']});
probe(dollar_literal) -> setup([], [{key,'$$'}]),ets:match(ets_match411,{key,'$$'});
probe(leading_zero_literal) -> setup([], [{key,'$01'}]),ets:match(ets_match411,{key,'$01'});
probe(signed_zero) -> setup([], [{key,-0.0}]),ets:match(ets_match411,{key,0.0});
probe(integer_float_distinct) -> setup([], [{key,1}]),ets:match(ets_match411,{key,1.0});
probe(copied_fun) -> N=40,setup([], [{key,fun(X)->X+N end}]),[[F]]=ets:match(ets_match411,{key,'$1'}),ets:delete(ets_match411),F(2).
