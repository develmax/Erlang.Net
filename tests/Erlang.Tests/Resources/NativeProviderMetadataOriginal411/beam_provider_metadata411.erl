-module(beam_provider_metadata411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [ets_module,ets_unknown,ets_nonatom,persistent_module,persistent_unknown,persistent_nonatom].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D) || D<-[default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C) -> V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
metadata(M,K) -> M:module_info(K).
probe(ets_module) -> metadata(ets,module);
probe(ets_unknown) -> metadata(ets,provider_metadata_unknown411);
probe(ets_nonatom) -> metadata(ets,17);
probe(persistent_module) -> metadata(persistent_term,module);
probe(persistent_unknown) -> metadata(persistent_term,provider_metadata_unknown411);
probe(persistent_nonatom) -> metadata(persistent_term,17).
