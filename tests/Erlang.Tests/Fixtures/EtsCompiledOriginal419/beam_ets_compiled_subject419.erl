-module(beam_ets_compiled_subject419).
-export([cases/0, own_cases/0, expected/1, worker/2, creator/1]).

cases() -> [compile_identity, compile_variable, run_empty, empty_arbitrary_acc, reversed_acc, improper_acc, all_no_match, tuple_bindings, guard_integer, guard_failure, body_failure, constant_tuple, construct_tuple, first_clause, false_then_clause, andalso_guard, orelse_guard, map_pattern, binary_pattern, leading_zero_literal, variable_zero, predicate_atom, predicate_integer, predicate_binary, predicate_list, predicate_reference, roundtrip, creator_exit, compile_nil, compile_atom, compile_improper, compile_clause_arity, compile_guard_atom, compile_guard_improper, compile_body_empty, compile_body_atom, compile_body_improper, compile_unknown, compile_unbound, compile_trace_action, run_bad_program, run_bad_list_and_program].
own_cases() -> [resource_shape,run_bad_list_live,run_improper_live,run_after_trap_live].

worker(Parent,Case) ->
    Outcome = try {returned,probe(Case)} catch C:R:S -> {raised,C,R,S} end,
    Parent ! {Case,Outcome}.

