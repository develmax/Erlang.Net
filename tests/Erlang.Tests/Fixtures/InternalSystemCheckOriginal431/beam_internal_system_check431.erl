-module(beam_internal_system_check431).
-export([run/0,run_stacks/0,capture/1,observer_child/3]).
cases() -> [schedulers,twice,mailbox_preserved,bad_atom,bad_integer,bad_tuple,bad_list,bad_binary,bad_bits,bad_case].
run() -> [{C,observe(C,default)} || C <- cases()].
run_stacks() -> [{D,actual_depth(D),[{C,observe(C,D)} || C <- cases()]} || D <- [default,0,1,2,8]].
actual_depth(default) -> V=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,V),V;
actual_depth(D) -> D.
observe(C,D) ->
 {P,R}=spawn_monitor(?MODULE,observer_child,[self(),C,D]),
 receive {P,V} -> receive {'DOWN',R,process,P,normal} -> V end;
         {'DOWN',R,process,P,Why} -> {unexpected_down,Why}
 after 10000 -> exit(P,kill),erlang:error({observer_timeout,C}) end.
observer_child(Parent,C,D) ->
 case D of default -> ok; _ -> erlang:system_flag(backtrace_depth,D) end,
 V=try probe(C) of X -> {value,X} catch K:R:S -> {exception,K,R,S} end,
 Parent!{self(),V}.
probe(schedulers) -> erts_internal:system_check(schedulers);
probe(twice) -> {erts_internal:system_check(schedulers),erts_internal:system_check(schedulers)};
probe(mailbox_preserved) -> self()!preserved431,ok=erts_internal:system_check(schedulers),receive preserved431 -> preserved end;
probe(bad_atom) -> erts_internal:system_check(invalid);
probe(bad_integer) -> erts_internal:system_check(17);
probe(bad_tuple) -> erts_internal:system_check({schedulers});
probe(bad_list) -> erts_internal:system_check([schedulers]);
probe(bad_binary) -> erts_internal:system_check(<<"schedulers">>);
probe(bad_bits) -> erts_internal:system_check(<<1:1>>);
probe(bad_case) -> erts_internal:system_check('Schedulers').
capture([Out]) ->
 ok=file:write_file(Out++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),halt().
