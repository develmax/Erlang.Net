-module(proplists_fun_semantics).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [is_function_2,deterministic_info,pair_update,atom_update,remove_singleton,remove_triple,ignored_term_seed,ignored_atom_seed_error,invalid_pair_accumulator,same_fun_identity,different_fun_identity,roundtrip_default_apply,roundtrip_safe_apply,roundtrip_identity,pid_self_relationship,code_info_relationships,different_local_identity,raw_fun_identities,raw_fun_info_all,raw_wire].
run() -> [{N,capture(N,false)} || N <- cases()].
run_stacks() ->
    Names=cases(),
    Default={default,[{N,capture(N,true)} || N <- Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try [Default|[at_depth(D,Names) || D <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old)
    end.
at_depth(D,Names) ->
    erlang:system_flag(backtrace_depth,D),
    {D,[{N,capture(N,true)} || N <- Names]}.
capture(N,Stacks) ->
    try run_case(N) of V -> {ok,V}
    catch C:R:S -> case Stacks of true -> {error,C,R,S}; false -> {error,C,R} end
    end.
callback() ->
    try proplists:to_map(bad) of V -> erlang:error({missing_foldr_error,V})
    catch error:function_clause:Frames -> find_callback(Frames)
    end.
find_callback([{lists,foldr_1,[F,_,_],_}|_]) when is_function(F,2) -> F;
find_callback([_|Rest]) -> find_callback(Rest);
find_callback([]) -> erlang:error(missing_foldr_callback).
run_case(is_function_2) -> F=callback(), {is_function(F),is_function(F,2),is_function(F,1)};
run_case(deterministic_info) ->
    F=callback(), [erlang:fun_info(F,K) || K <- [type,module,name,arity,env]];
run_case(pair_update) -> F=callback(), F({k,new},#{k=>old,prior=>keep});
run_case(atom_update) -> F=callback(), F(k,#{k=>old,prior=>keep});
run_case(remove_singleton) -> F=callback(), F({k},#{k=>old,prior=>keep});
run_case(remove_triple) -> F=callback(), F({k,x,y},#{k=>old,prior=>keep});
run_case(ignored_term_seed) -> F=callback(), F(123,seed);
run_case(ignored_atom_seed_error) -> F=callback(), F(ignored,seed);
run_case(invalid_pair_accumulator) -> F=callback(), F({k,v},seed);
run_case(same_fun_identity) -> F=callback(), G=callback(), F=:=G;
run_case(different_fun_identity) -> F=callback(), F=/=fun proplists:property/1;
run_case(roundtrip_default_apply) ->
    F=callback(), G=binary_to_term(term_to_binary(F)), G({k,v},#{});
run_case(roundtrip_safe_apply) ->
    F=callback(), G=binary_to_term(term_to_binary(F),[safe]), G({k,v},#{});
run_case(roundtrip_identity) ->
    F=callback(), B=term_to_binary(F), G=binary_to_term(B), H=binary_to_term(B,[safe]),
    {F=:=G,F=:=H,erlang:fun_info(F,pid)=:=erlang:fun_info(G,pid),erlang:fun_info(F,new_uniq)=:=erlang:fun_info(G,new_uniq)};
run_case(pid_self_relationship) -> F=callback(), erlang:fun_info(F,pid)=:={pid,self()};
run_case(code_info_relationships) ->
    F=callback(), G=binary_to_term(term_to_binary(F),[safe]),
    [{K,erlang:fun_info(F,K)=:=erlang:fun_info(G,K)} || K <- [pid,index,new_index,uniq,new_uniq,type,module,name,arity,env]];
run_case(different_local_identity) -> F=callback(), G=fun(A,B) -> {A,B} end, {is_function(G,2),F=/=G};
run_case(raw_fun_identities) -> F=callback(), G=callback(), {F,G,binary_to_term(term_to_binary(F))};
run_case(raw_fun_info_all) -> F=callback(), erlang:fun_info(F);
run_case(raw_wire) -> term_to_binary(callback()).
