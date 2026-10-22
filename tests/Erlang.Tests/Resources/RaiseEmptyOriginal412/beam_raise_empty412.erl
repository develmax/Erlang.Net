-module(beam_raise_empty412).
-export([run/0,run_stacks/0,worker/2]).
cases()->[valid_caught,error_nil,exit_nil,invalid_class,invalid_trace,ordinary_badarg].
run()->[{C,observe(C)}||C<-cases()].
run_stacks()->[depth(D)||D<-[default,0,1,2,8]].
depth(default)->{default,run()};
depth(D)->Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C)->P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C)->V=try probe(C)of X->{value,X}catch K:E:S->{caught,K,E,S}end,P!{R,V}.
caught(Reason)->try erlang:raise(throw,Reason,[]) catch K:E:S->{K,E,S} end.
probe(valid_caught)->caught(marker);
probe(error_nil)->erlang:raise(error,marker,[]);
probe(exit_nil)->erlang:raise(exit,marker,[]);
probe(invalid_class)->erlang:raise(marker,marker,[]);
probe(invalid_trace)->erlang:raise(throw,marker,marker);
probe(ordinary_badarg)->try erlang:raise(marker,marker,[])of badarg->returned_badarg catch _:_ ->unexpected end.
