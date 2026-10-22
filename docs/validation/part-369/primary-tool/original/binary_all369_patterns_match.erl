-module(binary_all369_patterns_match).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [compile_single,compile_singleton,compile_multiple,compile_duplicates,compile_emptylist,compile_emptybinary,compile_emptymember,compile_improper,compile_bitmember,compile_atom,compile_forgedtuple,match2_pattern_single,match2_pattern_singleton,match2_pattern_multiple,match2_pattern_duplicates,match2_pattern_emptylist,match2_pattern_emptybinary,match2_pattern_emptymember,match2_pattern_improper,match2_pattern_bitmember,match2_pattern_atom,match2_pattern_forgedtuple,match2_subject_empty,match2_subject_bits9,match2_subject_atom,match2_subject_offset3,match2_cp_bm,match2_cp_restored_bm,match2_cp_ac,match2_cp_restored_ac,match3_options_none,match3_options_empty_scope,match3_options_scope,match3_options_negative_scope,match3_options_scope_last,match3_options_invalid_then_valid,match3_options_improper_tail,match3_options_atom,match3_options_unknown,match3_options_outside,match3_options_float,match3_options_tuple3,match3_precedence,match2_left_longest,match2_nonoverlap,match2_resource_ordinary,match2_resource_wrong_tag,match_long_1,match_long_31,match_long_32,match_long_33,match_long_64,match_long_65,match_long_257].
run() -> binary_all369_support:run(binary_all369_patterns_match,cases()).
run_stacks() -> binary_all369_support:profiles(binary_all369_patterns_match,cases()).
probe(compile_single) -> binary_all369_support:compiled_observable(<<"ab">>);
probe(compile_singleton) -> binary_all369_support:compiled_observable([<<"ab">>]);
probe(compile_multiple) -> binary_all369_support:compiled_observable([<<"ab">>,<<"abc">>]);
probe(compile_duplicates) -> binary_all369_support:compiled_observable([<<"a">>,<<"a">>]);
probe(compile_emptylist) -> binary_all369_support:compiled_observable([]);
probe(compile_emptybinary) -> binary_all369_support:compiled_observable(<<>>);
probe(compile_emptymember) -> binary_all369_support:compiled_observable([<<"a">>,<<>>]);
probe(compile_improper) -> binary_all369_support:compiled_observable([<<"a">>|bad]);
probe(compile_bitmember) -> binary_all369_support:compiled_observable([<<0:9>>]);
probe(compile_atom) -> binary_all369_support:compiled_observable(bad);
probe(compile_forgedtuple) -> binary_all369_support:compiled_observable({bm,bad});
probe(match2_pattern_single) -> binary:match(<<"abcabc">>,<<"ab">>);
probe(match2_pattern_singleton) -> binary:match(<<"abcabc">>,[<<"ab">>]);
probe(match2_pattern_multiple) -> binary:match(<<"abcabc">>,[<<"ab">>,<<"abc">>]);
probe(match2_pattern_duplicates) -> binary:match(<<"abcabc">>,[<<"a">>,<<"a">>]);
probe(match2_pattern_emptylist) -> binary:match(<<"abcabc">>,[]);
probe(match2_pattern_emptybinary) -> binary:match(<<"abcabc">>,<<>>);
probe(match2_pattern_emptymember) -> binary:match(<<"abcabc">>,[<<"a">>,<<>>]);
probe(match2_pattern_improper) -> binary:match(<<"abcabc">>,[<<"a">>|bad]);
probe(match2_pattern_bitmember) -> binary:match(<<"abcabc">>,[<<0:9>>]);
probe(match2_pattern_atom) -> binary:match(<<"abcabc">>,bad);
probe(match2_pattern_forgedtuple) -> binary:match(<<"abcabc">>,{bm,bad});
probe(match2_subject_empty) -> binary:match(<<>>,<<"a">>);
probe(match2_subject_bits9) -> binary:match(<<0:9>>,<<"a">>);
probe(match2_subject_atom) -> binary:match(bad,<<"a">>);
probe(match2_subject_offset3) -> binary:match(binary_all369_support:shifted(<<"abcabc">>),<<"a">>);
probe(match2_cp_bm) -> CP = binary:compile_pattern(<<"a">>), binary:match(<<"abcabc">>,CP);
probe(match2_cp_restored_bm) -> CP = binary:compile_pattern(<<"a">>), Restored = binary_to_term(term_to_binary(CP)), {CP =:= Restored,binary:match(<<"abcabc">>,Restored)};
probe(match2_cp_ac) -> CP = binary:compile_pattern([<<"a">>,<<"ab">>]), binary:match(<<"abcabc">>,CP);
probe(match2_cp_restored_ac) -> CP = binary:compile_pattern([<<"a">>,<<"ab">>]), Restored = binary_to_term(term_to_binary(CP)), {CP =:= Restored,binary:match(<<"abcabc">>,Restored)};
probe(match3_options_none) -> binary:match(<<"abcabc">>,<<"a">>,[]);
probe(match3_options_empty_scope) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{0,0}}]);
probe(match3_options_scope) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{1,3}}]);
probe(match3_options_negative_scope) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{4,-3}}]);
probe(match3_options_scope_last) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{0,1}},{scope,{2,3}}]);
probe(match3_options_invalid_then_valid) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,bad},{scope,{0,1}}]);
probe(match3_options_improper_tail) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{0,1}}|tail]);
probe(match3_options_atom) -> binary:match(<<"abcabc">>,<<"a">>,tail);
probe(match3_options_unknown) -> binary:match(<<"abcabc">>,<<"a">>,[bad]);
probe(match3_options_outside) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{0,99}}]);
probe(match3_options_float) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{0.0,1}}]);
probe(match3_options_tuple3) -> binary:match(<<"abcabc">>,<<"a">>,[{scope,{0,1,2}}]);
probe(match3_precedence) -> binary:match(bad,<<>>,[bad]);
probe(match2_left_longest) -> binary:match(<<"ababa">>,[<<"a">>,<<"aba">>,<<"ab">>]);
probe(match2_nonoverlap) -> binary:match(<<"aaaaa">>,<<"aa">>);
probe(match2_resource_ordinary) -> Pattern = binary_all369_support:ordinary_reference(), binary:match(<<"abcabc">>,Pattern);
probe(match2_resource_wrong_tag) -> Pattern = binary_all369_support:wrong_tag_pattern(), binary:match(<<"abcabc">>,Pattern);
probe(match_long_1) -> B = binary:copy(<<"a">>,1), binary:match(<<B/binary,"b">>,<<"ab">>);
probe(match_long_31) -> B = binary:copy(<<"a">>,31), binary:match(<<B/binary,"b">>,<<"ab">>);
probe(match_long_32) -> B = binary:copy(<<"a">>,32), binary:match(<<B/binary,"b">>,<<"ab">>);
probe(match_long_33) -> B = binary:copy(<<"a">>,33), binary:match(<<B/binary,"b">>,<<"ab">>);
probe(match_long_64) -> B = binary:copy(<<"a">>,64), binary:match(<<B/binary,"b">>,<<"ab">>);
probe(match_long_65) -> B = binary:copy(<<"a">>,65), binary:match(<<B/binary,"b">>,<<"ab">>);
probe(match_long_257) -> B = binary:copy(<<"a">>,257), binary:match(<<B/binary,"b">>,<<"ab">>).
