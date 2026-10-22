-module(beam_ets_new_heir419).
-export([run/0,run_stacks/0,worker/2,owner/2,capture/1]).
cases()->[data_named,data_unnamed,without_message,none,dead,self,last_data,last_without_message,last_none,none_then_live,bad_atom,bad_arity,bad_none_triple,bad_list_tail,bad_option_first,bad_then_none,bad_name,collision,duplicate_none,empty].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
gift()->{gift_new419,#{nested=>[one,two,three]}}.
next()->{gift_last419,[4,5,6]}.
cleanup(T)->try ets:delete(T) catch error:badarg->ok end.
perform(Mode) when Mode=:=data_named;Mode=:=data_unnamed;Mode=:=without_message;Mode=:=none;Mode=:=dead;Mode=:=self;Mode=:=last_data;Mode=:=last_without_message;Mode=:=last_none;Mode=:=none_then_live->inherit(Mode);
perform(bad_atom)->ets:new(new_error419,[{heir,bad}]);
perform(bad_arity)->ets:new(new_error419,[{heir,none,one,two}]);
perform(bad_none_triple)->ets:new(new_error419,[{heir,none,gift}]);
perform(bad_list_tail)->ets:new(new_error419,[{heir,none}|bad]);
perform(bad_option_first)->ets:new(new_error419,[bad,{heir,none}]);
perform(bad_then_none)->ets:new(new_error419,[{heir,bad},{heir,none}]);
perform(bad_name)->ets:new(42,[]);
perform(collision)->T=ets:new(new_error419,[named_table]),try ets:new(new_error419,[named_table,{heir,none}]) after cleanup(T) end;
perform(duplicate_none)->T=ets:new(new_error419,[{heir,none},{heir,none}]),try ets:lookup(T,key) after cleanup(T) end;
perform(empty)->T=ets:new(new_error419,[]),try true=ets:insert(T,{key,42}),ets:lookup(T,key) after cleanup(T) end.
inherit(Mode)->
 {Pid,Ref}=spawn_monitor(?MODULE,owner,[self(),Mode]),
 receive {ready,T}->Pid!stop,
  Result=case Mode of
   data_named->transfer(T,Pid,Ref,gift());data_unnamed->transfer(T,Pid,Ref,gift());
   last_data->transfer(T,Pid,Ref,next());none_then_live->transfer(T,Pid,Ref,gift());
   _->receive {'DOWN',Ref,process,Pid,normal}->NoMessage=receive {'ETS-TRANSFER',_,_,_}->false after 0->true end,
     case Mode of without_message->{NoMessage,ets:lookup(T,key)};last_without_message->{NoMessage,ets:lookup(T,key)};_->{NoMessage,ets:whereis(new_live419)} end end
  end,cleanup(T),Result
 end.
transfer(T,Pid,Ref,Gift)->receive {'ETS-TRANSFER',Tid,From,Data}->receive {'DOWN',Ref,process,Pid,normal}->{Tid=:=T,From=:=Pid,Data=:=Gift,ets:lookup(T,key)} end;{'DOWN',Ref,process,Pid,Reason}->exit({missing_transfer,Reason}) end.
owner(Parent,Mode)->
 Base=case Mode of data_unnamed->[private];_->[named_table,private] end,
 Heir=case Mode of
  without_message->[{heir,Parent}];none->[{heir,none}];self->[{heir,self(),gift()}];
  dead->{Dead,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,Dead,normal}->ok end,[{heir,Dead,gift()}];
  last_data->[{heir,Parent,gift()},{heir,Parent,next()}];
  last_without_message->[{heir,Parent,gift()},{heir,Parent}];
  last_none->[{heir,Parent,gift()},{heir,none}];
  none_then_live->[{heir,none},{heir,Parent,gift()}];
  _->[{heir,Parent,gift()}]
 end,
 T=ets:new(new_live419,Base++Heir),true=ets:insert(T,{key,42}),Parent!{ready,T},receive stop->ok end.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
