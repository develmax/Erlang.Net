-module(beam_internal_owner419).
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
 {reset_empty, fun() -> erts_internal:erase_persistent_terms(), {erts_internal:erase_persistent_terms(),persistent_term:get()} end},
 {reset_pairs, fun() -> persistent_term:put(owner_key419,{one,[2,3],<<4,5>>}), persistent_term:put({owner_key419,2},two), {erts_internal:erase_persistent_terms(),persistent_term:get(),persistent_term:get(owner_key419,missing)} end},
 {reset_reuse, fun() -> erts_internal:erase_persistent_terms(), persistent_term:put(owner_key419,first), erts_internal:erase_persistent_terms(), persistent_term:put(owner_key419,second), {persistent_term:get(owner_key419),persistent_term:erase(owner_key419)} end},
 {cursor_end,fun() -> erts_internal:processes_next(1048576) end},
 {cursor_negative,fun() -> erts_internal:processes_next(-1) end},
 {cursor_atom,fun() -> erts_internal:processes_next(invalid) end},
 {cursor_nil,fun() -> erts_internal:processes_next([]) end},
 {cursor_tuple,fun() -> erts_internal:processes_next({0,[]}) end},
 {cursor_float,fun() -> erts_internal:processes_next(0.0) end},
 {cursor_past,fun() -> erts_internal:processes_next(1048577) end},
 {cursor_uint,fun() -> erts_internal:processes_next(1 bsl 32) end},
 {cursor_big,fun() -> erts_internal:processes_next(1 bsl 64) end},
 {debug_atoms,fun() -> erts_internal:notify_breakpoint_hit(a,b,c) end},
 {debug_tuple,fun() -> erts_internal:notify_breakpoint_hit({m,f,0},1,invalid) end},
 {debug_lists,fun() -> erts_internal:notify_breakpoint_hit([],[],[]) end}
].
capture([Dir]) ->
 ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run_child(),[{minor_version,2}])),
 ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks_child(),[{minor_version,2}])), halt().
