-module(beam_ets_mutations414).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[insert_set,insert_ordered,insert_bag,insert_duplicate,insert_existing,
          insert_batch,insert_batch_conflict,insert_same_key,insert_empty,insert_invalid,insert_improper,
          take_set,take_ordered,take_bag,take_duplicate,take_missing,take_numeric,
          delete_set,delete_ordered,delete_bag,delete_duplicate,delete_numeric,delete_invalid,
          update_tuple,update_list,update_duplicate_position,update_empty,update_missing_invalid,
          update_invalid,update_key_position,update_position,update_big_position,update_improper,
          update_bag,default_missing,default_existing,default_bad,default_bad_bag,default_empty,
          default_invalid_rollback,keypos_update,missing_table].
run()->[{C,observe(C)}||C<-cases()].
run_stacks()->
    Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,run()}|[at_depth(D)||D<-[0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D)->erlang:system_flag(backtrace_depth,D),
    Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(C)->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),C]),
    receive {C,V}->receive {'DOWN',Ref,process,Pid,normal}->V end
    after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,C)->
    Outcome=try probe(C) of V->{returned,V} catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent!{C,Outcome},ok.
probe(missing_table)->ets:update_element(missing_mutations414,1,invalid_update414);
probe(C)->
    Kind=case C of insert_ordered->ordered_set;take_ordered->ordered_set;take_numeric->ordered_set;
                   delete_ordered->ordered_set;delete_numeric->ordered_set;
                   insert_bag->bag;take_bag->bag;delete_bag->bag;update_bag->bag;default_bad_bag->bag;
                   insert_duplicate->duplicate_bag;take_duplicate->duplicate_bag;delete_duplicate->duplicate_bag;_->set end,
    Options=case C of keypos_update->[{keypos,2}];_->[] end,
    T=ets:new(mutation_original414,[named_table,public,Kind|Options]),
    try
        true=ets:insert(T,case C of keypos_update->[{a,1,10},{b,2,20}];_->[{1,10},{2,20}] end),
        %% Keep the exact native failure stack AND the post-error storage state.
        Operation=try operation(C,T) of V->{returned,V} catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
        {Operation,lists:sort(ets:select(T,[{'_',[],['$_']}]))}
    after ets:delete(T) end.
operation(insert_set,T)->ets:insert_new(T,{3,30});
operation(insert_ordered,T)->ets:insert_new(T,{3,30});
operation(insert_bag,T)->ets:insert_new(T,[{3,30},{3,31}]);
operation(insert_duplicate,T)->ets:insert_new(T,[{3,30},{3,30}]);
operation(insert_existing,T)->ets:insert_new(T,{1,99});
operation(insert_batch,T)->ets:insert_new(T,[{3,30},{4,40}]);
operation(insert_batch_conflict,T)->ets:insert_new(T,[{3,30},{1,99},{4,40}]);
operation(insert_same_key,T)->ets:insert_new(T,[{3,30},{3,31}]);
operation(insert_empty,T)->ets:insert_new(T,[]);
operation(insert_invalid,T)->ets:insert_new(T,[{3,30},invalid_object414]);
operation(insert_improper,T)->ets:insert_new(T,[{3,30}|invalid_tail414]);
operation(take_set,T)->ets:take(T,1);
operation(take_ordered,T)->ets:take(T,1);
operation(take_bag,T)->true=ets:insert(T,[{1,11},{1,10}]),lists:sort(ets:take(T,1));
operation(take_duplicate,T)->true=ets:insert(T,[{1,10},{1,10}]),lists:sort(ets:take(T,1));
operation(take_missing,T)->ets:take(T,3);
operation(take_numeric,T)->ets:take(T,1.0);
operation(delete_set,T)->ets:delete_object(T,{1,10});
operation(delete_ordered,T)->ets:delete_object(T,{1,10});
operation(delete_bag,T)->true=ets:insert(T,[{1,11},{1,10}]),ets:delete_object(T,{1,10});
operation(delete_duplicate,T)->true=ets:insert(T,[{1,10},{1,10}]),ets:delete_object(T,{1,10});
operation(delete_numeric,T)->ets:delete_object(T,{1.0,10});
operation(delete_invalid,T)->ets:delete_object(T,invalid_object414);
operation(update_tuple,T)->ets:update_element(T,1,{2,11});
operation(update_list,T)->ets:update_element(T,1,[{2,11}]);
operation(update_duplicate_position,T)->ets:update_element(T,1,[{2,11},{2,12}]);
operation(update_empty,T)->ets:update_element(T,1,[]);
operation(update_missing_invalid,T)->ets:update_element(T,3,invalid_update414);
operation(update_invalid,T)->ets:update_element(T,1,[{2,11},invalid_update414]);
operation(update_key_position,T)->ets:update_element(T,1,{1,1});
operation(update_position,T)->ets:update_element(T,1,{0,11});
operation(update_big_position,T)->ets:update_element(T,1,{1 bsl 100,11});
operation(update_improper,T)->ets:update_element(T,1,[{2,11}|invalid_tail414]);
operation(update_bag,T)->ets:update_element(T,1,{2,11});
operation(default_missing,T)->ets:update_element(T,3,{2,31},{ignored_default_key414,30});
operation(default_existing,T)->ets:update_element(T,1,{2,11},{ignored_default_key414,99});
operation(default_bad,T)->ets:update_element(T,1,{2,11},invalid_default414);
operation(default_bad_bag,T)->ets:update_element(T,1,{2,11},invalid_default414);
operation(default_empty,T)->ets:update_element(T,3,[],{ignored_default_key414,30});
operation(default_invalid_rollback,T)->ets:update_element(T,3,[{2,31},{1,3}],{ignored_default_key414,30});
operation(keypos_update,T)->ets:update_element(T,1,[{1,changed},{3,11}]).
capture([Output])->
    Values=run(),Stacks=run_stacks(),
    ok=prim_file:write_file(Output++"/values.etf",term_to_binary(Values,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(Stacks,[{minor_version,2}])),
    erlang:halt(0).
