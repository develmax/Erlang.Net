-module(beam_ets_compressed_mutations_observer442).
-export([cases/0,run/0,run_stacks/0,capture/0,capture_stacks/0,write_values/1,write_stacks/1]).

cases()->beam_ets_compressed_mutations_subject442:cases().
run()->{Rows,_}=capture(),Rows.
run_stacks()->{Profiles,_}=capture_stacks(),Profiles.
capture()->
 Old=erlang:system_flag(system_logger,undefined),
 try rows(cases()) after erlang:system_flag(system_logger,Old) end.
rows([])->{[],[]};
rows([Case|Rest])->
 {Outcome,Own}=observe(Case),
 Scope=case member(Case,beam_ets_compressed_mutations_subject442:own_cases()) of
  true->own_state_protocol;false->portable_full_term end,
 {Rows,OwnRows}=rows(Rest),
 {[{Case,Outcome}|Rows],[{Case,Scope,Own}|OwnRows]}.

capture_stacks()->
 Old=erlang:system_flag(backtrace_depth,8),
 erlang:system_flag(backtrace_depth,Old),
 try
  {Default,OwnDefault}=capture(),
  {Zero,OwnZero}=at_depth(0),{One,OwnOne}=at_depth(1),
  {Two,OwnTwo}=at_depth(2),{Eight,OwnEight}=at_depth(8),
  {[{default,Old,Default},Zero,One,Two,Eight],
   [{default,Old,OwnDefault},OwnZero,OwnOne,OwnTwo,OwnEight]}
 after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth)->
 erlang:system_flag(backtrace_depth,Depth),
 Effective=erlang:system_flag(backtrace_depth,Depth),
 {Rows,OwnRows}=capture(),{{Depth,Effective,Rows},{Depth,Effective,OwnRows}}.

observe(Case)->
 {Child,Monitor}=erlang:spawn_monitor(beam_ets_compressed_mutations_subject442,worker,[self(),Case]),
 receive
  {Case,Outcome,Own}->wait_down(Child,Monitor,Case,Outcome,Own);
  {'DOWN',Monitor,process,Child,Reason}->erlang:error({child_down_without_report,Case,Reason})
 after 30000->abort(Child,Monitor,Case) end.
wait_down(Child,Monitor,Case,Outcome,Own)->
 receive
  {'DOWN',Monitor,process,Child,normal}->{Outcome,Own};
  {'DOWN',Monitor,process,Child,Reason}->erlang:error({reported_but_abnormal_child,Case,Reason})
 after 5000->abort(Child,Monitor,Case) end.
abort(Child,Monitor,Case)->
 erlang:exit(Child,kill),
 receive {'DOWN',Monitor,process,Child,_}->erlang:error({child_timeout,Case})
 after 5000->erlang:error({unsettled_child,Case}) end.

write_values(Path)->
 {Rows,OwnRows}=capture(),
 ok=file:write_file(Path++"/values.etf",term_to_binary(Rows,[{minor_version,2}])),
 ok=file:write_file(Path++"/own-values.etf",term_to_binary(OwnRows,[{minor_version,2}])).
write_stacks(Path)->
 {Profiles,OwnProfiles}=capture_stacks(),
 ok=file:write_file(Path++"/stacks.etf",term_to_binary(Profiles,[{minor_version,2}])),
 ok=file:write_file(Path++"/own-stacks.etf",term_to_binary(OwnProfiles,[{minor_version,2}])).
member(_,[])->false;
member(Value,[Value|_])->true;
member(Value,[_|Rest])->member(Value,Rest).
