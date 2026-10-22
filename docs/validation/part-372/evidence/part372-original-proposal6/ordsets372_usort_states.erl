-module(ordsets372_usort_states).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [public_empty,public_singleton,public_two_less,public_two_greater,public_two_equal,public_two_numeric_equal,public_two_numeric_equal_reverse,public_three_equal,three_ascending_above,three_ascending_equal_upper,three_ascending_below,three_ascending_equal_lower,three_ascending_between,three_descending_above,three_descending_equal_upper,three_descending_below,three_descending_equal_lower,three_descending_between,usort_1_improper,usort_1_proper,usort_1_direct_lists_improper,usplit_1_improper,usplit_1_proper,usplit_1_direct_lists_improper,usplit_2_improper,usplit_2_proper,usplit_2_direct_lists_improper,usplit_1_1_improper,usplit_1_1_proper,usplit_1_1_direct_lists_improper,usplit_2_1_improper,usplit_2_1_proper,usplit_2_1_direct_lists_improper,numeric_representative_0,numeric_representative_1,numeric_representative_2,zigzag_forward,zigzag_reverse,alternating_runs,equal_runs,all_equal_numeric,map_representatives_0,filtermap_representatives_0,map_representatives_1,filtermap_representatives_1].
run() -> ordsets_core_support:run(ordsets372_usort_states,cases()).
run_stacks() -> ordsets_core_support:profiles(ordsets372_usort_states,cases()).
probe(public_empty) -> ordsets:from_list([]);
probe(public_singleton) -> ordsets:from_list([a]);
probe(public_two_less) -> ordsets:from_list([1,2]);
probe(public_two_greater) -> ordsets:from_list([2,1]);
probe(public_two_equal) -> ordsets:from_list([1,1]);
probe(public_two_numeric_equal) -> ordsets:from_list([1.0,1]);
probe(public_two_numeric_equal_reverse) -> ordsets:from_list([1,1.0]);
probe(public_three_equal) -> ordsets:from_list([1,1.0,1]);
probe(three_ascending_above) -> ordsets:from_list([1,2,3]);
probe(three_ascending_equal_upper) -> ordsets:from_list([1,2,2.0]);
probe(three_ascending_below) -> ordsets:from_list([1,2,0]);
probe(three_ascending_equal_lower) -> ordsets:from_list([1,2,1.0]);
probe(three_ascending_between) -> ordsets:from_list([1,2,1.5]);
probe(three_descending_above) -> ordsets:from_list([2,1,3]);
probe(three_descending_equal_upper) -> ordsets:from_list([2,1,2.0]);
probe(three_descending_below) -> ordsets:from_list([2,1,0]);
probe(three_descending_equal_lower) -> ordsets:from_list([2,1,1.0]);
probe(three_descending_between) -> ordsets:from_list([2,1,1.5]);
probe(usort_1_improper) -> ordsets:from_list([1,1,1|bad]);
probe(usort_1_proper) -> ordsets:from_list([1,1,1]);
probe(usort_1_direct_lists_improper) -> lists:usort([1,1,1|bad]);
probe(usplit_1_improper) -> ordsets:from_list([1,3|bad]);
probe(usplit_1_proper) -> ordsets:from_list([1,3,4,5]);
probe(usplit_1_direct_lists_improper) -> lists:usort([1,3|bad]);
probe(usplit_2_improper) -> ordsets:from_list([3,1|bad]);
probe(usplit_2_proper) -> ordsets:from_list([3,1,0,-1]);
probe(usplit_2_direct_lists_improper) -> lists:usort([3,1|bad]);
probe(usplit_1_1_improper) -> ordsets:from_list([1,3,0,-1|bad]);
probe(usplit_1_1_proper) -> ordsets:from_list([1,3,0,-1]);
probe(usplit_1_1_direct_lists_improper) -> lists:usort([1,3,0,-1|bad]);
probe(usplit_2_1_improper) -> ordsets:from_list([3,1,4,5|bad]);
probe(usplit_2_1_proper) -> ordsets:from_list([3,1,4,5]);
probe(usplit_2_1_direct_lists_improper) -> lists:usort([3,1,4,5|bad]);
probe(numeric_representative_0) -> ordsets:from_list([2.0,1,2,0,1.0]);
probe(numeric_representative_1) -> ordsets:from_list([3,1.0,2,1,0,2.0]);
probe(numeric_representative_2) -> ordsets:from_list([1.0,1,1.0,2]);
probe(zigzag_forward) -> ordsets:from_list([5,1,6,2,7,3,8,4,9,0]);
probe(zigzag_reverse) -> ordsets:from_list([0,9,4,8,3,7,2,6,1,5]);
probe(alternating_runs) -> ordsets:from_list([1,2,3,9,8,7,0,4,5,6]);
probe(equal_runs) -> ordsets:from_list([3.0,3,3.0,1,1.0,2,2.0,2,0,0.0]);
probe(all_equal_numeric) -> ordsets:from_list([1.0,1,1.0,1,1.0,1]);
probe(map_representatives_0) -> ordsets:map(fun(E)->E end,[2.0,1,2,0,1.0]);
probe(filtermap_representatives_0) -> ordsets:filtermap(fun(E)->{true,E} end,[2.0,1,2,0,1.0]);
probe(map_representatives_1) -> ordsets:map(fun(E)->E end,[3,1.0,2,1,0,2.0]);
probe(filtermap_representatives_1) -> ordsets:filtermap(fun(E)->{true,E} end,[3,1.0,2,1,0,2.0]).
