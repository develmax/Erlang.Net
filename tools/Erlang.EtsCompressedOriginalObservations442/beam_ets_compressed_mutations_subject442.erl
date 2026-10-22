-module(beam_ets_compressed_mutations_subject442).
-export([cases/0,own_cases/0,worker/2,resume_worker/3]).

cases()->[
 insert_new_set,insert_new_ordered,insert_new_bag,insert_new_duplicate,
 insert_new_dups_set,insert_new_dups_ordered,insert_new_dups_bag,insert_new_dups_duplicate,
 counter_default_set,counter_default_ordered,counter_default_keypos,
 counter_limits_set,counter_limits_ordered,counter_big_limits,
 counter_atomic_error,counter_key_error,counter_bad_default,counter_bag_error,counter_duplicate_error,
 replace_set,replace_ordered,replace_keypos,replace_partial,replace_changed_key,
 replace_bad_spec,replace_bag_error,replace_duplicate_error,
 fix_delete_set,fix_delete_ordered,fix_delete_bag,fix_delete_duplicate,
 select_chunks_set,select_chunks_ordered,select_chunks_bag,select_chunks_duplicate,
 match_chunks_set,match_chunks_ordered,match_chunks_bag,match_chunks_duplicate,
 object_chunks_set,object_chunks_ordered,object_chunks_bag,object_chunks_duplicate,
 cursor_mutation_ordered,cursor_keypos_ordered,cursor_cross_actor,
 cursor_foreign_table,cursor_roundtrip,cursor_stale,
 cursor_zero_limit,cursor_negative_limit,cursor_bad_limit,cursor_bad_shape,cursor_end].

own_cases()->[
 fix_delete_set,fix_delete_ordered,fix_delete_bag,fix_delete_duplicate,
 select_chunks_set,select_chunks_ordered,select_chunks_bag,select_chunks_duplicate,
 match_chunks_set,match_chunks_ordered,match_chunks_bag,match_chunks_duplicate,
 object_chunks_set,object_chunks_ordered,object_chunks_bag,object_chunks_duplicate,
 cursor_mutation_ordered,cursor_keypos_ordered,cursor_cross_actor,
 cursor_foreign_table,cursor_roundtrip,cursor_stale].

worker(Parent,Case)->
 put(compressed_mutations_own442,[]),
 Outcome=try probe(Case) of Value->{returned,Value} catch Kind:Reason:Stack->{raised,Kind,Reason,Stack} end,
 Own=reverse(get(compressed_mutations_own442)),
 Parent!{Case,Outcome,Own}.

probe(insert_new_set)->table(set,1,insert_new);
probe(insert_new_ordered)->table(ordered_set,1,insert_new);
probe(insert_new_bag)->table(bag,1,insert_new);
probe(insert_new_duplicate)->table(duplicate_bag,1,insert_new);
probe(insert_new_dups_set)->table(set,1,input_duplicates);
probe(insert_new_dups_ordered)->table(ordered_set,1,input_duplicates);
probe(insert_new_dups_bag)->table(bag,1,input_duplicates);
probe(insert_new_dups_duplicate)->table(duplicate_bag,1,input_duplicates);
probe(counter_default_set)->table(set,1,counter_default);
probe(counter_default_ordered)->table(ordered_set,1,counter_default);
probe(counter_default_keypos)->table(set,2,counter_keypos);
probe(counter_limits_set)->table(set,1,counter_limits);
probe(counter_limits_ordered)->table(ordered_set,1,counter_limits);
probe(counter_big_limits)->table(set,1,counter_big);
probe(counter_atomic_error)->table(set,1,counter_atomic);
probe(counter_key_error)->table(set,1,counter_key);
probe(counter_bad_default)->table(set,1,counter_bad_default);
probe(counter_bag_error)->table(bag,1,counter_kind_error);
probe(counter_duplicate_error)->table(duplicate_bag,1,counter_kind_error);
probe(replace_set)->table(set,1,replace);
probe(replace_ordered)->table(ordered_set,1,replace);
probe(replace_keypos)->table(ordered_set,2,replace_keypos);
probe(replace_partial)->table(set,1,replace_partial);
probe(replace_changed_key)->table(set,1,replace_changed_key);
probe(replace_bad_spec)->table(set,1,replace_bad_spec);
probe(replace_bag_error)->table(bag,1,replace_kind_error);
probe(replace_duplicate_error)->table(duplicate_bag,1,replace_kind_error);
probe(fix_delete_set)->table(set,1,fix_delete);
probe(fix_delete_ordered)->table(ordered_set,1,fix_delete);
probe(fix_delete_bag)->table(bag,1,fix_delete);
probe(fix_delete_duplicate)->table(duplicate_bag,1,fix_delete);
probe(select_chunks_set)->table(set,1,{chunks,select});
probe(select_chunks_ordered)->table(ordered_set,1,{chunks,select});
probe(select_chunks_bag)->table(bag,1,{chunks,select});
probe(select_chunks_duplicate)->table(duplicate_bag,1,{chunks,select});
probe(match_chunks_set)->table(set,1,{chunks,match});
probe(match_chunks_ordered)->table(ordered_set,1,{chunks,match});
probe(match_chunks_bag)->table(bag,1,{chunks,match});
probe(match_chunks_duplicate)->table(duplicate_bag,1,{chunks,match});
probe(object_chunks_set)->table(set,1,{chunks,match_object});
probe(object_chunks_ordered)->table(ordered_set,1,{chunks,match_object});
probe(object_chunks_bag)->table(bag,1,{chunks,match_object});
probe(object_chunks_duplicate)->table(duplicate_bag,1,{chunks,match_object});
probe(cursor_mutation_ordered)->table(ordered_set,1,cursor_mutation);
probe(cursor_keypos_ordered)->table(ordered_set,2,cursor_keypos);
probe(cursor_cross_actor)->table(ordered_set,1,cursor_cross_actor);
probe(cursor_foreign_table)->table(ordered_set,1,cursor_foreign_table);
probe(cursor_roundtrip)->table(ordered_set,1,cursor_roundtrip);
probe(cursor_stale)->table(ordered_set,1,cursor_stale);
probe(cursor_zero_limit)->table(ordered_set,1,{bad_limit,0});
probe(cursor_negative_limit)->table(ordered_set,1,{bad_limit,-1});
probe(cursor_bad_limit)->table(ordered_set,1,{bad_limit,1.5});
probe(cursor_bad_shape)->ets:select({bad_cursor442});
probe(cursor_end)->{ets:select('$end_of_table'),ets:match('$end_of_table'),ets:match_object('$end_of_table')}.

