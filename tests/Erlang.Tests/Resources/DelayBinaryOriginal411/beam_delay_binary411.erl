-module(beam_delay_binary411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [parse_valid,parse_empty,parse_tail,parse_short_length,parse_short_name,parse_short_creation,parse_nonbinary,list_named,list_short,delay_zero,delay_one,delay_bad_timeout,after_delay_valid,after_delay_short,infinity_killed,wrong_clause,wait_keeps_message,caught_parse].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D) || D<-[default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C) -> V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
disjoint({parse,B}) -> <<N:16,Name:N/binary,Creation:32,_/binary>>=B,{Name,Creation};
disjoint({list_named,L}) -> B=list_to_binary(L),<<N:16,Name:N/binary,Creation:32,_/binary>>=B,{Name,Creation};
disjoint({wait,T}) -> receive after T->waited end;
disjoint({after_delay,B}) -> receive after 0-><<N:16,Name:N/binary,Creation:32,_/binary>>=B,{Name,Creation} end;
disjoint({hold,P,R}) -> P!{R,ready},receive after infinity->never end.
kill_delay() -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(fun()->disjoint({hold,P,R}) end),receive {R,ready}->exit(Pid,kill),receive {'DOWN',M,process,Pid,killed}->killed end end.
probe(parse_valid) -> disjoint({parse,<<0,2,97,98,0,0,0,7>>});
probe(parse_empty) -> disjoint({parse,<<0,0,0,0,0,7>>});
probe(parse_tail) -> disjoint({parse,<<0,2,97,98,0,0,0,7,99>>});
probe(parse_short_length) -> disjoint({parse,<<0>>});
probe(parse_short_name) -> disjoint({parse,<<0,3,97>>});
probe(parse_short_creation) -> disjoint({parse,<<0,1,97,0>>});
probe(parse_nonbinary) -> disjoint({parse,invalid});
probe(list_named) -> disjoint({list_named,[0,2,97,98,0,0,0,7]});
probe(list_short) -> disjoint({list_named,[0]});
probe(delay_zero) -> disjoint({wait,0});
probe(delay_one) -> disjoint({wait,1});
probe(delay_bad_timeout) -> disjoint({wait,invalid});
probe(after_delay_valid) -> disjoint({after_delay,<<0,2,97,98,0,0,0,7>>});
probe(after_delay_short) -> disjoint({after_delay,<<0>>});
probe(infinity_killed) -> kill_delay();
probe(wrong_clause) -> disjoint(invalid);
probe(wait_keeps_message) -> self()!payload,waited=disjoint({wait,0}),receive payload->retained after 0->missing end;
probe(caught_parse) -> try disjoint({parse,<<0>>}) catch error:{badmatch,_}->caught end.
