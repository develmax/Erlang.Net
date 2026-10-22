-module(beam_ddll_boundary443).
-export([cases/0,run/1,run_stacks/1,capture_values/1,capture_stacks/1,observer_child/4]).
cases() -> [success_keep,success_immediate,success_deeper,success_both,
            fail_keep,fail_immediate,fail_deeper,fail_both,bad_load_options,bad_unload_options].
run(Path) -> [{C,observe(C,Path,default)} || C <- cases()].
run_stacks(Path) -> [{D,actual_depth(D),[{C,observe(C,Path,D)} || C <- cases()]} || D <- [default,0,1,2,8]].
actual_depth(default) -> V=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,V),V;
actual_depth(D) -> D.
observe(C,Path,D) ->
 {P,R}=spawn_monitor(?MODULE,observer_child,[self(),C,Path,D]),
 receive {P,V} -> receive {'DOWN',R,process,P,normal} -> V end;
         {'DOWN',R,process,P,Why} -> erlang:error({unexpected_observer_down,C,Why})
 after 10000 -> exit(P,kill),receive {'DOWN',R,process,P,_}->ok end,erlang:error({observer_timeout,C}) end.
observer_child(Parent,C,Path,D) ->
 case D of default -> ok; _ -> erlang:system_flag(backtrace_depth,D) end,
 V=try probe(C,Path) of X -> {value,X} catch K:R:S -> {exception,K,R,S} end,
 cleanup(),Parent!{self(),V}.
probe(bad_load_options,_Path) -> erl_ddll:try_load(bad_path,ddll_boundary443,[bad]);
probe(bad_unload_options,_Path) -> erl_ddll:try_unload(ddll_boundary443,[bad]);
probe(C,Path) ->
 Loaded=erl_ddll:try_load(Path,ddll_boundary443,[]),
 P=open_port({spawn_driver,"ddll_boundary443"},[binary]),
 Result=try
   Before=snapshot(),
   Bytes=port_control(P,4430+mode(C),<<>>),
   {Loaded,Before,Bytes,snapshot()}
 after port_close(P) end,
 Cleanup=cleanup(),{Result,Cleanup,snapshot()}.
mode(success_keep)->0; mode(success_immediate)->1; mode(success_deeper)->2; mode(success_both)->3;
mode(fail_keep)->4; mode(fail_immediate)->5; mode(fail_deeper)->6; mode(fail_both)->7.
names()->[ddll_boundary443,ddll_boundary443_a,ddll_boundary443_b,ddll_boundary443_c,ddll_boundary443_d].
snapshot()->{[{N,linked(N)} || N<-names()],filtered(erl_ddll:loaded_drivers())}.
linked(N)->try erl_ddll:info(N,linked_in_driver) of X->{present,X} catch error:badarg->absent end.
filtered({ok,Drivers})->filter(Drivers).
filter([])->[];
filter([N|Rest])->case member(N,["ddll_boundary443","ddll_boundary443_a","ddll_boundary443_b","ddll_boundary443_c","ddll_boundary443_d"]) of
 true->[N|filter(Rest)];false->filter(Rest) end.
member(_,[])->false;member(X,[X|_])->true;member(X,[_|Rest])->member(X,Rest).
cleanup()->
 R=erl_ddll:monitor(driver,{ddll_boundary443,unloaded_only}),U=erl_ddll:try_unload(ddll_boundary443,[]),
 receive {'DOWN',R,driver,_,unloaded}->{U,unloaded}
 after 5000->erlang:error(cleanup_timeout) end.
capture_values([Out,Path])->ok=file:write_file(Out++"/values.etf",term_to_binary(run(Path),[{minor_version,2}])),halt().
capture_stacks([Out,Path])->ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(Path),[{minor_version,2}])),halt().
