-module(beam_writable_receive411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [after_empty,after_bytes,after_tail,after_bad_list,before_empty,before_bytes,retained_wrong,retained_binary,append_alias,append_retry,after_bad_byte,build_again].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D) || D<-[default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C) -> V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
after_build() -> receive {go,L}->build(L,<<>>) end.
before_build(L) -> B=build(L,<<>>),receive go->B end.
build([],B) -> B;
build([H|T],B) -> build(T,<<B/binary,H>>).
aliases(L) -> B=build(L,<<>>),self()!unrelated,self()!{go,B},receive {go,B}->C=build([9,10],B),{B,C} end.
probe(after_empty) -> self()!{go,[]},after_build();
probe(after_bytes) -> self()!{go,[1,2,255]},after_build();
probe(after_tail) -> self()!{go,[1|bad]},after_build();
probe(after_bad_list) -> self()!{go,bad},after_build();
probe(before_empty) -> self()!go,before_build([]);
probe(before_bytes) -> self()!go,before_build([1,2,255]);
probe(retained_wrong) -> self()!unrelated,self()!{go,[3,4]},B=after_build(),receive unrelated->{B,retained} after 0->missing end;
probe(retained_binary) -> Old=build([5,6],<<>>),self()!{skip,Old},self()!{go,[3,4]},B=after_build(),receive {skip,Old}->{Old,B} after 0->missing end;
probe(append_alias) -> aliases([1,2]);
probe(append_retry) -> Old=build([1,2],<<>>),self()!{skip,Old},self()!go,B=before_build([3]),C=build([4],B),receive {skip,Old}->{Old,B,C} end;
probe(after_bad_byte) -> self()!{go,[bad]},after_build();
probe(build_again) -> self()!{go,[1]},A=after_build(),self()!{go,[2]},B=after_build(),{A,B}.