table(Kind,Position,Mode)->
 T=ets:new(compressed_mutations442,[named_table,public,compressed,Kind,{keypos,Position}]),
 try {ets:info(T,compressed),exercise(T,Mode)}
 after case ets:info(T) of undefined->ok;_->true=ets:delete(T) end end.

exercise(T,insert_new)->
 true=ets:insert(T,{existing,old}),
 Result=ets:insert_new(T,[{before,new},{existing,replacement},{later,new}]),
 {Result,ets:lookup(T,before),ets:lookup(T,existing),ets:lookup(T,later),ets:info(T,size)};
exercise(T,input_duplicates)->
 Result=ets:insert_new(T,[{same,first},{same,second}]),
 {Result,ets:lookup(T,same),ets:info(T,size)};
exercise(T,counter_default)->
 R=ets:update_counter(T,wanted,{2,4},{discarded,10,tail}),
 {R,ets:lookup(T,wanted),ets:lookup(T,discarded)};
exercise(T,counter_keypos)->
 R=ets:update_counter(T,wanted,{1,3},{10,discarded,tail}),
 {R,ets:lookup(T,wanted),ets:lookup(T,discarded)};
exercise(T,counter_limits)->
 true=ets:insert(T,{key,5}),
 R=ets:update_counter(T,key,[{2,6,10,0},{2,-3,-2,7},{2,0}]),
 {R,ets:lookup(T,key)};
exercise(T,counter_big)->
 Big=1 bsl 100,true=ets:insert(T,{key,Big}),
 R=ets:update_counter(T,key,[{2,Big,Big+1,Big+7},{2,-Big,-1,Big+9}]),
 {R,ets:lookup(T,key)};
exercise(T,counter_atomic)->
 true=ets:insert(T,{key,5}),
 R=attempt(fun()->ets:update_counter(T,key,[{2,1},{9,1}]) end),
 {R,ets:lookup(T,key)};
exercise(T,counter_key)->
 true=ets:insert(T,{key,5}),R=attempt(fun()->ets:update_counter(T,key,{1,1}) end),
 {R,ets:lookup(T,key)};
exercise(T,counter_bad_default)->
 true=ets:insert(T,{known,5}),R=attempt(fun()->ets:update_counter(T,missing,{2,1},invalid_default) end),
 {R,ets:lookup(T,known),ets:lookup(T,missing)};
exercise(T,counter_kind_error)->
 true=ets:insert(T,{key,5}),R=attempt(fun()->ets:update_counter(T,key,{2,1}) end),
 {R,ets:lookup(T,key)};
