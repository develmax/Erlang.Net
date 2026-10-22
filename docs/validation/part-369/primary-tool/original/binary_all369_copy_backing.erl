-module(binary_all369_copy_backing).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [copy1_empty,reference_empty,copy2_empty_0,copy2_empty_1,copy2_empty_2,copy2_empty_n1,copy2_empty_36893488147419103232,copy1_bytes,reference_bytes,copy2_bytes_0,copy2_bytes_1,copy2_bytes_2,copy2_bytes_n1,copy2_bytes_36893488147419103232,copy1_offset3,reference_offset3,copy2_offset3_0,copy2_offset3_1,copy2_offset3_2,copy2_offset3_n1,copy2_offset3_36893488147419103232,copy1_bits9,reference_bits9,copy2_bits9_0,copy2_bits9_1,copy2_bits9_2,copy2_bits9_n1,copy2_bits9_36893488147419103232,copy1_atom,reference_atom,copy2_atom_0,copy2_atom_1,copy2_atom_2,copy2_atom_n1,copy2_atom_36893488147419103232,copy_count_atom,copy_count_float,copy_count_list,backing_constructor_0,backing_constructor_1,backing_constructor_63,backing_constructor_64,backing_constructor_65,backing_constructor_255,backing_constructor_256,backing_constructor_65537,backing_part_0,backing_part_1,backing_part_63,backing_part_64,backing_part_65,backing_part_257,backing_part_65536,backing_retained_nested,backing_binary_constructor].
run() -> binary_all369_support:run(binary_all369_copy_backing,cases()).
run_stacks() -> binary_all369_support:profiles(binary_all369_copy_backing,cases()).
probe(copy1_empty) -> binary:copy(<<>>);
probe(reference_empty) -> binary:referenced_byte_size(<<>>);
probe(copy2_empty_0) -> binary:copy(<<>>,0);
probe(copy2_empty_1) -> binary:copy(<<>>,1);
probe(copy2_empty_2) -> binary:copy(<<>>,2);
probe(copy2_empty_n1) -> binary:copy(<<>>,-1);
probe(copy2_empty_36893488147419103232) -> binary:copy(<<>>,36893488147419103232);
probe(copy1_bytes) -> binary:copy(<<0,255>>);
probe(reference_bytes) -> binary:referenced_byte_size(<<0,255>>);
probe(copy2_bytes_0) -> binary:copy(<<0,255>>,0);
probe(copy2_bytes_1) -> binary:copy(<<0,255>>,1);
probe(copy2_bytes_2) -> binary:copy(<<0,255>>,2);
probe(copy2_bytes_n1) -> binary:copy(<<0,255>>,-1);
probe(copy2_bytes_36893488147419103232) -> binary:copy(<<0,255>>,36893488147419103232);
probe(copy1_offset3) -> binary:copy(binary_all369_support:shifted(<<1,2,3>>));
probe(reference_offset3) -> binary:referenced_byte_size(binary_all369_support:shifted(<<1,2,3>>));
probe(copy2_offset3_0) -> binary:copy(binary_all369_support:shifted(<<1,2,3>>),0);
probe(copy2_offset3_1) -> binary:copy(binary_all369_support:shifted(<<1,2,3>>),1);
probe(copy2_offset3_2) -> binary:copy(binary_all369_support:shifted(<<1,2,3>>),2);
probe(copy2_offset3_n1) -> binary:copy(binary_all369_support:shifted(<<1,2,3>>),-1);
probe(copy2_offset3_36893488147419103232) -> binary:copy(binary_all369_support:shifted(<<1,2,3>>),36893488147419103232);
probe(copy1_bits9) -> binary:copy(<<0:9>>);
probe(reference_bits9) -> binary:referenced_byte_size(<<0:9>>);
probe(copy2_bits9_0) -> binary:copy(<<0:9>>,0);
probe(copy2_bits9_1) -> binary:copy(<<0:9>>,1);
probe(copy2_bits9_2) -> binary:copy(<<0:9>>,2);
probe(copy2_bits9_n1) -> binary:copy(<<0:9>>,-1);
probe(copy2_bits9_36893488147419103232) -> binary:copy(<<0:9>>,36893488147419103232);
probe(copy1_atom) -> binary:copy(bad);
probe(reference_atom) -> binary:referenced_byte_size(bad);
probe(copy2_atom_0) -> binary:copy(bad,0);
probe(copy2_atom_1) -> binary:copy(bad,1);
probe(copy2_atom_2) -> binary:copy(bad,2);
probe(copy2_atom_n1) -> binary:copy(bad,-1);
probe(copy2_atom_36893488147419103232) -> binary:copy(bad,36893488147419103232);
probe(copy_count_atom) -> binary:copy(<<1>>,bad);
probe(copy_count_float) -> binary:copy(<<1>>,1.0);
probe(copy_count_list) -> binary:copy(<<1>>,[]);
probe(backing_constructor_0) -> B = binary:copy(<<7>>,0), binary_all369_support:backing(B);
probe(backing_constructor_1) -> B = binary:copy(<<7>>,1), binary_all369_support:backing(B);
probe(backing_constructor_63) -> B = binary:copy(<<7>>,63), binary_all369_support:backing(B);
probe(backing_constructor_64) -> B = binary:copy(<<7>>,64), binary_all369_support:backing(B);
probe(backing_constructor_65) -> B = binary:copy(<<7>>,65), binary_all369_support:backing(B);
probe(backing_constructor_255) -> B = binary:copy(<<7>>,255), binary_all369_support:backing(B);
probe(backing_constructor_256) -> B = binary:copy(<<7>>,256), binary_all369_support:backing(B);
probe(backing_constructor_65537) -> B = binary:copy(<<7>>,65537), binary_all369_support:backing(B);
probe(backing_part_0) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,0}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_part_1) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,1}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_part_63) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,63}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_part_64) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,64}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_part_65) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,65}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_part_257) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,257}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_part_65536) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,65536}), {binary_all369_support:backing(P),binary_all369_support:backing(binary:copy(P))};
probe(backing_retained_nested) -> B = binary:copy(<<7>>,65537), P = binary:part(B,{1,257}), Q = binary:part(P,{1,129}), {binary_all369_support:backing(B),binary_all369_support:backing(P),binary_all369_support:backing(Q)};
probe(backing_binary_constructor) -> B = binary:copy(<<7>>,257), P = binary:part(B,{1,129}), Q = <<0,P/binary,1>>, {binary_all369_support:backing(P),binary_all369_support:backing(Q)}.
