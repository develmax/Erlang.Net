-module(beam_ets_rename418).
-export([run/0,run_stacks/0,worker/2,rename_other/4,capture/1]).
cases() -> [named_owner,named_reference,unnamed_owner,unnamed_same,unnamed_collision,named_collision,named_same,bad_name_float,bad_name_list,bad_name_tuple,bad_tid_type,bad_tid_missing,public_other,protected_other,private_other,protected_bad_name,private_bad_name,chain,old_name_missing,renamed_owner_death].
run() -> [{Name,observe(Name)} || Name <- cases()].
run_stacks() ->
    Old=erlang:system_flag(backtrace_depth,8), erlang:system_flag(backtrace_depth,Old),
    try [{default,Old,run()}|[at_depth(D)||D<-[0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D) -> erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name) ->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),
    receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end
    after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name) ->
    Result=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,
    Parent!{Name,Result}.
with_table(Options,Operation) ->
    T=ets:new(rename_old418,Options),
    try true=ets:insert(T,{key,42}),Operation(T)
    after cleanup(T),cleanup(rename_new418),cleanup(rename_final418) end.
cleanup(T) -> try ets:delete(T) catch error:badarg->ok end.
perform(named_owner) -> with_table([named_table],fun(T)->N=ets:rename(T,rename_new418),{N,ets:whereis(rename_old418),is_reference(ets:whereis(N)),ets:lookup(N,key)} end);
perform(named_reference) -> with_table([named_table],fun(T)->Ref=ets:whereis(T),N=ets:rename(Ref,rename_new418),{N,ets:whereis(N)=:=Ref,ets:lookup(Ref,key)} end);
perform(unnamed_owner) -> with_table([],fun(T)->R=ets:rename(T,rename_new418),{R=:=T,ets:whereis(rename_new418),ets:lookup(T,key)} end);
perform(unnamed_same) -> with_table([],fun(T)->ets:rename(T,rename_old418)=:=T end);
perform(unnamed_collision) ->
    Other=ets:new(rename_new418,[named_table]),
    try with_table([],fun(T)->R=ets:rename(T,rename_new418),{R=:=T,ets:whereis(rename_new418)=:=ets:whereis(Other),ets:lookup(T,key)} end)
    after cleanup(Other) end;
perform(named_collision) -> Other=ets:new(rename_new418,[named_table]),try with_table([named_table],fun(T)->ets:rename(T,rename_new418) end) after cleanup(Other) end;
perform(named_same) -> with_table([named_table],fun(T)->ets:rename(T,rename_old418) end);
perform(bad_name_float) -> with_table([named_table],fun(T)->ets:rename(T,1.5) end);
perform(bad_name_list) -> with_table([named_table],fun(T)->ets:rename(T,[]) end);
perform(bad_name_tuple) -> with_table([named_table],fun(T)->ets:rename(T,{bad,name}) end);
perform(bad_tid_type) -> ets:rename(17,rename_new418);
perform(bad_tid_missing) -> ets:rename(rename_missing418,{invalid,name});
perform(public_other) -> with_table([named_table,public],fun(T)->other(T) end);
perform(protected_other) -> with_table([named_table,protected],fun(T)->other(T) end);
perform(private_other) -> with_table([named_table,private],fun(T)->other(T) end);
perform(protected_bad_name) -> with_table([named_table,protected],fun(T)->other(T,{invalid,name}) end);
perform(private_bad_name) -> with_table([named_table,private],fun(T)->other(T,{invalid,name}) end);
perform(chain) -> with_table([named_table],fun(T)->Ref=ets:whereis(T),N=ets:rename(T,rename_new418),F=ets:rename(N,rename_final418),{F,ets:whereis(T),ets:whereis(N),ets:whereis(F)=:=Ref,ets:lookup(Ref,key)} end);
perform(old_name_missing) -> with_table([named_table],fun(T)->ets:rename(T,rename_new418),ets:rename(T,rename_final418) end);
perform(renamed_owner_death) ->
    {Pid,Ref}=spawn_monitor(fun()->T=ets:new(rename_old418,[named_table]),ets:rename(T,rename_new418),ok end),
    receive {'DOWN',Ref,process,Pid,normal}->{ets:whereis(rename_old418),ets:whereis(rename_new418)} end.
other(T) -> other(T,rename_new418).
other(T,Next) ->
    {Pid,Ref}=spawn_monitor(?MODULE,rename_other,[self(),T,Next,rename_reply418]),
    receive {rename_reply418,V}->receive {'DOWN',Ref,process,Pid,normal}->V end end.
rename_other(Parent,T,N,Tag) ->
    R=try {value,ets:rename(T,N)} catch C:E:S->{exception,C,E,S} end,Parent!{Tag,R}.
capture([Dir]) ->
    ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
    ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),
    erlang:halt(0).
