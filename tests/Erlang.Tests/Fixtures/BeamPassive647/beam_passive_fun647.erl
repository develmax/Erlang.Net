-module(beam_passive_fun647).
-export([complete/2,abort/2,reload/2,capture/1]).
complete(A,B) ->
 load(A),F=beam_purge_subject432:fun_value(),Before=capture(F),load(B),
 Held=erlang:check_process_code(self(),beam_purge_subject432),
 Purged=erts_code_purger:soft_purge(beam_purge_subject432),
 {Before,Held,Purged,capture(F),beam_purge_subject432:version()}.
reload(A,B) ->
 load(A),F=beam_purge_subject432:fun_value(),Before=capture(F),load(B),
 Held=erlang:check_process_code(self(),beam_purge_subject432),
 Purged=erts_code_purger:soft_purge(beam_purge_subject432),load(A),
 {Before,Held,Purged,capture(F),beam_purge_subject432:version()}.
abort(A,B) ->
 load(A),F=beam_purge_subject432:fun_value(),Before=capture(F),
 {P,Ref}=spawn_monitor(beam_purge_subject432,hold,[self()]),
 try
  receive {ready432,P} -> ok after 5000 -> erlang:error(no_ready647) end,
  load(B),Held=erlang:check_process_code(self(),beam_purge_subject432),
  Purged=erts_code_purger:soft_purge(beam_purge_subject432),
  {Before,Held,Purged,capture(F),beam_purge_subject432:version()}
 after
  P!stop432,
  receive {stopped432,P,_} -> ok after 5000 -> exit(P,kill) end,
  receive {'DOWN',Ref,process,P,_} -> ok after 5000 -> exit(P,kill) end
 end.
capture(F) -> try F() of V -> {value,V} catch C:R:S -> {exception,C,R,S,same_reason_fun(R,F)} end.
same_reason_fun({badfun,G},F) -> G=:=F;
same_reason_fun(_,_) -> not_applicable.
load(Bin) -> {module,beam_purge_subject432}=erlang:load_module(beam_purge_subject432,Bin),ok.
