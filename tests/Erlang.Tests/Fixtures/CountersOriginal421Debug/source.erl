-module(beam_counters421).
-export([run/0,run_stacks/0,capture/1,run_child/0,run_stacks_child/0,child/2]).
run_child() -> in_child(run).
run_stacks_child() -> in_child(run_stacks).
in_child(Function) ->
    {Pid,Ref}=erlang:spawn_monitor(?MODULE,child,[self(),Function]),
    receive {Pid,Result} ->
        receive {'DOWN',Ref,process,Pid,normal} -> Result end
    end.
child(Parent,Function) ->
    Result=?MODULE:Function(),
    Parent ! {self(),Result}.
run() -> [{K, observe(F)} || {K,F} <- cases()].
run_stacks() ->
    Old=erlang:system_flag(backtrace_depth,8),
    try [profile(P) || P <- [default,0,1,2,8]]
    after erlang:system_flag(backtrace_depth,Old) end.
profile(default) -> {default,8,run()};
profile(N) -> erlang:system_flag(backtrace_depth,N), {N,N,run()}.
observe(F) -> try {ok,F()} catch C:R:S -> {C,R,S} end.
cases() -> [
 {new_zero,fun() -> erts_internal:counters_new(0) end},
 {new_negative,fun() -> erts_internal:counters_new(-1) end},
 {new_atom,fun() -> erts_internal:counters_new(invalid) end},
 {new_float,fun() -> erts_internal:counters_new(1.0) end},
 {new_nil,fun() -> erts_internal:counters_new([]) end},
 {new_tuple,fun() -> erts_internal:counters_new({1}) end},
 {new_past_word,fun() -> erts_internal:counters_new(1 bsl 64) end},
 {new_system_limit,fun() -> erts_internal:counters_new(1 bsl 63) end},
 {get_atom,fun() -> erts_internal:counters_get(invalid,1) end},
 {get_nil,fun() -> erts_internal:counters_get([],1) end},
 {get_integer,fun() -> erts_internal:counters_get(1,1) end},
 {get_tuple,fun() -> erts_internal:counters_get({counter,1},1) end},
 {add_atom,fun() -> erts_internal:counters_add(invalid,1,1) end},
 {add_nil,fun() -> erts_internal:counters_add([],1,1) end},
 {add_integer,fun() -> erts_internal:counters_add(1,1,1) end},
 {add_tuple,fun() -> erts_internal:counters_add({counter,1},1,1) end},
 {put_atom,fun() -> erts_internal:counters_put(invalid,1,1) end},
 {put_nil,fun() -> erts_internal:counters_put([],1,1) end},
 {put_integer,fun() -> erts_internal:counters_put(1,1,1) end},
 {put_tuple,fun() -> erts_internal:counters_put({counter,1},1,1) end},
 {info_atom,fun() -> erts_internal:counters_info(invalid) end},
 {info_nil,fun() -> erts_internal:counters_info([]) end},
 {info_integer,fun() -> erts_internal:counters_info(1) end},
 {info_tuple,fun() -> erts_internal:counters_info({counter,1}) end},
 {info_1,fun() -> R=erts_internal:counters_new(1), erts_internal:counters_info(R) end},
 {info_8,fun() -> R=erts_internal:counters_new(8), erts_internal:counters_info(R) end},
 {info_9,fun() -> R=erts_internal:counters_new(9), erts_internal:counters_info(R) end},
 {info_17,fun() -> R=erts_internal:counters_new(17), erts_internal:counters_info(R) end},
 {initial_cells,fun() -> R=erts_internal:counters_new(17), {erts_internal:counters_get(R,1),erts_internal:counters_get(R,8),erts_internal:counters_get(R,9),erts_internal:counters_get(R,17)} end},
 {add_put,fun() -> R=erts_internal:counters_new(1), A=erts_internal:counters_add(R,1,7), B=erts_internal:counters_put(R,1,-3), C=erts_internal:counters_add(R,1,5), {A,B,C,erts_internal:counters_get(R,1)} end},
 {signed_wrap,fun() -> R=erts_internal:counters_new(1), erts_internal:counters_put(R,1,(1 bsl 63)-1), erts_internal:counters_add(R,1,1), A=erts_internal:counters_get(R,1), erts_internal:counters_add(R,1,-1), {A,erts_internal:counters_get(R,1)} end},
 {uint_increment,fun() -> R=erts_internal:counters_new(1), erts_internal:counters_add(R,1,(1 bsl 64)-1), A=erts_internal:counters_get(R,1), erts_internal:counters_put(R,1,-(1 bsl 63)), erts_internal:counters_add(R,1,(1 bsl 64)-1), {A,erts_internal:counters_get(R,1)} end}
].
capture([Dir]) ->
 ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run_child(),[{minor_version,2}])),
 ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks_child(),[{minor_version,2}])), halt().
