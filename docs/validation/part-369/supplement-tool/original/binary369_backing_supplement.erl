-module(binary369_backing_supplement).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [append_63,append_64,append_65,append_257,private_loop_65,escaped_loop_65,private_loop_257,escaped_loop_257,private_loop_4097,escaped_loop_4097,comprehension_65,comprehension_257,append_shared_growth,append_repeated_segment,append_zero,pin_after_growth,join_single_large_view,join_pair_large_view,replace_no_match_large_view,replace_empty_scope_large_view,replace_identity_large_view,retained_partial_large,hex_capacity_7,hex_capacity_8,hex_capacity_9,hex_capacity_64,hex_capacity_65,hex_capacity_257].
run() -> binary_all369_support:run(binary369_backing_supplement,cases()).
run_stacks() -> binary_all369_support:profiles(binary369_backing_supplement,cases()).
probe(append_63) -> B = binary:copy(<<7>>,63), C = append_once(B), {observe(B),observe(C)};
probe(append_64) -> B = binary:copy(<<7>>,64), C = append_once(B), {observe(B),observe(C)};
probe(append_65) -> B = binary:copy(<<7>>,65), C = append_once(B), {observe(B),observe(C)};
probe(append_257) -> B = binary:copy(<<7>>,257), C = append_once(B), {observe(B),observe(C)};
probe(private_loop_65) -> C = append_loop(65,<<>>), observe(C);
probe(escaped_loop_65) -> {C,Previous} = escaped_loop(65,<<>>,<<>>), {observe(Previous),observe(C)};
probe(private_loop_257) -> C = append_loop(257,<<>>), observe(C);
probe(escaped_loop_257) -> {C,Previous} = escaped_loop(257,<<>>,<<>>), {observe(Previous),observe(C)};
probe(private_loop_4097) -> C = append_loop(4097,<<>>), observe(C);
probe(escaped_loop_4097) -> {C,Previous} = escaped_loop(4097,<<>>,<<>>), {observe(Previous),observe(C)};
probe(comprehension_65) -> B = binary:copy(<<7>>,65), C = << <<X>> || <<X>> <= B >>, {observe(B),observe(C)};
probe(comprehension_257) -> B = binary:copy(<<7>>,257), C = << <<X>> || <<X>> <= B >>, {observe(B),observe(C)};
probe(append_shared_growth) -> B = binary:copy(<<7>>,65), C = <<B/binary,1>>, D = <<C/binary,0:2048>>, {observe(B),observe(C),observe(D)};
probe(append_repeated_segment) -> B = append_loop(65,<<>>), C = <<B/binary,B/binary>>, {observe(B),observe(C)};
probe(append_zero) -> B = append_loop(65,<<>>), C = <<B/binary>>, {observe(B),observe(C)};
probe(pin_after_growth) -> B = append_loop(257,<<>>), Before = observe(B), <<_:8,Rest/binary>> = B, {Before,observe(B),observe(Rest)};
probe(join_single_large_view) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{17,257}), R = binary:join([P],<<",">>), {observe(P),observe(R),R =:= P};
probe(join_pair_large_view) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{17,257}), R = binary:join([P,<<>>],<<>>), {observe(P),observe(R),R =:= P};
probe(replace_no_match_large_view) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{17,257}), R = binary:replace(P,<<8>>,<<9>>), {observe(P),observe(R),R =:= P};
probe(replace_empty_scope_large_view) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{17,257}), R = binary:replace(P,<<7>>,<<9>>,[{scope,{0,0}}]), {observe(P),observe(R),R =:= P};
probe(replace_identity_large_view) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{17,257}), R = binary:replace(P,<<7>>,fun binary369_callback_supplement:identity/1), {observe(P),observe(R),R =:= P};
probe(retained_partial_large) -> B = binary:copy(<<7>>,65537), <<_:1,R:1025/bitstring,_/bitstring>> = B, {bit_size(R),binary:referenced_byte_size(R)};
probe(hex_capacity_7) -> B = binary:copy(<<7>>,7), E = binary:encode_hex(B), D = binary:decode_hex(E), {observe(B),observe(E),observe(D),D =:= B};
probe(hex_capacity_8) -> B = binary:copy(<<7>>,8), E = binary:encode_hex(B), D = binary:decode_hex(E), {observe(B),observe(E),observe(D),D =:= B};
probe(hex_capacity_9) -> B = binary:copy(<<7>>,9), E = binary:encode_hex(B), D = binary:decode_hex(E), {observe(B),observe(E),observe(D),D =:= B};
probe(hex_capacity_64) -> B = binary:copy(<<7>>,64), E = binary:encode_hex(B), D = binary:decode_hex(E), {observe(B),observe(E),observe(D),D =:= B};
probe(hex_capacity_65) -> B = binary:copy(<<7>>,65), E = binary:encode_hex(B), D = binary:decode_hex(E), {observe(B),observe(E),observe(D),D =:= B};
probe(hex_capacity_257) -> B = binary:copy(<<7>>,257), E = binary:encode_hex(B), D = binary:decode_hex(E), {observe(B),observe(E),observe(D),D =:= B}.
observe(Binary) -> {byte_size(Binary),binary:referenced_byte_size(Binary)}.
append_once(Binary) -> <<Binary/binary,9>>.
append_loop(0,Binary) -> Binary;
append_loop(Count,Binary) -> append_loop(Count-1,<<Binary/binary,7>>).
escaped_loop(0,Binary,Previous) -> {Binary,Previous};
escaped_loop(Count,Binary,_Previous) -> Next = <<Binary/binary,7>>,escaped_loop(Count-1,Next,Binary).
