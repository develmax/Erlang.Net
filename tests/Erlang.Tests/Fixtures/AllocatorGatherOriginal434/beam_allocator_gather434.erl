-module(beam_allocator_gather434).
-export([run/0,run_stacks/0,run_own/0,capture/1,observer_child/3]).
cases() -> [hist_sys,hist_sys_zero,hist_sys_flags,hist_bad_atom,hist_bad_tuple,hist_bad_type,hist_bad_aux_low,hist_bad_aux_high,carrier_sys,carrier_sys_zero,carrier_sys_flags,carrier_bad_atom,carrier_bad_tuple,carrier_bad_type,carrier_bad_aux_low,carrier_bad_aux_high].
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
probe(hist_sys) -> erts_internal:gather_alloc_histograms({sys_alloc,0,8,16,0,make_ref()});
probe(hist_sys_zero) -> erts_internal:gather_alloc_histograms({sys_alloc,0,0,0,0,make_ref()});
probe(hist_sys_flags) -> erts_internal:gather_alloc_histograms({sys_alloc,0,8,16,7,make_ref()});
probe(hist_bad_atom) -> erts_internal:gather_alloc_histograms(bad);
probe(hist_bad_tuple) -> erts_internal:gather_alloc_histograms({sys_alloc,0,8,16,0});
probe(hist_bad_type) -> erts_internal:gather_alloc_histograms({unknown_allocator434,0,8,16,0,no_ref434});
probe(hist_bad_aux_low) -> erts_internal:gather_alloc_histograms({std_alloc,-1,8,16,0,no_ref434});
probe(hist_bad_aux_high) -> erts_internal:gather_alloc_histograms({std_alloc,999999,8,16,0,no_ref434});
probe(carrier_sys) -> erts_internal:gather_carrier_info({sys_alloc,0,8,16,0,make_ref()});
probe(carrier_sys_zero) -> erts_internal:gather_carrier_info({sys_alloc,0,0,0,0,make_ref()});
probe(carrier_sys_flags) -> erts_internal:gather_carrier_info({sys_alloc,0,8,16,7,make_ref()});
probe(carrier_bad_atom) -> erts_internal:gather_carrier_info(bad);
probe(carrier_bad_tuple) -> erts_internal:gather_carrier_info({sys_alloc,0,8,16,0});
probe(carrier_bad_type) -> erts_internal:gather_carrier_info({unknown_allocator434,0,8,16,0,no_ref434});
probe(carrier_bad_aux_low) -> erts_internal:gather_carrier_info({std_alloc,-1,8,16,0,no_ref434});
probe(carrier_bad_aux_high) -> erts_internal:gather_carrier_info({std_alloc,999999,8,16,0,no_ref434}).
run_own() -> [{histograms,own(gather_alloc_histograms)},{carriers,own(gather_carrier_info)}].
own(F) ->
 R=make_ref(),N=apply(erts_internal,F,[{std_alloc,0,8,16,0,R}]),
 {N,gather(R,N,[])}.
gather(_R,0,Rows) -> Rows;
gather(R,N,Rows) ->
 receive T when is_tuple(T),element(1,T)=:=R -> gather(R,N-1,[T|Rows])
 after 10000 -> erlang:error(allocator_reply_timeout) end.
capture([Out]) ->
 ok=file:write_file(Out++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),
 ok=file:write_file(Out++"/own.etf",term_to_binary(run_own(),[{minor_version,2}])),halt().
