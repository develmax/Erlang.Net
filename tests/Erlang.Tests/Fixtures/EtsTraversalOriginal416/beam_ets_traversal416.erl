-module(beam_ets_traversal416).
-export([run/0,run_stacks/0,worker/2,capture/1]).
definitions()->[{ordered_first,first,ordered,1},{ordered_last,last,ordered,1},{ordered_next,next,ordered,2},{ordered_prev,prev,ordered,2},{ordered_first_lookup,first_lookup,ordered,1},{ordered_last_lookup,last_lookup,ordered,1},{ordered_next_lookup,next_lookup,ordered,2},{ordered_prev_lookup,prev_lookup,ordered,2},{empty_first,first,empty,1},{empty_last,last,empty,1},{empty_next,next,empty,2},{empty_prev,prev,empty,2},{empty_first_lookup,first_lookup,empty,1},{empty_last_lookup,last_lookup,empty,1},{empty_next_lookup,next_lookup,empty,2},{empty_prev_lookup,prev_lookup,empty,2},{missing_first,first,missing,1},{missing_last,last,missing,1},{missing_next,next,missing,2},{missing_prev,prev,missing,2},{missing_first_lookup,first_lookup,missing,1},{missing_last_lookup,last_lookup,missing,1},{missing_next_lookup,next_lookup,missing,2},{missing_prev_lookup,prev_lookup,missing,2},{wrong_type_first,first,wrong_type,1},{wrong_type_last,last,wrong_type,1},{wrong_type_next,next,wrong_type,2},{wrong_type_prev,prev,wrong_type,2},{wrong_type_first_lookup,first_lookup,wrong_type,1},{wrong_type_last_lookup,last_lookup,wrong_type,1},{wrong_type_next_lookup,next_lookup,wrong_type,2},{wrong_type_prev_lookup,prev_lookup,wrong_type,2},{numeric_next,next,numeric,2},{numeric_prev,prev,numeric,2},{numeric_next_lookup,next_lookup,numeric,2},{numeric_prev_lookup,prev_lookup,numeric,2},{gap_next,next,gap,2},{gap_prev,prev,gap,2},{gap_next_lookup,next_lookup,gap,2},{gap_prev_lookup,prev_lookup,gap,2},{sentinel_next,next,sentinel,2},{sentinel_prev,prev,sentinel,2},{sentinel_next_lookup,next_lookup,sentinel,2},{sentinel_prev_lookup,prev_lookup,sentinel,2},{walk_set,first,set,1},{walk_bag,first,bag,1},{walk_duplicate_bag,first,duplicate_bag,1},{keypos_first_lookup,first_lookup,keypos,1},{compound_next_lookup,next_lookup,compound,2},{stored_eot_first_lookup,first_lookup,stored_eot,1}].
cases()->[C||{C,_,_,_}<-definitions()].
run()->[{C,observe(C)}||C<-cases()].
run_stacks()->
 Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
 try [{default,Old,run()}|[at_depth(D)||D<-[0,1,2,8]]]
 after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(C)->
 {Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),C]),
 receive {C,V}->receive {'DOWN',Ref,process,Pid,normal}->V end
 after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,C)->
 Outcome=try probe(C) of V->{returned,V} catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
 Parent!{C,Outcome},ok.
probe(C)->
 {C,F,Mode,Arity}=lists:keyfind(C,1,definitions()),
 case Mode of
  missing->invoke(F,Arity,missing_traversal416,2);
  wrong_type->invoke(F,Arity,42,2);
  _->
   Kind=case Mode of set->set;bag->bag;duplicate_bag->duplicate_bag;_->ordered_set end,
   Options=case Mode of keypos->[{keypos,2}];_->[] end,
   T=ets:new(traversal_original416,[named_table,public,Kind|Options]),
   try
    Rows=case Mode of empty->[];keypos->[{a,3},{b,1},{c,2}];compound->[{[1],a},{[3],c}];stored_eot->[{'$end_of_table',value}];_->[{3,30},{1,10},{2,20}] end,
    true=ets:insert(T,Rows),
    case Mode of
     set->walk_contract(T);
     bag->true=ets:insert(T,[{1,11},{1,10}]),walk_contract(T);
     duplicate_bag->true=ets:insert(T,[{1,10},{1,10}]),walk_contract(T);
     _->Key=case Mode of numeric->2.0;gap->2.5;sentinel->'$end_of_table';compound->[2];_->2 end,
        invoke(F,Arity,T,Key)
    end
   after ets:delete(T) end
 end.
invoke(F,1,T,_)->apply(ets,F,[T]);
invoke(F,2,T,K)->apply(ets,F,[T,K]).
walk_contract(T)->
 Keys=walk(T,ets:first(T),[]),
 Lookup=walk_lookup(T,ets:first_lookup(T),[]),
 Reverse=walk_previous(T,ets:last(T),[]),
 {lists:sort(Keys),lists:sort(Lookup),Keys=:=Reverse,
  ets:first(T)=:=ets:last(T),ets:first_lookup(T)=:=ets:last_lookup(T)}.
walk(_, '$end_of_table', Acc)->lists:reverse(Acc);
walk(T,K,Acc)->walk(T,ets:next(T,K),[K|Acc]).
walk_previous(_, '$end_of_table', Acc)->lists:reverse(Acc);
walk_previous(T,K,Acc)->walk_previous(T,ets:prev(T,K),[K|Acc]).
walk_lookup(_, '$end_of_table', Acc)->Acc;
walk_lookup(T,{K,Objects},Acc)->
 true=lists:sort(Objects)=:=lists:sort(ets:lookup(T,K)),
 true=ets:next_lookup(T,K)=:=ets:prev_lookup(T,K),
 walk_lookup(T,ets:next_lookup(T,K),Objects++Acc).
capture([Output])->
 V=run(),S=run_stacks(),
 ok=prim_file:write_file(Output++"/values.etf",term_to_binary(V,[{minor_version,2}])),
 ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(S,[{minor_version,2}])),erlang:halt(0).