exercise(T,replace)->
 true=ets:insert(T,[{1,10},{2,20}]),R=ets:select_replace(T,replace_spec()),
 {R,ets:lookup(T,1),ets:lookup(T,2)};
exercise(T,replace_keypos)->
 true=ets:insert(T,[{10,first,tail},{20,second,tail}]),
 R=ets:select_replace(T,[{{'$1','$2','$3'},[],[{{{'+','$1',1},'$2','$3'}}]}]),
 {R,ets:lookup(T,first),ets:lookup(T,second)};
exercise(T,replace_partial)->
 true=ets:insert(T,[{1,10},{2,20},{3,30}]),
 R=ets:select_replace(T,[{{'$1','$2'},[{'=:=','$1',2}],[{{'$1',{'+','$2',1}}}]}]),
 {R,ets:lookup(T,1),ets:lookup(T,2),ets:lookup(T,3)};
exercise(T,replace_changed_key)->
 true=ets:insert(T,{1,10}),R=attempt(fun()->ets:select_replace(T,[{{'$1','$2'},[],[{{changed,'$2'}}]}]) end),
 {R,ets:lookup(T,1),ets:lookup(T,changed)};
exercise(T,replace_bad_spec)->
 true=ets:insert(T,{1,10}),R=attempt(fun()->ets:select_replace(T,[bad_specification]) end),
 {R,ets:lookup(T,1)};
exercise(T,replace_kind_error)->
 true=ets:insert(T,{1,10}),R=attempt(fun()->ets:select_replace(T,replace_spec()) end),
 {R,ets:lookup(T,1)};
exercise(T,fix_delete)->
 true=ets:insert(T,[{10,a},{20,b},{30,c}]),true=ets:safe_fixtable(T,true),
 Key=ets:first(T),Before=ets:next(T,Key),true=ets:delete(T,Key),
 Fixed=ets:info(T,fixed),During=attempt(fun()->ets:next(T,Key) end),
 Deleted=not ets:member(T,Key),true=ets:safe_fixtable(T,false),
 After=attempt(fun()->ets:next(T,Key) end),
 note({fixation,Key,Before,During,After}),
 {Fixed,Deleted,During=:={returned,Before},ets:info(T,fixed),source_check(After,next,[T,Key])};
exercise(T,{chunks,Operation})->
 true=ets:insert(T,[{1,a},{1,b},{2,c},{3,d},{4,e}]),Before=ets:tab2list(T),
 note({before_chunks,Operation,Before}),
 Result=drain(Operation,start(Operation,T,2)),
 Expected=case Operation of match->bindings(Before);_->Before end,
 {length(Result),same_bag(Result,Expected),ets:info(T,size)};
exercise(T,cursor_mutation)->
 true=ets:insert(T,[{1,old},{3,keep},{5,remove}]),First=ets:select(T,whole_spec(),1),
 note({before_mutation,First}),{Chunk,Cursor}=First,
 true=ets:insert(T,{2,new}),true=ets:delete(T,5),true=ets:update_element(T,3,{2,changed}),
 Rest=drain(select,ets:select(Cursor)),
 {Chunk,Rest,ets:lookup(T,2),ets:lookup(T,3),ets:lookup(T,5)};
exercise(T,cursor_keypos)->
 true=ets:insert(T,[{a,1,tail},{b,2,tail},{c,3,tail}]),Before=ets:tab2list(T),
 First=ets:match_object(T,{'$1','$2','$3'},1),note({keypos_cursor,First}),
 Result=drain(match_object,First),{length(Result),same_bag(Result,Before)};
exercise(T,cursor_cross_actor)->
 true=ets:insert(T,[{1,a},{2,b},{3,c}]),{Chunk,Cursor}=ets:select(T,whole_spec(),1),
 {Child,Monitor}=spawn_monitor(?MODULE,resume_worker,[self(),select,Cursor]),
 {Outcome,ChildOwn}=await_cursor_child(Child,Monitor),
 note({cross_actor,Child,Cursor,Outcome,ChildOwn}),
 {Chunk,source_check(Outcome,select,[Cursor]),returned_bag(Outcome,[{2,b},{3,c}])};
exercise(T,cursor_foreign_table)->
 true=ets:insert(T,[{1,a},{2,b},{3,c}]),{_,Cursor}=ets:select(T,whole_spec(),1),
 Other=ets:new(compressed_foreign442,[named_table,public,compressed,ordered_set]),
 try true=ets:insert(Other,[{8,other},{9,other}]),
     Foreign=setelement(1,Cursor,Other),Outcome=attempt(fun()->drain(select,ets:select(Foreign)) end),
     note({foreign_table,Cursor,Foreign,Outcome}),source_check(Outcome,select,[Foreign])
 after true=ets:delete(Other) end;
