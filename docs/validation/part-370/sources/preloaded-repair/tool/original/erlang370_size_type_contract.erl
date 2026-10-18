-module(erlang370_size_type_contract).
-export([cases/0,run/0,run_stacks/0,probe/1]).
-record(rec,{value}).
cases() -> [length_nil,length_proper,length_improper,length_atom,length_integer,length_float,length_tuple,length_empty_tuple,length_map,length_binary,length_partial,length_pid,length_reference,length_function,tuple_size_nil,tuple_size_proper,tuple_size_improper,tuple_size_atom,tuple_size_integer,tuple_size_float,tuple_size_tuple,tuple_size_empty_tuple,tuple_size_map,tuple_size_binary,tuple_size_partial,tuple_size_pid,tuple_size_reference,tuple_size_function,bit_size_nil,bit_size_proper,bit_size_improper,bit_size_atom,bit_size_integer,bit_size_float,bit_size_tuple,bit_size_empty_tuple,bit_size_map,bit_size_binary,bit_size_partial,bit_size_pid,bit_size_reference,bit_size_function,byte_size_nil,byte_size_proper,byte_size_improper,byte_size_atom,byte_size_integer,byte_size_float,byte_size_tuple,byte_size_empty_tuple,byte_size_map,byte_size_binary,byte_size_partial,byte_size_pid,byte_size_reference,byte_size_function,map_size_nil,map_size_proper,map_size_improper,map_size_atom,map_size_integer,map_size_float,map_size_tuple,map_size_empty_tuple,map_size_map,map_size_binary,map_size_partial,map_size_pid,map_size_reference,map_size_function,size_nil,size_proper,size_improper,size_atom,size_integer,size_float,size_tuple,size_empty_tuple,size_map,size_binary,size_partial,size_pid,size_reference,size_function,bit_size_bits_0,byte_size_bits_0,size_bits_0,bit_size_bits_1,byte_size_bits_1,size_bits_1,bit_size_bits_7,byte_size_bits_7,size_bits_7,bit_size_bits_8,byte_size_bits_8,size_bits_8,bit_size_bits_9,byte_size_bits_9,size_bits_9,bit_size_bits_511,byte_size_bits_511,size_bits_511,bit_size_bits_512,byte_size_bits_512,size_bits_512,bit_size_bits_513,byte_size_bits_513,size_bits_513,bit_size_bits_1025,byte_size_bits_1025,size_bits_1025,bit_size_unaligned_view,byte_size_unaligned_view,size_unaligned_view,map_size_0,map_size_1,map_size_31,map_size_32,map_size_33,map_size_257,length_proper_257,length_improper_257,length_proper_64017,length_improper_64017,is_function_external_zero,is_function_nonfun_zero,is_function_external_one,is_function_nonfun_one,is_function_external_255,is_function_nonfun_255,is_function_external_256,is_function_nonfun_256,is_function_external_maxsmall,is_function_nonfun_maxsmall,is_function_external_big,is_function_nonfun_big,is_function_external_negative,is_function_nonfun_negative,is_function_external_negative_big,is_function_nonfun_negative_big,is_function_external_float,is_function_nonfun_float,is_function_external_atom,is_function_nonfun_atom,is_integer_range_inside,is_integer_range_minimum,is_integer_range_maximum,is_integer_range_outside,is_integer_range_reversed,is_integer_range_big,is_integer_range_negative_big,is_integer_range_noninteger,is_integer_range_invalid_min,is_integer_range_invalid_max,is_integer_range_both_invalid,is_integer_range_float_min,is_map_key_present,is_map_key_absent,is_map_key_exact_int,is_map_key_exact_float,is_map_key_tuple,is_map_key_improper,is_map_key_invalid_atom,is_map_key_invalid_tuple,is_map_key_large_present,is_map_key_large_absent,is_record_one_nil,is_record_one_proper,is_record_one_improper,is_record_one_atom,is_record_one_integer,is_record_one_float,is_record_one_tuple,is_record_one_empty_tuple,is_record_one_map,is_record_one_binary,is_record_one_partial,is_record_one_pid,is_record_one_reference,is_record_one_function,is_record_two_tagged,is_record_two_tag_only,is_record_two_empty,is_record_two_wrong_tag,is_record_two_not_tuple,is_record_two_invalid_tag,is_record_three_two,is_record_three_one,is_record_three_zero,is_record_three_negative,is_record_three_minimum,is_record_three_maximum,is_record_three_positive_big,is_record_three_negative_big,is_record_three_float,is_record_three_module_name,is_record_three_bad_tag,native_record_197,native_record_198,native_record_199,native_record_200,guard_failure_length,guard_failure_tuple_size,guard_failure_bit_size,guard_failure_byte_size,guard_failure_map_size,guard_failure_size,non_tail_error_length,non_tail_error_tuple_size,non_tail_error_bit_size,non_tail_error_byte_size,non_tail_error_map_size,non_tail_error_size,compiled_record_correct,compiled_record_wrong_size,direct_record_positive_big,direct_record_negative_big].
run() -> erlang370_pure_support:run(erlang370_size_type_contract,cases()).
run_stacks() -> erlang370_pure_support:profiles(erlang370_size_type_contract,cases()).
probe(length_nil) -> erlang370_pure_support:invoke(length,[[]]);
probe(length_proper) -> erlang370_pure_support:invoke(length,[[a,b,c]]);
probe(length_improper) -> erlang370_pure_support:invoke(length,[[a|tail]]);
probe(length_atom) -> erlang370_pure_support:invoke(length,[value]);
probe(length_integer) -> erlang370_pure_support:invoke(length,[42]);
probe(length_float) -> erlang370_pure_support:invoke(length,[1.5]);
probe(length_tuple) -> erlang370_pure_support:invoke(length,[{a,b}]);
probe(length_empty_tuple) -> erlang370_pure_support:invoke(length,[{}]);
probe(length_map) -> erlang370_pure_support:invoke(length,[#{a=>1}]);
probe(length_binary) -> erlang370_pure_support:invoke(length,[<<1,2,3>>]);
probe(length_partial) -> erlang370_pure_support:invoke(length,[<<1:3>>]);
probe(length_pid) -> erlang370_pure_support:invoke(length,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(length_reference) -> erlang370_pure_support:invoke(length,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(length_function) -> erlang370_pure_support:invoke(length,[fun erlang:self/0]);
probe(tuple_size_nil) -> erlang370_pure_support:invoke(tuple_size,[[]]);
probe(tuple_size_proper) -> erlang370_pure_support:invoke(tuple_size,[[a,b,c]]);
probe(tuple_size_improper) -> erlang370_pure_support:invoke(tuple_size,[[a|tail]]);
probe(tuple_size_atom) -> erlang370_pure_support:invoke(tuple_size,[value]);
probe(tuple_size_integer) -> erlang370_pure_support:invoke(tuple_size,[42]);
probe(tuple_size_float) -> erlang370_pure_support:invoke(tuple_size,[1.5]);
probe(tuple_size_tuple) -> erlang370_pure_support:invoke(tuple_size,[{a,b}]);
probe(tuple_size_empty_tuple) -> erlang370_pure_support:invoke(tuple_size,[{}]);
probe(tuple_size_map) -> erlang370_pure_support:invoke(tuple_size,[#{a=>1}]);
probe(tuple_size_binary) -> erlang370_pure_support:invoke(tuple_size,[<<1,2,3>>]);
probe(tuple_size_partial) -> erlang370_pure_support:invoke(tuple_size,[<<1:3>>]);
probe(tuple_size_pid) -> erlang370_pure_support:invoke(tuple_size,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(tuple_size_reference) -> erlang370_pure_support:invoke(tuple_size,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(tuple_size_function) -> erlang370_pure_support:invoke(tuple_size,[fun erlang:self/0]);
probe(bit_size_nil) -> erlang370_pure_support:invoke(bit_size,[[]]);
probe(bit_size_proper) -> erlang370_pure_support:invoke(bit_size,[[a,b,c]]);
probe(bit_size_improper) -> erlang370_pure_support:invoke(bit_size,[[a|tail]]);
probe(bit_size_atom) -> erlang370_pure_support:invoke(bit_size,[value]);
probe(bit_size_integer) -> erlang370_pure_support:invoke(bit_size,[42]);
probe(bit_size_float) -> erlang370_pure_support:invoke(bit_size,[1.5]);
probe(bit_size_tuple) -> erlang370_pure_support:invoke(bit_size,[{a,b}]);
probe(bit_size_empty_tuple) -> erlang370_pure_support:invoke(bit_size,[{}]);
probe(bit_size_map) -> erlang370_pure_support:invoke(bit_size,[#{a=>1}]);
probe(bit_size_binary) -> erlang370_pure_support:invoke(bit_size,[<<1,2,3>>]);
probe(bit_size_partial) -> erlang370_pure_support:invoke(bit_size,[<<1:3>>]);
probe(bit_size_pid) -> erlang370_pure_support:invoke(bit_size,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(bit_size_reference) -> erlang370_pure_support:invoke(bit_size,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(bit_size_function) -> erlang370_pure_support:invoke(bit_size,[fun erlang:self/0]);
probe(byte_size_nil) -> erlang370_pure_support:invoke(byte_size,[[]]);
probe(byte_size_proper) -> erlang370_pure_support:invoke(byte_size,[[a,b,c]]);
probe(byte_size_improper) -> erlang370_pure_support:invoke(byte_size,[[a|tail]]);
probe(byte_size_atom) -> erlang370_pure_support:invoke(byte_size,[value]);
probe(byte_size_integer) -> erlang370_pure_support:invoke(byte_size,[42]);
probe(byte_size_float) -> erlang370_pure_support:invoke(byte_size,[1.5]);
probe(byte_size_tuple) -> erlang370_pure_support:invoke(byte_size,[{a,b}]);
probe(byte_size_empty_tuple) -> erlang370_pure_support:invoke(byte_size,[{}]);
probe(byte_size_map) -> erlang370_pure_support:invoke(byte_size,[#{a=>1}]);
probe(byte_size_binary) -> erlang370_pure_support:invoke(byte_size,[<<1,2,3>>]);
probe(byte_size_partial) -> erlang370_pure_support:invoke(byte_size,[<<1:3>>]);
probe(byte_size_pid) -> erlang370_pure_support:invoke(byte_size,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(byte_size_reference) -> erlang370_pure_support:invoke(byte_size,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(byte_size_function) -> erlang370_pure_support:invoke(byte_size,[fun erlang:self/0]);
probe(map_size_nil) -> erlang370_pure_support:invoke(map_size,[[]]);
probe(map_size_proper) -> erlang370_pure_support:invoke(map_size,[[a,b,c]]);
probe(map_size_improper) -> erlang370_pure_support:invoke(map_size,[[a|tail]]);
probe(map_size_atom) -> erlang370_pure_support:invoke(map_size,[value]);
probe(map_size_integer) -> erlang370_pure_support:invoke(map_size,[42]);
probe(map_size_float) -> erlang370_pure_support:invoke(map_size,[1.5]);
probe(map_size_tuple) -> erlang370_pure_support:invoke(map_size,[{a,b}]);
probe(map_size_empty_tuple) -> erlang370_pure_support:invoke(map_size,[{}]);
probe(map_size_map) -> erlang370_pure_support:invoke(map_size,[#{a=>1}]);
probe(map_size_binary) -> erlang370_pure_support:invoke(map_size,[<<1,2,3>>]);
probe(map_size_partial) -> erlang370_pure_support:invoke(map_size,[<<1:3>>]);
probe(map_size_pid) -> erlang370_pure_support:invoke(map_size,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(map_size_reference) -> erlang370_pure_support:invoke(map_size,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(map_size_function) -> erlang370_pure_support:invoke(map_size,[fun erlang:self/0]);
probe(size_nil) -> erlang370_pure_support:invoke(size,[[]]);
probe(size_proper) -> erlang370_pure_support:invoke(size,[[a,b,c]]);
probe(size_improper) -> erlang370_pure_support:invoke(size,[[a|tail]]);
probe(size_atom) -> erlang370_pure_support:invoke(size,[value]);
probe(size_integer) -> erlang370_pure_support:invoke(size,[42]);
probe(size_float) -> erlang370_pure_support:invoke(size,[1.5]);
probe(size_tuple) -> erlang370_pure_support:invoke(size,[{a,b}]);
probe(size_empty_tuple) -> erlang370_pure_support:invoke(size,[{}]);
probe(size_map) -> erlang370_pure_support:invoke(size,[#{a=>1}]);
probe(size_binary) -> erlang370_pure_support:invoke(size,[<<1,2,3>>]);
probe(size_partial) -> erlang370_pure_support:invoke(size,[<<1:3>>]);
probe(size_pid) -> erlang370_pure_support:invoke(size,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(size_reference) -> erlang370_pure_support:invoke(size,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(size_function) -> erlang370_pure_support:invoke(size,[fun erlang:self/0]);
probe(bit_size_bits_0) -> erlang370_pure_support:invoke(bit_size,[<<0:0>>]);
probe(byte_size_bits_0) -> erlang370_pure_support:invoke(byte_size,[<<0:0>>]);
probe(size_bits_0) -> erlang370_pure_support:invoke(size,[<<0:0>>]);
probe(bit_size_bits_1) -> erlang370_pure_support:invoke(bit_size,[<<0:1>>]);
probe(byte_size_bits_1) -> erlang370_pure_support:invoke(byte_size,[<<0:1>>]);
probe(size_bits_1) -> erlang370_pure_support:invoke(size,[<<0:1>>]);
probe(bit_size_bits_7) -> erlang370_pure_support:invoke(bit_size,[<<0:7>>]);
probe(byte_size_bits_7) -> erlang370_pure_support:invoke(byte_size,[<<0:7>>]);
probe(size_bits_7) -> erlang370_pure_support:invoke(size,[<<0:7>>]);
probe(bit_size_bits_8) -> erlang370_pure_support:invoke(bit_size,[<<0:8>>]);
probe(byte_size_bits_8) -> erlang370_pure_support:invoke(byte_size,[<<0:8>>]);
probe(size_bits_8) -> erlang370_pure_support:invoke(size,[<<0:8>>]);
probe(bit_size_bits_9) -> erlang370_pure_support:invoke(bit_size,[<<0:9>>]);
probe(byte_size_bits_9) -> erlang370_pure_support:invoke(byte_size,[<<0:9>>]);
probe(size_bits_9) -> erlang370_pure_support:invoke(size,[<<0:9>>]);
probe(bit_size_bits_511) -> erlang370_pure_support:invoke(bit_size,[<<0:511>>]);
probe(byte_size_bits_511) -> erlang370_pure_support:invoke(byte_size,[<<0:511>>]);
probe(size_bits_511) -> erlang370_pure_support:invoke(size,[<<0:511>>]);
probe(bit_size_bits_512) -> erlang370_pure_support:invoke(bit_size,[<<0:512>>]);
probe(byte_size_bits_512) -> erlang370_pure_support:invoke(byte_size,[<<0:512>>]);
probe(size_bits_512) -> erlang370_pure_support:invoke(size,[<<0:512>>]);
probe(bit_size_bits_513) -> erlang370_pure_support:invoke(bit_size,[<<0:513>>]);
probe(byte_size_bits_513) -> erlang370_pure_support:invoke(byte_size,[<<0:513>>]);
probe(size_bits_513) -> erlang370_pure_support:invoke(size,[<<0:513>>]);
probe(bit_size_bits_1025) -> erlang370_pure_support:invoke(bit_size,[<<0:1025>>]);
probe(byte_size_bits_1025) -> erlang370_pure_support:invoke(byte_size,[<<0:1025>>]);
probe(size_bits_1025) -> erlang370_pure_support:invoke(size,[<<0:1025>>]);
probe(bit_size_unaligned_view) -> begin <<_:3,V:129/binary,_:5>> = <<0:1040>>, erlang:bit_size(V) end;
probe(byte_size_unaligned_view) -> begin <<_:3,V:129/binary,_:5>> = <<0:1040>>, erlang:byte_size(V) end;
probe(size_unaligned_view) -> begin <<_:3,V:129/binary,_:5>> = <<0:1040>>, erlang:size(V) end;
probe(map_size_0) -> erlang370_pure_support:invoke(map_size,[make_map(0,#{})]);
probe(map_size_1) -> erlang370_pure_support:invoke(map_size,[make_map(1,#{})]);
probe(map_size_31) -> erlang370_pure_support:invoke(map_size,[make_map(31,#{})]);
probe(map_size_32) -> erlang370_pure_support:invoke(map_size,[make_map(32,#{})]);
probe(map_size_33) -> erlang370_pure_support:invoke(map_size,[make_map(33,#{})]);
probe(map_size_257) -> erlang370_pure_support:invoke(map_size,[make_map(257,#{})]);
probe(length_proper_257) -> erlang370_pure_support:invoke(length,[make_list(257,[])]);
probe(length_improper_257) -> erlang370_pure_support:invoke(length,[make_list(257,tail)]);
probe(length_proper_64017) -> erlang370_pure_support:invoke(length,[make_list(64017,[])]);
probe(length_improper_64017) -> erlang370_pure_support:invoke(length,[make_list(64017,tail)]);
probe(is_function_external_zero) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,0]);
probe(is_function_nonfun_zero) -> erlang370_pure_support:invoke(is_function,[a,0]);
probe(is_function_external_one) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,1]);
probe(is_function_nonfun_one) -> erlang370_pure_support:invoke(is_function,[a,1]);
probe(is_function_external_255) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,255]);
probe(is_function_nonfun_255) -> erlang370_pure_support:invoke(is_function,[a,255]);
probe(is_function_external_256) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,256]);
probe(is_function_nonfun_256) -> erlang370_pure_support:invoke(is_function,[a,256]);
probe(is_function_external_maxsmall) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,576460752303423487]);
probe(is_function_nonfun_maxsmall) -> erlang370_pure_support:invoke(is_function,[a,576460752303423487]);
probe(is_function_external_big) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,(1 bsl 100)]);
probe(is_function_nonfun_big) -> erlang370_pure_support:invoke(is_function,[a,(1 bsl 100)]);
probe(is_function_external_negative) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,-1]);
probe(is_function_nonfun_negative) -> erlang370_pure_support:invoke(is_function,[a,-1]);
probe(is_function_external_negative_big) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,-(1 bsl 100)]);
probe(is_function_nonfun_negative_big) -> erlang370_pure_support:invoke(is_function,[a,-(1 bsl 100)]);
probe(is_function_external_float) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,1.0]);
probe(is_function_nonfun_float) -> erlang370_pure_support:invoke(is_function,[a,1.0]);
probe(is_function_external_atom) -> erlang370_pure_support:invoke(is_function,[fun erlang:self/0,a]);
probe(is_function_nonfun_atom) -> erlang370_pure_support:invoke(is_function,[a,a]);
probe(is_integer_range_inside) -> erlang370_pure_support:invoke(is_integer,[0,-1,1]);
probe(is_integer_range_minimum) -> erlang370_pure_support:invoke(is_integer,[-1,-1,1]);
probe(is_integer_range_maximum) -> erlang370_pure_support:invoke(is_integer,[1,-1,1]);
probe(is_integer_range_outside) -> erlang370_pure_support:invoke(is_integer,[2,-1,1]);
probe(is_integer_range_reversed) -> erlang370_pure_support:invoke(is_integer,[0,1,-1]);
probe(is_integer_range_big) -> erlang370_pure_support:invoke(is_integer,[(1 bsl 200),0,(1 bsl 201)]);
probe(is_integer_range_negative_big) -> erlang370_pure_support:invoke(is_integer,[-(1 bsl 200),-(1 bsl 201),0]);
probe(is_integer_range_noninteger) -> erlang370_pure_support:invoke(is_integer,[a,-1,1]);
probe(is_integer_range_invalid_min) -> erlang370_pure_support:invoke(is_integer,[0,a,1]);
probe(is_integer_range_invalid_max) -> erlang370_pure_support:invoke(is_integer,[0,-1,a]);
probe(is_integer_range_both_invalid) -> erlang370_pure_support:invoke(is_integer,[a,a,b]);
probe(is_integer_range_float_min) -> erlang370_pure_support:invoke(is_integer,[0,0.0,1]);
probe(is_map_key_present) -> erlang370_pure_support:invoke(is_map_key,[a,#{a=>1}]);
probe(is_map_key_absent) -> erlang370_pure_support:invoke(is_map_key,[b,#{a=>1}]);
probe(is_map_key_exact_int) -> erlang370_pure_support:invoke(is_map_key,[1,#{1.0=>float}]);
probe(is_map_key_exact_float) -> erlang370_pure_support:invoke(is_map_key,[1.0,#{1=>integer}]);
probe(is_map_key_tuple) -> erlang370_pure_support:invoke(is_map_key,[{a},#{{a}=>1}]);
probe(is_map_key_improper) -> erlang370_pure_support:invoke(is_map_key,[[a|tail],#{[a|tail]=>1}]);
probe(is_map_key_invalid_atom) -> erlang370_pure_support:invoke(is_map_key,[a,b]);
probe(is_map_key_invalid_tuple) -> erlang370_pure_support:invoke(is_map_key,[a,{}]);
probe(is_map_key_large_present) -> erlang370_pure_support:invoke(is_map_key,[33,make_map(40,#{})]);
probe(is_map_key_large_absent) -> erlang370_pure_support:invoke(is_map_key,[41,make_map(40,#{})]);
probe(is_record_one_nil) -> erlang370_pure_support:invoke(is_record,[[]]);
probe(is_record_one_proper) -> erlang370_pure_support:invoke(is_record,[[a,b,c]]);
probe(is_record_one_improper) -> erlang370_pure_support:invoke(is_record,[[a|tail]]);
probe(is_record_one_atom) -> erlang370_pure_support:invoke(is_record,[value]);
probe(is_record_one_integer) -> erlang370_pure_support:invoke(is_record,[42]);
probe(is_record_one_float) -> erlang370_pure_support:invoke(is_record,[1.5]);
probe(is_record_one_tuple) -> erlang370_pure_support:invoke(is_record,[{a,b}]);
probe(is_record_one_empty_tuple) -> erlang370_pure_support:invoke(is_record,[{}]);
probe(is_record_one_map) -> erlang370_pure_support:invoke(is_record,[#{a=>1}]);
probe(is_record_one_binary) -> erlang370_pure_support:invoke(is_record,[<<1,2,3>>]);
probe(is_record_one_partial) -> erlang370_pure_support:invoke(is_record,[<<1:3>>]);
probe(is_record_one_pid) -> erlang370_pure_support:invoke(is_record,[binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)]);
probe(is_record_one_reference) -> erlang370_pure_support:invoke(is_record,[binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)]);
probe(is_record_one_function) -> erlang370_pure_support:invoke(is_record,[fun erlang:self/0]);
probe(is_record_two_tagged) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec]);
probe(is_record_two_tag_only) -> erlang370_pure_support:invoke(is_record,[{rec},rec]);
probe(is_record_two_empty) -> erlang370_pure_support:invoke(is_record,[{},rec]);
probe(is_record_two_wrong_tag) -> erlang370_pure_support:invoke(is_record,[{rec,1},other]);
probe(is_record_two_not_tuple) -> erlang370_pure_support:invoke(is_record,[a,rec]);
probe(is_record_two_invalid_tag) -> erlang370_pure_support:invoke(is_record,[{rec,1},1]);
probe(is_record_three_two) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,2]);
probe(is_record_three_one) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,1]);
probe(is_record_three_zero) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,0]);
probe(is_record_three_negative) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,-1]);
probe(is_record_three_minimum) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,-576460752303423488]);
probe(is_record_three_maximum) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,576460752303423487]);
probe(is_record_three_positive_big) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,576460752303423488]);
probe(is_record_three_negative_big) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,-576460752303423489]);
probe(is_record_three_float) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,2.0]);
probe(is_record_three_module_name) -> erlang370_pure_support:invoke(is_record,[{rec,1},rec,rec]);
probe(is_record_three_bad_tag) -> erlang370_pure_support:invoke(is_record,[{rec,1},1,2]);
probe(native_record_197) -> begin R=records:create(type_owner,rec,[{value,1}],#{is_exported=>true}), erlang370_pure_support:invoke(is_record,[R]) end;
probe(native_record_198) -> begin R=records:create(type_owner,rec,[{value,1}],#{is_exported=>true}), erlang370_pure_support:invoke(is_record,[R,rec]) end;
probe(native_record_199) -> begin R=records:create(type_owner,rec,[{value,1}],#{is_exported=>true}), erlang370_pure_support:invoke(is_record,[R,type_owner,rec]) end;
probe(native_record_200) -> begin R=records:create(type_owner,rec,[{value,1}],#{is_exported=>true}), erlang370_pure_support:invoke(is_record,[R,type_owner,other]) end;
probe(guard_failure_length) -> guard_length([a|tail]);
probe(guard_failure_tuple_size) -> guard_tuple_size(a);
probe(guard_failure_bit_size) -> guard_bit_size(a);
probe(guard_failure_byte_size) -> guard_byte_size(a);
probe(guard_failure_map_size) -> guard_map_size(a);
probe(guard_failure_size) -> guard_size(a);
probe(non_tail_error_length) -> context_length(invalid);
probe(non_tail_error_tuple_size) -> context_tuple_size(invalid);
probe(non_tail_error_bit_size) -> context_bit_size(invalid);
probe(non_tail_error_byte_size) -> context_byte_size(invalid);
probe(non_tail_error_map_size) -> context_map_size(invalid);
probe(non_tail_error_size) -> context_size(invalid);
probe(compiled_record_correct) -> compiled_record({rec,1});
probe(compiled_record_wrong_size) -> compiled_record({rec,1,2});
probe(direct_record_positive_big) -> direct_record_positive_big({rec,1});
probe(direct_record_negative_big) -> direct_record_negative_big({rec,1}).
make_list(0,Tail) -> Tail;
make_list(N,Tail) -> make_list(N-1,[N|Tail]).
make_map(0,Map) -> Map;
make_map(N,Map) -> make_map(N-1,Map#{N=>N}).
guard_length(V) when erlang:length(V) >= 0 -> true;
guard_length(_) -> false.
context_length(V) -> Result=erlang:length(V), {returned,Result}.
guard_tuple_size(V) when erlang:tuple_size(V) >= 0 -> true;
guard_tuple_size(_) -> false.
context_tuple_size(V) -> Result=erlang:tuple_size(V), {returned,Result}.
guard_bit_size(V) when erlang:bit_size(V) >= 0 -> true;
guard_bit_size(_) -> false.
context_bit_size(V) -> Result=erlang:bit_size(V), {returned,Result}.
guard_byte_size(V) when erlang:byte_size(V) >= 0 -> true;
guard_byte_size(_) -> false.
context_byte_size(V) -> Result=erlang:byte_size(V), {returned,Result}.
guard_map_size(V) when erlang:map_size(V) >= 0 -> true;
guard_map_size(_) -> false.
context_map_size(V) -> Result=erlang:map_size(V), {returned,Result}.
guard_size(V) when erlang:size(V) >= 0 -> true;
guard_size(_) -> false.
context_size(V) -> Result=erlang:size(V), {returned,Result}.
compiled_record(V) -> erlang:is_record(V,rec).
direct_record_positive_big(V) -> erlang:is_record(V,rec,576460752303423488).
direct_record_negative_big(V) -> erlang:is_record(V,rec,-576460752303423489).
