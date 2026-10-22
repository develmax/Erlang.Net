-module(binary_all369_common).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [longest_common_prefix_empty,longest_common_prefix_oneempty,longest_common_prefix_one,longest_common_prefix_shared,longest_common_prefix_shorter,longest_common_prefix_empty_member,longest_common_prefix_different,longest_common_prefix_bad_member,longest_common_prefix_improper,longest_common_prefix_atom,longest_common_prefix_bits,longest_common_prefix_offset3,longest_common_prefix_long,longest_common_suffix_empty,longest_common_suffix_oneempty,longest_common_suffix_one,longest_common_suffix_shared,longest_common_suffix_shorter,longest_common_suffix_empty_member,longest_common_suffix_different,longest_common_suffix_bad_member,longest_common_suffix_improper,longest_common_suffix_atom,longest_common_suffix_bits,longest_common_suffix_offset3,longest_common_suffix_long].
run() -> binary_all369_support:run(binary_all369_common,cases()).
run_stacks() -> binary_all369_support:profiles(binary_all369_common,cases()).
probe(longest_common_prefix_empty) -> binary:longest_common_prefix([]);
probe(longest_common_prefix_oneempty) -> binary:longest_common_prefix([<<>>]);
probe(longest_common_prefix_one) -> binary:longest_common_prefix([<<1,2>>]);
probe(longest_common_prefix_shared) -> binary:longest_common_prefix([<<1,2,3>>,<<1,2,4>>]);
probe(longest_common_prefix_shorter) -> binary:longest_common_prefix([<<1,2>>,<<1,2,3>>]);
probe(longest_common_prefix_empty_member) -> binary:longest_common_prefix([<<>>,<<1>>]);
probe(longest_common_prefix_different) -> binary:longest_common_prefix([<<1>>,<<2>>]);
probe(longest_common_prefix_bad_member) -> binary:longest_common_prefix([<<1>>,bad]);
probe(longest_common_prefix_improper) -> binary:longest_common_prefix([<<1>>|bad]);
probe(longest_common_prefix_atom) -> binary:longest_common_prefix(bad);
probe(longest_common_prefix_bits) -> binary:longest_common_prefix([<<0:9>>]);
probe(longest_common_prefix_offset3) -> binary:longest_common_prefix([binary_all369_support:shifted(<<1,2,3>>),<<1,2,4>>]);
probe(longest_common_prefix_long) -> B = binary:copy(<<7>>,65537), binary:longest_common_prefix([B,<<B/binary,8>>]);
probe(longest_common_suffix_empty) -> binary:longest_common_suffix([]);
probe(longest_common_suffix_oneempty) -> binary:longest_common_suffix([<<>>]);
probe(longest_common_suffix_one) -> binary:longest_common_suffix([<<1,2>>]);
probe(longest_common_suffix_shared) -> binary:longest_common_suffix([<<1,2,3>>,<<1,2,4>>]);
probe(longest_common_suffix_shorter) -> binary:longest_common_suffix([<<1,2>>,<<1,2,3>>]);
probe(longest_common_suffix_empty_member) -> binary:longest_common_suffix([<<>>,<<1>>]);
probe(longest_common_suffix_different) -> binary:longest_common_suffix([<<1>>,<<2>>]);
probe(longest_common_suffix_bad_member) -> binary:longest_common_suffix([<<1>>,bad]);
probe(longest_common_suffix_improper) -> binary:longest_common_suffix([<<1>>|bad]);
probe(longest_common_suffix_atom) -> binary:longest_common_suffix(bad);
probe(longest_common_suffix_bits) -> binary:longest_common_suffix([<<0:9>>]);
probe(longest_common_suffix_offset3) -> binary:longest_common_suffix([binary_all369_support:shifted(<<1,2,3>>),<<1,2,4>>]);
probe(longest_common_suffix_long) -> B = binary:copy(<<7>>,65537), binary:longest_common_suffix([B,<<B/binary,8>>]).
