-module(beam_ets_delete412).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[set,ordered_set,bag,duplicate_bag,wildcard,no_match,false_body,empty_spec,
          holder,invalid_spec,invalid_pattern,missing_table].
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
probe(missing_table)->ets:internal_select_delete(missing_deletion412,invalid_specification412);
probe(C)->
    Kind=case C of ordered_set->ordered_set;bag->bag;duplicate_bag->duplicate_bag;_->set end,
    T=ets:new(deletion_original412,[named_table,public,Kind]),
    try
        true=ets:insert(T,[{1,remove},{2,keep},{3,remove}]),
        Result=case C of
            wildcard->ets:match_delete(T,'_');
            no_match->ets:match_delete(T,{'_',missing});
            false_body->ets:internal_select_delete(T,[{'_',[],[false]}]);
            empty_spec->ets:internal_select_delete(T,[]);
            holder->ets:internal_delete_all(T,{retained_holder412,123});
            invalid_spec->ets:internal_select_delete(T,invalid_specification412);
            invalid_pattern->ets:match_delete(T,{'$0','$1'});
            _->ets:match_delete(T,{'_',remove})
        end,
        {Result,lists:sort(ets:select(T,[{'_',[],['$_']}]))}
    after ets:delete(T) end.
capture([Output])->
    %% Original oracle must resolve installed original ets, never generated partial facet.
    Values=run(),Stacks=run_stacks(),
    ok=prim_file:write_file(Output++"/values.etf",term_to_binary(Values,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(Stacks,[{minor_version,2}])),
    erlang:halt(0).
