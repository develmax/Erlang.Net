-module(beam_internal_text_aux430).
-export([run/0,run_stacks/0,capture/1,observer_child/3]).
cases() -> [integer,large_integer,atom_budget,unicode_atom,empty_atom,tuple_two,tuple_three,
 tuple_four,list_budget,string_budget,string_exact,binary_quoted,binary_numeric,partial_bits,
 full_plus_tail,map_budget,external_fun,float,undefined_limit,maximum_limit,bad_zero,bad_one,
 bad_negative,bad_maximum,bad_float,bad_atom,bad_list,pid_text,reference_text,aux_participants].
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
probe(integer) -> erts_internal:term_to_string(123,2);
probe(large_integer) -> erts_internal:term_to_string(1180591620717411303424,2);
probe(atom_budget) -> erts_internal:term_to_string(abc,2);
probe(unicode_atom) -> erts_internal:term_to_string('λ',3);
probe(empty_atom) -> erts_internal:term_to_string('',2);
probe(tuple_two) -> erts_internal:term_to_string({1,2},2);
probe(tuple_three) -> erts_internal:term_to_string({1,2},3);
probe(tuple_four) -> erts_internal:term_to_string({1,2},4);
probe(list_budget) -> erts_internal:term_to_string([1,2,3],2);
probe(string_budget) -> erts_internal:term_to_string("abc",2);
probe(string_exact) -> erts_internal:term_to_string("abc",4);
probe(binary_quoted) -> erts_internal:term_to_string(<<"abc">>,2);
probe(binary_numeric) -> erts_internal:term_to_string(<<0,1,2>>,2);
probe(partial_bits) -> erts_internal:term_to_string(<<5:3>>,2);
probe(full_plus_tail) -> erts_internal:term_to_string(<<1,5:3>>,2);
probe(map_budget) -> erts_internal:term_to_string(#{1=>{2,3}},3);
probe(external_fun) -> erts_internal:term_to_string(fun erlang:length/1,2);
probe(float) -> erts_internal:term_to_string(1.25,2);
probe(undefined_limit) -> erts_internal:term_to_string({1,[2|3],<<0,255>>},undefined);
probe(maximum_limit) -> erts_internal:term_to_string(abc,2147483646);
probe(bad_zero) -> erts_internal:term_to_string(foo,0);
probe(bad_one) -> erts_internal:term_to_string(foo,1);
probe(bad_negative) -> erts_internal:term_to_string(foo,-1);
probe(bad_maximum) -> erts_internal:term_to_string(foo,2147483647);
probe(bad_float) -> erts_internal:term_to_string(foo,2.0);
probe(bad_atom) -> erts_internal:term_to_string(foo,bad);
probe(bad_list) -> erts_internal:term_to_string(foo,[]);
probe(pid_text) -> P=self(),erts_internal:term_to_string(P,undefined)=:=pid_to_list(P);
probe(reference_text) -> R=make_ref(),erts_internal:term_to_string(R,undefined)=:=ref_to_list(R);
probe(aux_participants) -> erts_internal:no_aux_work_threads().
capture([Out]) ->
 ok=file:write_file(Out++"/values.etf",term_to_binary(run(),[{minor_version,2}])),
 ok=file:write_file(Out++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),halt().
