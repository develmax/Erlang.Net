-module(ordsets372_equality_size).
-export([cases/0,probe/1]).
cases() -> [equal_nil,equal_raw_first,equal_raw_second,equal_raw_both,equal_improper_numeric,equal_improper_diff,equal_head_shortcut,equal_numeric_tail,equal_float_boundary,equal_float_exact_boundary,equal_signed_zero,map_swapped_fun_keys,map_swapped_fun_keys_reverse,map_swapped_fun_bad_values,map_swapped_fun_zero_values,fun_numeric_vs_exact,map_nested_numeric_values,map_nested_exact_keys,map_nested_fun_keys,map_hamt_swapped_fun,size_proper_0,size_improper_0,size_proper_1,size_improper_1,size_proper_15,size_improper_15,size_proper_16,size_improper_16,size_proper_17,size_improper_17,size_proper_31999,size_improper_31999,size_proper_32000,size_improper_32000,size_proper_32001,size_improper_32001,size_proper_64001,size_improper_64001,size_atom,size_tuple,size_binary].
probe(equal_nil) -> ordsets:is_equal([],[]);
probe(equal_raw_first) -> ordsets:is_equal(raw,[]);
probe(equal_raw_second) -> ordsets:is_equal([],raw);
probe(equal_raw_both) -> ordsets:is_equal(raw,other);
probe(equal_improper_numeric) -> ordsets:is_equal([1|2],[1.0|2.0]);
probe(equal_improper_diff) -> ordsets:is_equal([1|left],[1|right]);
probe(equal_head_shortcut) -> ordsets:is_equal([1|left],[2|right]);
probe(equal_numeric_tail) -> ordsets:is_equal([1|2],[1|2.0]);
probe(equal_float_boundary) -> ordsets:is_equal([9007199254740993],[9007199254740992.0]);
probe(equal_float_exact_boundary) -> ordsets:is_equal([9007199254740992],[9007199254740992.0]);
probe(equal_signed_zero) -> ordsets:is_equal([0.0],[-0.0]);
probe(map_swapped_fun_keys) -> {A,B,C,D}=functions(), ordsets:is_equal([#{A=>0,B=>1}],[#{C=>1,D=>0}]);
probe(map_swapped_fun_keys_reverse) -> {A,B,C,D}=functions(), ordsets:is_equal([#{C=>1,D=>0}],[#{A=>0,B=>1}]);
probe(map_swapped_fun_bad_values) -> {A,B,C,D}=functions(), ordsets:is_equal([#{A=>0,B=>1}],[#{C=>0,D=>1}]);
probe(map_swapped_fun_zero_values) -> {A,B,C,D}=functions(), ordsets:is_equal([#{A=>0,B=>0}],[#{C=>0,D=>0}]);
probe(fun_numeric_vs_exact) -> {A,_,_,D}=functions(), {ordsets:is_equal([A],[D]),A=:=D,A==D};
probe(map_nested_numeric_values) -> ordsets:is_equal([#{x=>1,y=>2}],[#{x=>1.0,y=>2.0}]);
probe(map_nested_exact_keys) -> ordsets:is_equal([#{#{x=>1,y=>2}=>0}],[#{#{x=>1.0,y=>2.0}=>0}]);
probe(map_nested_fun_keys) -> {A,B,C,D}=functions(), ordsets:is_equal([#{#{A=>0,B=>1}=>0}],[#{#{C=>1,D=>0}=>0}]);
probe(map_hamt_swapped_fun) -> {A,B,C,D}=functions(), Base=maps:from_list([{I,I} || I<-lists:seq(1,33)]), ordsets:is_equal([Base#{A=>0,B=>1}],[Base#{C=>1,D=>0}]);
probe(size_proper_0) -> ordsets:size(lists:duplicate(0,element));
probe(size_improper_0) -> ordsets:size(improper(0,tail));
probe(size_proper_1) -> ordsets:size(lists:duplicate(1,element));
probe(size_improper_1) -> ordsets:size(improper(1,tail));
probe(size_proper_15) -> ordsets:size(lists:duplicate(15,element));
probe(size_improper_15) -> ordsets:size(improper(15,tail));
probe(size_proper_16) -> ordsets:size(lists:duplicate(16,element));
probe(size_improper_16) -> ordsets:size(improper(16,tail));
probe(size_proper_17) -> ordsets:size(lists:duplicate(17,element));
probe(size_improper_17) -> ordsets:size(improper(17,tail));
probe(size_proper_31999) -> ordsets:size(lists:duplicate(31999,element));
probe(size_improper_31999) -> ordsets:size(improper(31999,tail));
probe(size_proper_32000) -> ordsets:size(lists:duplicate(32000,element));
probe(size_improper_32000) -> ordsets:size(improper(32000,tail));
probe(size_proper_32001) -> ordsets:size(lists:duplicate(32001,element));
probe(size_improper_32001) -> ordsets:size(improper(32001,tail));
probe(size_proper_64001) -> ordsets:size(lists:duplicate(64001,element));
probe(size_improper_64001) -> ordsets:size(improper(64001,tail));
probe(size_atom) -> ordsets:size(raw);
probe(size_tuple) -> ordsets:size({a,b});
probe(size_binary) -> ordsets:size(<<1,2>>).
improper(0,Tail) -> Tail;
improper(N,Tail) -> [element|improper(N-1,Tail)].
functions() -> {stub(0,1),stub(255,2),stub(64,2),stub(128,1)}.
stub(Byte,OldUnique) ->
    Unique=binary:copy(<<Byte>>,16),
    <<131,Module/binary>>=erlang:term_to_binary(ordsets372_map_key_stub),
    <<131,OldIndex/binary>>=erlang:term_to_binary(0),
    <<131,OldUniq/binary>>=erlang:term_to_binary(OldUnique),
    Owner= <<88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>,
    Body= <<0:8,Unique/binary,0:32,0:32,Module/binary,OldIndex/binary,OldUniq/binary,Owner/binary>>,
    Size=byte_size(Body)+4,
    erlang:binary_to_term(<<131,112,Size:32,Body/binary>>).