probe(compile_identity) -> compiled([{'_',[],['$_']}], [1,2,3], []);
probe(compile_variable) -> compiled([{'$1',[],['$1']}], [a,{b,2}], []);
probe(run_empty) -> compiled([{'_',[],['$_']}], [], []);
probe(empty_arbitrary_acc) -> compiled([{'_',[],['$_']}], [], {acc,#{a=>1}});
probe(reversed_acc) -> compiled([{'_',[],['$_']}], [1,2], [tail]);
probe(improper_acc) -> compiled([{'_',[],['$_']}], [1,2], tail);
probe(all_no_match) -> compiled([{wanted,[],[true]}], [1,2,3], tail);
probe(tuple_bindings) -> compiled([{{'$2','$1'},[],['$$']}], [{a,b}], []);
probe(guard_integer) -> compiled([{'$1',[{is_integer,'$1'},{'>','$1',1}],['$1']}], [a,1,2,3], []);
probe(guard_failure) -> compiled([{'$1',[{'hd','$1'}],['$_']}], [[],a], []);
probe(body_failure) -> compiled([{'_',[],[{'div',1,0}]}], [a], []);
probe(constant_tuple) -> compiled([{'_',[],[{const,{payload,[1,2]}}]}], [a], []);
probe(construct_tuple) -> compiled([{'$1',[],[{{out,'$1'}}]}], [a,b], []);
probe(first_clause) -> compiled([{'_',[],[first]},{'_',[],[second]}], [a], []);
probe(false_then_clause) -> compiled([{'_',[false],[first]},{'_',[],[second]}], [a], []);
probe(andalso_guard) -> compiled([{'_',[{'andalso',false,{'hd',[]}}],[bad]}], [a], []);
probe(orelse_guard) -> compiled([{'_',[{'orelse',true,{'hd',[]}}],[ok]}], [a], []);
probe(map_pattern) -> compiled([{#{a=>'$1'},[],['$1']}], [#{a=>1,b=>2},#{}], []);
probe(binary_pattern) -> compiled([{<<1,2>>,[],[hit]}], [<<1,2>>,<<1>>], []);
probe(leading_zero_literal) -> compiled([{'$01',[],['$01']}], ['$01',1], []);
probe(variable_zero) -> compiled([{'$0',[],['$0']}], [42], []);
probe(predicate_atom) -> ets:is_compiled_ms(atom);
probe(predicate_integer) -> ets:is_compiled_ms(1);
probe(predicate_binary) -> ets:is_compiled_ms(<<1,2,3>>);
probe(predicate_list) -> ets:is_compiled_ms([]);
probe(predicate_reference) -> ets:is_compiled_ms(make_ref());
probe(roundtrip) -> roundtrip([{'_',[],['$_']}]);
probe(creator_exit) -> creator_exit();
probe(compile_nil) -> ets:match_spec_compile([]);
probe(compile_atom) -> ets:match_spec_compile(atom);
probe(compile_improper) -> ets:match_spec_compile([{'_',[],[true]}|tail]);
probe(compile_clause_arity) -> ets:match_spec_compile([{}]);
probe(compile_guard_atom) -> ets:match_spec_compile([{'_',bad,[true]}]);
probe(compile_guard_improper) -> ets:match_spec_compile([{'_',[true|tail],[true]}]);
probe(compile_body_empty) -> ets:match_spec_compile([{'_',[],[]}]);
probe(compile_body_atom) -> ets:match_spec_compile([{'_',[],body}]);
probe(compile_body_improper) -> ets:match_spec_compile([{'_',[],[true|tail]}]);
probe(compile_unknown) -> ets:match_spec_compile([{'_',[],[{unknown,true}]}]);
probe(compile_unbound) -> ets:match_spec_compile([{'_',[],['$1']}]);
probe(compile_trace_action) -> ets:match_spec_compile([{'_',[],[{message,true}]}]);
probe(run_bad_program) -> ets:match_spec_run_r([],bad_program,[]);
probe(run_bad_list_and_program) -> ets:match_spec_run_r(not_list,bad_program,[]);
probe(resource_shape) ->
    P=ets:match_spec_compile([{'_',[],['$_']}]),
    {own_state,P,is_reference(P),ets:is_compiled_ms(P)};
probe(run_bad_list_live) -> own_failure(not_list);
probe(run_improper_live) -> own_failure([1,2|tail]);
probe(run_after_trap_live) -> own_failure(prefix(4001,tail)).

expected(compile_identity) -> {value,[3,2,1]};
expected(compile_variable) -> {value,[{b,2},a]};
expected(run_empty) -> {value,[]};
expected(empty_arbitrary_acc) -> {value,{acc,#{a=>1}}};
expected(reversed_acc) -> {value,[2,1,tail]};
expected(improper_acc) -> {value,[2,1|tail]};
expected(all_no_match) -> {value,tail};
expected(tuple_bindings) -> {value,[[b,a]]};
expected(guard_integer) -> {value,[3,2]};
expected(guard_failure) -> {value,[]};
expected(body_failure) -> {value,['EXIT']};
expected(constant_tuple) -> {value,[{payload,[1,2]}]};
expected(construct_tuple) -> {value,[{out,b},{out,a}]};
expected(first_clause) -> {value,[first]};
expected(false_then_clause) -> {value,[second]};
expected(andalso_guard) -> {value,[]};
expected(orelse_guard) -> {value,[ok]};
expected(map_pattern) -> {value,[1]};
expected(binary_pattern) -> {value,[hit]};
expected(leading_zero_literal) -> {value,['$01']};
expected(variable_zero) -> {value,[42]};
expected(predicate_atom) -> {value,false};
expected(predicate_integer) -> {value,false};
expected(predicate_binary) -> {value,false};
expected(predicate_list) -> {value,false};
expected(predicate_reference) -> {value,false};
expected(roundtrip) -> {value,{true,true,[2,1]}};
expected(creator_exit) -> {value,{true,[b,a]}};
expected(compile_nil) -> badarg;
expected(compile_atom) -> badarg;
expected(compile_improper) -> badarg;
expected(compile_clause_arity) -> badarg;
expected(compile_guard_atom) -> badarg;
expected(compile_guard_improper) -> badarg;
expected(compile_body_empty) -> badarg;
expected(compile_body_atom) -> badarg;
expected(compile_body_improper) -> badarg;
expected(compile_unknown) -> badarg;
expected(compile_unbound) -> badarg;
expected(compile_trace_action) -> badarg;
expected(run_bad_program) -> badarg;
expected(run_bad_list_and_program) -> badarg.

compiled(Spec,List,Acc) ->
    P=ets:match_spec_compile(Spec),
    true=ets:is_compiled_ms(P),
    ets:match_spec_run_r(List,P,Acc).

roundtrip(Spec) ->
    P=ets:match_spec_compile(Spec),
    Q=binary_to_term(term_to_binary(P)),
    {ets:is_compiled_ms(Q),P=:=Q,ets:match_spec_run_r([1,2],Q,[])}.

creator_exit() ->
    {Pid,Monitor}=spawn_monitor(?MODULE,creator,[self()]),
    receive {program,Pid,P} ->
        receive {'DOWN',Monitor,process,Pid,normal} ->
            {ets:is_compiled_ms(P),ets:match_spec_run_r([a,b],P,[])}
        after 5000 -> erlang:error(creator_down_timeout) end
    after 5000 -> erlang:error(creator_program_timeout) end.

creator(Parent) -> Parent ! {program,self(),ets:match_spec_compile([{'_',[],['$_']}])}.

own_failure(List) ->
    P=ets:match_spec_compile([{{tuple_only},[],[true]}]),
    true=ets:is_compiled_ms(P),
    Result=try {value,ets:match_spec_run_r(List,P,[])} catch C:R:S -> {caught,C,R,S} end,
    {own_state,P,is_reference(P),ets:is_compiled_ms(P),Result}.

prefix(0,Tail) -> Tail;
prefix(N,Tail) -> [N|prefix(N-1,Tail)].
