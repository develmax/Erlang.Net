-module(beam_ets_queries414).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[match_set,match_ordered,match_bag,match_duplicate,match_pattern,match_all,
          count_true,count_false,count_other,count_clauses,count_exit,count_empty,
          reverse_ordered,reverse_hash,reverse_bag,tab2list,invalid_count,invalid_reverse,
          malformed_count,missing_count,missing_reverse,missing_match,wrong_id,pattern_variable].
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
whole()->[{'_',[],['$_']}].
probe(missing_count)->ets:select_count(missing_queries414,invalid_specification414);
probe(missing_reverse)->ets:select_reverse(missing_queries414,invalid_specification414);
probe(missing_match)->ets:match_object(missing_queries414,'_');
probe(wrong_id)->ets:select_count(42,[]);
probe(C)->
    Kind=case C of match_ordered->ordered_set;reverse_ordered->ordered_set;
                   match_bag->bag;reverse_bag->bag;match_duplicate->duplicate_bag;_->set end,
    T=ets:new(query_original414,[named_table,public,Kind]),
    try
        true=ets:insert(T,[{3,30},{1,10},{2,20}]),
        case C of
            match_set->lists:sort(ets:match_object(T,'_'));
            match_ordered->ets:match_object(T,'_');
            match_bag->true=ets:insert(T,[{1,11},{1,10}]),lists:sort(ets:match_object(T,'_'));
            match_duplicate->true=ets:insert(T,[{1,10},{1,10}]),lists:sort(ets:match_object(T,'_'));
            match_pattern->ets:match_object(T,{2,'_'});
            match_all->ets:match_object(T,{'$1',999});
            count_true->ets:select_count(T,[{'_',[],[true]}]);
            count_false->ets:select_count(T,[{'_',[],[false]}]);
            count_other->ets:select_count(T,[{'_',[],[42]}]);
            count_clauses->ets:select_count(T,[{{1,'_'},[],[false]},{'_',[],[true]}]);
            count_exit->ets:select_count(T,[{'_',[],[{'div',1,0}]}]);
            count_empty->ets:select_count(T,[]);
            reverse_ordered->ets:select_reverse(T,[{{'$1','_'},[],['$1']}]);
            reverse_hash->{lists:sort(ets:select_reverse(T,whole())),
                           ets:select(T,whole())=:=ets:select_reverse(T,whole())};
            reverse_bag->true=ets:insert(T,[{1,11},{1,10}]),lists:sort(ets:select_reverse(T,whole()));
            tab2list->lists:sort(ets:tab2list(T));
            invalid_count->ets:select_count(T,invalid_specification414);
            invalid_reverse->ets:select_reverse(T,invalid_specification414);
            malformed_count->ets:select_count(T,[{'_',invalid_guards414,[true]}]);
            pattern_variable->lists:sort(ets:match_object(T,{'$1','$2'}))
        end
    after ets:delete(T) end.
capture([Output])->
    Values=run(),Stacks=run_stacks(),
    ok=prim_file:write_file(Output++"/values.etf",term_to_binary(Values,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(Stacks,[{minor_version,2}])),
    erlang:halt(0).
