-module(beam_closed_wait412).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [wait_error,wait_error_reject,ordinary_prefix,mutual_cycle_killed,timeout_error,unhandled_wait].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D) || D<-[default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C) -> V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
wait_throw() -> receive marker->erlang:error(marker) end.
outer() -> try wait_throw() catch error:marker->caught_marker end.
prefix() -> erlang:put(effect,seen),wait_throw().
cycle_a() -> receive go->cycle_b() end.
cycle_b() -> cycle_a().
timeout_throw(T) -> receive after T->erlang:error(timeout_marker) end.
kill_cycle() -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(fun()->P!{R,ready},cycle_a() end),receive {R,ready}->Pid!go,exit(Pid,kill),receive {'DOWN',M,process,Pid,killed}->killed end end.
probe(wait_error) -> self()!marker,outer();
probe(wait_error_reject) -> self()!unrelated,self()!marker,caught_marker=outer(),receive unrelated->{caught_marker,retained} after 0->missing end;
probe(ordinary_prefix) -> self()!marker,try prefix() catch error:marker->erlang:get(effect) end;
probe(mutual_cycle_killed) -> kill_cycle();
probe(timeout_error) -> try timeout_throw(0) catch error:timeout_marker->caught_timeout end;
probe(unhandled_wait) -> self()!marker,wait_throw().
