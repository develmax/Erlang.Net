-module(beam_ets_heir419).
-export([run/0,run_stacks/0,worker/2,owner/2,recipient/2,borrower/3,capture/1]).
cases() -> [data_named,data_unnamed,without_message,heir_none,dead_heir,self_heir,replace_data,protection_public,empty_options,tuple_protection,bad_options_atom,bad_list_tail,bad_list_element,bad_option_name,duplicate_heir,duplicate_protection,bad_heir_atom,bad_heir_arity,bad_protection,bad_protection_arity,bad_tid_type,bad_tid_missing,bad_tid_precedes_badopts,give_named,give_unnamed,give_bad_pid,give_missing_table,give_self_classified,setopts_not_owner_public,setopts_access_protected,setopts_access_private,replace_without_message].
run() -> [{Name,observe(Name)} || Name <- cases()].
run_stacks() ->
 Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),
 try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]]
 after erlang:system_flag(backtrace_depth,Old) end.
depth(D) -> erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name) ->
 {Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),
 receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end
 after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name) -> V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
gift() -> {gift419,#{nested=>[one,two,three]}}.
next_gift() -> {next419,[4,5,6]}.
cleanup(T) -> try ets:delete(T) catch error:badarg->ok end.
with_table(Protection,Operation) ->
 T=ets:new(heir_error419,[named_table,Protection]),
 try true=ets:insert(T,{key,42}),Operation(T) after cleanup(T) end.
perform(data_named)->inherit(data_named);
perform(data_unnamed)->inherit(data_unnamed);
perform(without_message)->inherit(without_message);
perform(heir_none)->inherit(heir_none);
perform(dead_heir)->inherit(dead_heir);
perform(self_heir)->inherit(self_heir);
perform(replace_data)->inherit(replace_data);
perform(replace_without_message)->inherit(replace_without_message);
perform(protection_public)->with_table(private,fun(T)->true=ets:setopts(T,{protection,public}),borrow(T,insert) end);
perform(empty_options)->with_table(private,fun(T)->ets:setopts(T,[]) end);
perform(tuple_protection)->with_table(public,fun(T)->true=ets:setopts(T,{protection,private}),ets:lookup(T,key) end);
perform(bad_options_atom)->with_table(public,fun(T)->ets:setopts(T,bad) end);
perform(bad_list_tail)->with_table(public,fun(T)->ets:setopts(T,[{protection,private}|bad]) end);
perform(bad_list_element)->with_table(public,fun(T)->ets:setopts(T,[bad]) end);
perform(bad_option_name)->with_table(public,fun(T)->ets:setopts(T,{unknown,true}) end);
perform(duplicate_heir)->with_table(public,fun(T)->ets:setopts(T,[{heir,none},{heir,none}]) end);
perform(duplicate_protection)->with_table(public,fun(T)->ets:setopts(T,[{protection,private},{protection,private}]) end);
perform(bad_heir_atom)->with_table(public,fun(T)->ets:setopts(T,{heir,invalid}) end);
perform(bad_heir_arity)->with_table(public,fun(T)->ets:setopts(T,{heir,none,gift,extra}) end);
perform(bad_protection)->with_table(public,fun(T)->ets:setopts(T,{protection,invalid}) end);
perform(bad_protection_arity)->with_table(public,fun(T)->ets:setopts(T,{protection,private,extra}) end);
perform(bad_tid_type)->ets:setopts(17,[]);
perform(bad_tid_missing)->ets:setopts(missing419,[]);
perform(bad_tid_precedes_badopts)->ets:setopts(missing419,bad);
perform(give_named)->give(named);
perform(give_unnamed)->give(unnamed);
perform(give_bad_pid)->with_table(public,fun(T)->ets:give_away(T,invalid,gift419) end);
perform(give_missing_table)->ets:give_away(missing419,invalid,gift419);
perform(give_self_classified)->with_table(private,fun(T)->Me=self(),try ets:give_away(T,Me,gift419) catch error:badarg:Stack->case Stack of [{ets,give_away,Args,Info}|_]->{self_owner_refused,Args=:=[T,Me,gift419],Info};_->erlang:raise(error,badarg,Stack) end end end);
perform(setopts_not_owner_public)->with_table(public,fun(T)->borrow(T,setopts) end);
perform(setopts_access_protected)->with_table(protected,fun(T)->borrow(T,setopts) end);
perform(setopts_access_private)->with_table(private,fun(T)->borrow(T,setopts) end).
borrow(T,Operation)->
 {Pid,Ref}=spawn_monitor(?MODULE,borrower,[self(),T,Operation]),
 receive {borrowed,V}->receive {'DOWN',Ref,process,Pid,normal}->V end end.
borrower(Parent,T,Operation)->
 V=try
  Result=case Operation of insert->ets:insert(T,{other,43});setopts->ets:setopts(T,[]) end,
  {value,Result}
 catch C:R:S->{exception,C,R,S} end,Parent!{borrowed,V}.
inherit(Mode)->
 {Pid,Ref}=spawn_monitor(?MODULE,owner,[self(),Mode]),
 receive {ready,T}->
  Pid!stop,
  Result=case Mode of
   data_named->expected_transfer(T,Pid,Ref,gift());
   data_unnamed->expected_transfer(T,Pid,Ref,gift());
   replace_data->expected_transfer(T,Pid,Ref,next_gift());
   _->receive {'DOWN',Ref,process,Pid,normal}->
       NoMessage=receive {'ETS-TRANSFER',_,_,_}->false after 0->true end,
       case Mode of
        without_message->{NoMessage,ets:lookup(T,key)};
        replace_without_message->{NoMessage,ets:lookup(T,key)};
        _->{NoMessage,ets:whereis(heir_live419)}
       end
      end
  end,
  cleanup(T),Result
 end.
expected_transfer(T,Pid,Ref,Gift)->
 receive
  {'ETS-TRANSFER',Tid,From,Data}->
   receive {'DOWN',Ref,process,Pid,normal}->{Tid=:=T,From=:=Pid,Data=:=Gift,ets:lookup(T,key)} end;
  {'DOWN',Ref,process,Pid,Reason}->exit({missing_transfer,Reason})
 end.
owner(Parent,Mode)->
 Options=case Mode of data_unnamed->[private];_->[named_table,private] end,
 T=ets:new(heir_live419,Options),true=ets:insert(T,{key,42}),
 case Mode of
  without_message->true=ets:setopts(T,{heir,Parent});
  replace_without_message->true=ets:setopts(T,{heir,Parent,gift()}),true=ets:setopts(T,{heir,Parent});
  heir_none->true=ets:setopts(T,{heir,Parent,gift()}),true=ets:setopts(T,{heir,none});
  dead_heir->
   {Dead,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,Dead,normal}->ok end,
   true=ets:setopts(T,{heir,Dead,gift()});
  self_heir->true=ets:setopts(T,{heir,self(),gift()});
  replace_data->true=ets:setopts(T,{heir,Parent,gift()}),true=ets:setopts(T,{heir,Parent,next_gift()});
  _->true=ets:setopts(T,{heir,Parent,gift()})
 end,
 Parent!{ready,T},receive stop->ok end.
give(Kind)->
 Options=case Kind of named->[named_table,private];unnamed->[private] end,
 T=ets:new(heir_give419,Options),true=ets:insert(T,{key,42}),
 {Pid,Ref}=spawn_monitor(?MODULE,recipient,[self(),T]),
 try
  true=ets:give_away(T,Pid,gift()),
  receive {given,V}->receive {'DOWN',Ref,process,Pid,normal}->V end end
 after cleanup(T) end.
recipient(Parent,T)->
 receive {'ETS-TRANSFER',Tid,From,Data}->Parent!{given,{Tid=:=T,From=:=Parent,Data=:=gift(),ets:lookup(T,key)}} end.
capture([Dir])->
 ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),
 erlang:halt(0).