exercise(T,cursor_roundtrip)->
 true=ets:insert(T,[{1,a},{2,b},{3,c}]),{_,Cursor}=ets:select(T,whole_spec(),1),
 Encoded=term_to_binary(Cursor),Copied=binary_to_term(Encoded),
 Outcome=attempt(fun()->drain(select,ets:select(Copied)) end),
 note({roundtrip_cursor,Cursor,Encoded,Copied,Outcome}),source_check(Outcome,select,[Copied]);
exercise(T,cursor_stale)->
 true=ets:insert(T,[{1,a},{2,b},{3,c}]),{_,Cursor}=ets:select(T,whole_spec(),1),true=ets:delete(T),
 Outcome=attempt(fun()->drain(select,ets:select(Cursor)) end),
 note({stale_cursor,Cursor,Outcome}),source_check(Outcome,select,[Cursor]);
exercise(T,{bad_limit,Limit})->ets:select(T,whole_spec(),Limit).

replace_spec()->[{{'$1','$2'},[],[{{'$1',{'+','$2',1}}}]}].
whole_spec()->[{{'$1','$2'},[],['$_']}].
start(select,T,Limit)->ets:select(T,whole_spec(),Limit);
start(match,T,Limit)->ets:match(T,{'$1','$2'},Limit);
start(match_object,T,Limit)->ets:match_object(T,{'$1','$2'},Limit).
resume(select,C)->ets:select(C);
resume(match,C)->ets:match(C);
resume(match_object,C)->ets:match_object(C).
drain(Operation,Value)->
 note({cursor_round,Operation,Value}),
 case Value of '$end_of_table'->[];{Chunk,Cursor}->append(Chunk,drain(Operation,resume(Operation,Cursor))) end.
resume_worker(Parent,Operation,Cursor)->
 put(compressed_mutations_own442,[]),
 Outcome=attempt(fun()->drain(Operation,resume(Operation,Cursor)) end),
 Parent!{resumed442,self(),Outcome,reverse(get(compressed_mutations_own442))}.
await_cursor_child(Child,Monitor)->
 receive
  {resumed442,Child,Value,Own}->
   receive
    {'DOWN',Monitor,process,Child,normal}->{Value,Own};
    {'DOWN',Monitor,process,Child,Reason}->error({cursor_child_down,Reason})
   after 5000->stop_cursor_child(Child,Monitor,cursor_child_unsettled) end;
  {'DOWN',Monitor,process,Child,Reason}->error({cursor_child_without_report,Reason})
 after 5000->stop_cursor_child(Child,Monitor,cursor_child_timeout) end.
stop_cursor_child(Child,Monitor,Reason)->
 exit(Child,kill),
 receive {'DOWN',Monitor,process,Child,_}->error(Reason)
 after 5000->error(cursor_child_kill_unsettled) end.

attempt(Function)->try Function() of Value->{returned,Value} catch Kind:Reason:Stack->{raised,Kind,Reason,Stack} end.
source_check({returned,Value},_,_)->{returned,is_list(Value),case is_list(Value) of true->length(Value);false->0 end};
source_check({raised,Kind,Reason,Stack},Function,Arguments)->
 {raised,Kind,Reason=:=badarg,is_list(Stack),frame_has_input(Stack,Function,Arguments)}.
frame_has_input([],_,_)->false;
frame_has_input([{ets,Function,Arguments,_}|_],Function,Arguments)->true;
frame_has_input([_|Rest],Function,Arguments)->frame_has_input(Rest,Function,Arguments).
returned_bag({returned,Value},Expected)->same_bag(Value,Expected);
returned_bag(_,_) -> false.
bindings([])->[];
bindings([{Key,Value}|Rest])->[[Key,Value]|bindings(Rest)].
same_bag([],[])->true;
same_bag([Value|Rest],Other)->case remove_one(Value,Other) of missing->false;{found,Tail}->same_bag(Rest,Tail) end;
same_bag(_,_) -> false.
remove_one(_,[])->missing;
remove_one(Value,[Head|Tail]) when Value=:=Head->{found,Tail};
remove_one(Value,[Head|Tail])->case remove_one(Value,Tail) of missing->missing;{found,Rest}->{found,[Head|Rest]} end.
append([],Right)->Right;
append([Head|Tail],Right)->[Head|append(Tail,Right)].
reverse(List)->reverse(List,[]).
reverse([],Acc)->Acc;
reverse([Head|Tail],Acc)->reverse(Tail,[Head|Acc]).
note(Value)->
 case get(compressed_mutations_own442) of undefined->ok;Old->put(compressed_mutations_own442,[Value|Old]),ok end.
