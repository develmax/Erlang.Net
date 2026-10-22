-module(beam_code_purger_observer432).
-export([run/3,run_stacks/3,capture/1,observer_child/5]).
cases() -> [unauthorized,unauthorized_bad_args,unauthorized_bad_op,soft_absent,hard_absent,hard_idle,soft_idle,soft_held,hard_held,soft_fun,onload_empty,onload_saved].
run(A,B,C) -> [{K,observe(K,default,A,B,C)} || K <- cases()].
run_stacks(A,B,C) -> [{D,actual_depth(D),[{K,observe(K,D,A,B,C)} || K <- cases()]} || D <- [default,0,1,2,8]].
actual_depth(default) -> V=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,V),V;
actual_depth(D) -> D.
observe(K,D,A,B,C) ->
 {P,R}=spawn_monitor(?MODULE,observer_child,[self(),K,D,{A,B},C]),
 receive {P,V} -> receive {'DOWN',R,process,P,normal} -> V end;
         {'DOWN',R,process,P,Why} -> {unexpected_down,Why}
 after 15000 -> exit(P,kill),erlang:error({observer_timeout,K}) end.
observer_child(Parent,K,D,{A,B},C) ->
 case D of default -> ok; _ -> erlang:system_flag(backtrace_depth,D) end,
 V=try probe(K,A,B,C) of X -> {value,X} catch T:R:S -> {exception,T,R,S} end,
 Parent!{self(),V}.
probe(unauthorized,_,_,_) -> erts_internal:purge_module(beam_purge_subject432,prepare);
probe(unauthorized_bad_args,_,_,_) -> erts_internal:purge_module(17,[]);
probe(unauthorized_bad_op,_,_,_) -> erts_internal:purge_module(beam_purge_subject432,invalid);
probe(soft_absent,_,_,_) -> reset(),erts_code_purger:soft_purge(beam_purge_subject432);
probe(hard_absent,_,_,_) -> reset(),erts_code_purger:purge(beam_purge_subject432);
probe(hard_idle,A,B,_) -> reset(),load(A),load(B),erts_code_purger:purge(beam_purge_subject432);
probe(soft_idle,A,B,_) -> reset(),load(A),load(B),erts_code_purger:soft_purge(beam_purge_subject432);
probe(soft_held,A,B,_) ->
 reset(),load(A),{P,R}=spawn_monitor(beam_purge_subject432,hold,[self()]),
 receive {ready432,P} -> ok end,load(B),Res=erts_code_purger:soft_purge(beam_purge_subject432),P!stop432,
 receive {stopped432,P,V} -> receive {'DOWN',R,process,P,normal} -> {Res,V} end end;
probe(hard_held,A,B,_) ->
 reset(),load(A),{P,R}=spawn_monitor(beam_purge_subject432,hold,[self()]),
 receive {ready432,P} -> ok end,load(B),Res=erts_code_purger:purge(beam_purge_subject432),
 receive {'DOWN',R,process,P,Why} -> {Res,Why} end;
probe(soft_fun,A,B,_) ->
 reset(),load(A),F=beam_purge_subject432:held_fun(self()),{P,R}=spawn_monitor(F),
 receive {ready432,P} -> ok end,load(B),Res=erts_code_purger:soft_purge(beam_purge_subject432),P!stop432,
 receive {stopped432,P,V} -> receive {'DOWN',R,process,P,normal} -> {Res,V} end end;
probe(onload_empty,_,_,C) ->
 reset(),reject(C),{ok,erts_code_purger:purge(beam_purge_subject432)};
probe(onload_saved,A,_,C) ->
 reset(),load(A),true=erlang:delete_module(beam_purge_subject432),reject(C),{ok,erts_code_purger:purge(beam_purge_subject432)}.
reset() ->
 _=erts_code_purger:purge(beam_purge_subject432),
 try erlang:delete_module(beam_purge_subject432) of true -> ok; undefined -> ok catch error:badarg -> ok end,
 _=erts_code_purger:purge(beam_purge_subject432),ok.
load(Bin) -> {module,beam_purge_subject432}=erlang:load_module(beam_purge_subject432,Bin),ok.
reject(Bin) ->
 Ref=erlang:prepare_loading(beam_purge_subject432,Bin),
 {on_load,[beam_purge_subject432]}=erlang:finish_loading([Ref]),
 rejected432=erlang:call_on_load_function(beam_purge_subject432),
 ok=erts_code_purger:finish_after_on_load(beam_purge_subject432,false).
capture([Out]) ->
 {ok,A}=file:read_file(Out++"/v1/beam_purge_subject432.beam"),
 {ok,B}=file:read_file(Out++"/v2/beam_purge_subject432.beam"),
 {ok,C}=file:read_file(Out++"/rejected/beam_purge_subject432.beam"),
 ok=file:write_file(Out++"/values.etf",term_to_binary(run(A,B,C),[{minor_version,2}])),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(A,B,C),[{minor_version,2}])),halt().
