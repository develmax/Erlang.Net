-module(beam_passive_entry649).
-export([run/3,worker/5]).
run(Mode,A,B) ->
 {Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Mode,A,B,full_stack]),
 receive
  {Pid,Outcome} ->
   receive {'DOWN',Ref,process,Pid,normal} -> Outcome;
           {'DOWN',Ref,process,Pid,Reason} -> erlang:error({unexpected_down649,Reason})
   after 5000 -> exit(Pid,kill),erlang:error(no_down649) end;
  {'DOWN',Ref,process,Pid,Reason} -> erlang:error({worker_failed649,Reason})
 after 10000 -> exit(Pid,kill),erlang:error(no_result649)
 end.
worker(Parent,Mode,A,B,full_stack) ->
 Result=case Mode of
  complete -> beam_passive_fun647:complete(A,B);
  abort -> beam_passive_fun647:abort(A,B);
  reload -> beam_passive_fun647:reload(A,B)
 end,
 Stack=case Result of {_,_,_,{exception,_,_,S,_},_} -> S; _ -> [] end,
 Parent!{self(),{whole_outcome,Result,unchanged_full_stack,Stack}}.
