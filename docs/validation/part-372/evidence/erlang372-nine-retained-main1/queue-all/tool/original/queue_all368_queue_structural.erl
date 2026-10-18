-module(queue_all368_queue_structural).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [member_bad_atom,
            member_bad_tuple0,
            member_bad_tuple1,
            member_bad_tuple3,
            member_bad_rear,
            member_bad_front,
            member_empty,
            member_rear_one,
            member_rear_two,
            member_rear_three,
            member_front_one,
            member_front_two,
            member_front_three,
            member_split,
            member_rear_improper_one,
            member_rear_improper_three,
            member_front_improper_one,
            member_front_improper_three,
            member_both_improper,
            member_rear_improper_front_one,
            member_rear_one_front_improper,
            reverse_bad_atom,
            reverse_bad_tuple0,
            reverse_bad_tuple1,
            reverse_bad_tuple3,
            reverse_bad_rear,
            reverse_bad_front,
            reverse_empty,
            reverse_rear_one,
            reverse_rear_two,
            reverse_rear_three,
            reverse_front_one,
            reverse_front_two,
            reverse_front_three,
            reverse_split,
            reverse_rear_improper_one,
            reverse_rear_improper_three,
            reverse_front_improper_one,
            reverse_front_improper_three,
            reverse_both_improper,
            reverse_rear_improper_front_one,
            reverse_rear_one_front_improper,
            delete_bad_atom,
            delete_bad_tuple0,
            delete_bad_tuple1,
            delete_bad_tuple3,
            delete_bad_rear,
            delete_bad_front,
            delete_empty,
            delete_rear_one,
            delete_rear_two,
            delete_rear_three,
            delete_front_one,
            delete_front_two,
            delete_front_three,
            delete_split,
            delete_rear_improper_one,
            delete_rear_improper_three,
            delete_front_improper_one,
            delete_front_improper_three,
            delete_both_improper,
            delete_rear_improper_front_one,
            delete_rear_one_front_improper,
            delete_r_bad_atom,
            delete_r_bad_tuple0,
            delete_r_bad_tuple1,
            delete_r_bad_tuple3,
            delete_r_bad_rear,
            delete_r_bad_front,
            delete_r_empty,
            delete_r_rear_one,
            delete_r_rear_two,
            delete_r_rear_three,
            delete_r_front_one,
            delete_r_front_two,
            delete_r_front_three,
            delete_r_split,
            delete_r_rear_improper_one,
            delete_r_rear_improper_three,
            delete_r_front_improper_one,
            delete_r_front_improper_three,
            delete_r_both_improper,
            delete_r_rear_improper_front_one,
            delete_r_rear_one_front_improper,
            member_float_exact,
            member_float_reverse_exact,
            member_tuple_exact,
            member_map_key,
            member_duplicates,
            member_front_hit_bad_rear,
            member_rear_hit_bad_front,
            member_false_tail_first,
            member_false_tail_later,
            member_opaque_tail_first,
            member_rear_improper_hit,
            delete_float_exact,
            delete_float_reverse_exact,
            delete_tuple_exact,
            delete_map_key,
            delete_duplicates,
            delete_front_hit_bad_rear,
            delete_rear_hit_bad_front,
            delete_false_tail_first,
            delete_false_tail_later,
            delete_opaque_tail_first,
            delete_rear_improper_hit,
            delete_r_float_exact,
            delete_r_float_reverse_exact,
            delete_r_tuple_exact,
            delete_r_map_key,
            delete_r_duplicates,
            delete_r_front_hit_bad_rear,
            delete_r_rear_hit_bad_front,
            delete_r_false_tail_first,
            delete_r_false_tail_later,
            delete_r_opaque_tail_first,
            delete_r_rear_improper_hit,
            join_empty_empty,
            join_left_empty,
            join_right_empty,
            join_two_full,
            join_both_bad,
            join_left_bad,
            join_right_bad,
            join_left_bad_field,
            join_right_bad_field,
            join_left_improper_empty,
            join_empty_right_improper,
            join_left_rear_improper,
            join_left_front_improper,
            join_right_front_improper_tail,
            join_both_improper_precedence,
            split_empty_0,
            split_empty_1,
            split_empty_2,
            split_empty_3,
            split_empty_4,
            split_empty_5,
            split_empty_minus1,
            split_empty_1p0,
            split_empty_bad,
            split_empty_1_bsl_128,
            split_balanced_0,
            split_balanced_1,
            split_balanced_2,
            split_balanced_3,
            split_balanced_4,
            split_balanced_5,
            split_balanced_minus1,
            split_balanced_1p0,
            split_balanced_bad,
            split_balanced_1_bsl_128,
            split_rear_only_0,
            split_rear_only_1,
            split_rear_only_2,
            split_rear_only_3,
            split_rear_only_4,
            split_rear_only_5,
            split_rear_only_minus1,
            split_rear_only_1p0,
            split_rear_only_bad,
            split_rear_only_1_bsl_128,
            split_front_only_0,
            split_front_only_1,
            split_front_only_2,
            split_front_only_3,
            split_front_only_4,
            split_front_only_5,
            split_front_only_minus1,
            split_front_only_1p0,
            split_front_only_bad,
            split_front_only_1_bsl_128,
            split_front_improper_0,
            split_front_improper_1,
            split_front_improper_2,
            split_front_improper_3,
            split_front_improper_4,
            split_front_improper_5,
            split_front_improper_minus1,
            split_front_improper_1p0,
            split_front_improper_bad,
            split_front_improper_1_bsl_128,
            split_rear_improper_0,
            split_rear_improper_1,
            split_rear_improper_2,
            split_rear_improper_3,
            split_rear_improper_4,
            split_rear_improper_5,
            split_rear_improper_minus1,
            split_rear_improper_1p0,
            split_rear_improper_bad,
            split_rear_improper_1_bsl_128,
            split_bad_0,
            split_bad_1,
            split_bad_2,
            split_bad_3,
            split_bad_4,
            split_bad_5,
            split_bad_minus1,
            split_bad_1p0,
            split_bad_bad,
            split_bad_1_bsl_128,
            member_caller_bad,
            reverse_caller_bad,
            join_caller_bad,
            split_caller_bad,
            delete_caller_bad,
            delete_r_caller_bad].
run() -> queue_all368_support:run(queue_all368_queue_structural,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_queue_structural,cases()).
probe(member_bad_atom) -> queue:member(1,bad);
probe(member_bad_tuple0) -> queue:member(1,{});
probe(member_bad_tuple1) -> queue:member(1,{[]});
probe(member_bad_tuple3) -> queue:member(1,{[],[],[]});
probe(member_bad_rear) -> queue:member(1,{bad,[]});
probe(member_bad_front) -> queue:member(1,{[],bad});
probe(member_empty) -> queue:member(1,{[],[]});
probe(member_rear_one) -> queue:member(1,{[1],[]});
probe(member_rear_two) -> queue:member(1,{[2,1],[]});
probe(member_rear_three) -> queue:member(1,{[3,2,1],[]});
probe(member_front_one) -> queue:member(1,{[],[1]});
probe(member_front_two) -> queue:member(1,{[],[1,2]});
probe(member_front_three) -> queue:member(1,{[],[1,2,3]});
probe(member_split) -> queue:member(1,{[4,3],[1,2]});
probe(member_rear_improper_one) -> queue:member(1,{[1|tail],[]});
probe(member_rear_improper_three) -> queue:member(1,{[1,2,3|tail],[]});
probe(member_front_improper_one) -> queue:member(1,{[],[1|tail]});
probe(member_front_improper_three) -> queue:member(1,{[],[1,2,3|tail]});
probe(member_both_improper) -> queue:member(1,{[4,3|rear_tail],[1,2|front_tail]});
probe(member_rear_improper_front_one) -> queue:member(1,{[4,3|tail],[1]});
probe(member_rear_one_front_improper) -> queue:member(1,{[4],[1,2|tail]});
probe(reverse_bad_atom) -> queue:reverse(bad);
probe(reverse_bad_tuple0) -> queue:reverse({});
probe(reverse_bad_tuple1) -> queue:reverse({[]});
probe(reverse_bad_tuple3) -> queue:reverse({[],[],[]});
probe(reverse_bad_rear) -> queue:reverse({bad,[]});
probe(reverse_bad_front) -> queue:reverse({[],bad});
probe(reverse_empty) -> queue:reverse({[],[]});
probe(reverse_rear_one) -> queue:reverse({[1],[]});
probe(reverse_rear_two) -> queue:reverse({[2,1],[]});
probe(reverse_rear_three) -> queue:reverse({[3,2,1],[]});
probe(reverse_front_one) -> queue:reverse({[],[1]});
probe(reverse_front_two) -> queue:reverse({[],[1,2]});
probe(reverse_front_three) -> queue:reverse({[],[1,2,3]});
probe(reverse_split) -> queue:reverse({[4,3],[1,2]});
probe(reverse_rear_improper_one) -> queue:reverse({[1|tail],[]});
probe(reverse_rear_improper_three) -> queue:reverse({[1,2,3|tail],[]});
probe(reverse_front_improper_one) -> queue:reverse({[],[1|tail]});
probe(reverse_front_improper_three) -> queue:reverse({[],[1,2,3|tail]});
probe(reverse_both_improper) -> queue:reverse({[4,3|rear_tail],[1,2|front_tail]});
probe(reverse_rear_improper_front_one) -> queue:reverse({[4,3|tail],[1]});
probe(reverse_rear_one_front_improper) -> queue:reverse({[4],[1,2|tail]});
probe(delete_bad_atom) -> queue:delete(1,bad);
probe(delete_bad_tuple0) -> queue:delete(1,{});
probe(delete_bad_tuple1) -> queue:delete(1,{[]});
probe(delete_bad_tuple3) -> queue:delete(1,{[],[],[]});
probe(delete_bad_rear) -> queue:delete(1,{bad,[]});
probe(delete_bad_front) -> queue:delete(1,{[],bad});
probe(delete_empty) -> queue:delete(1,{[],[]});
probe(delete_rear_one) -> queue:delete(1,{[1],[]});
probe(delete_rear_two) -> queue:delete(1,{[2,1],[]});
probe(delete_rear_three) -> queue:delete(1,{[3,2,1],[]});
probe(delete_front_one) -> queue:delete(1,{[],[1]});
probe(delete_front_two) -> queue:delete(1,{[],[1,2]});
probe(delete_front_three) -> queue:delete(1,{[],[1,2,3]});
probe(delete_split) -> queue:delete(1,{[4,3],[1,2]});
probe(delete_rear_improper_one) -> queue:delete(1,{[1|tail],[]});
probe(delete_rear_improper_three) -> queue:delete(1,{[1,2,3|tail],[]});
probe(delete_front_improper_one) -> queue:delete(1,{[],[1|tail]});
probe(delete_front_improper_three) -> queue:delete(1,{[],[1,2,3|tail]});
probe(delete_both_improper) -> queue:delete(1,{[4,3|rear_tail],[1,2|front_tail]});
probe(delete_rear_improper_front_one) -> queue:delete(1,{[4,3|tail],[1]});
probe(delete_rear_one_front_improper) -> queue:delete(1,{[4],[1,2|tail]});
probe(delete_r_bad_atom) -> queue:delete_r(1,bad);
probe(delete_r_bad_tuple0) -> queue:delete_r(1,{});
probe(delete_r_bad_tuple1) -> queue:delete_r(1,{[]});
probe(delete_r_bad_tuple3) -> queue:delete_r(1,{[],[],[]});
probe(delete_r_bad_rear) -> queue:delete_r(1,{bad,[]});
probe(delete_r_bad_front) -> queue:delete_r(1,{[],bad});
probe(delete_r_empty) -> queue:delete_r(1,{[],[]});
probe(delete_r_rear_one) -> queue:delete_r(1,{[1],[]});
probe(delete_r_rear_two) -> queue:delete_r(1,{[2,1],[]});
probe(delete_r_rear_three) -> queue:delete_r(1,{[3,2,1],[]});
probe(delete_r_front_one) -> queue:delete_r(1,{[],[1]});
probe(delete_r_front_two) -> queue:delete_r(1,{[],[1,2]});
probe(delete_r_front_three) -> queue:delete_r(1,{[],[1,2,3]});
probe(delete_r_split) -> queue:delete_r(1,{[4,3],[1,2]});
probe(delete_r_rear_improper_one) -> queue:delete_r(1,{[1|tail],[]});
probe(delete_r_rear_improper_three) -> queue:delete_r(1,{[1,2,3|tail],[]});
probe(delete_r_front_improper_one) -> queue:delete_r(1,{[],[1|tail]});
probe(delete_r_front_improper_three) -> queue:delete_r(1,{[],[1,2,3|tail]});
probe(delete_r_both_improper) -> queue:delete_r(1,{[4,3|rear_tail],[1,2|front_tail]});
probe(delete_r_rear_improper_front_one) -> queue:delete_r(1,{[4,3|tail],[1]});
probe(delete_r_rear_one_front_improper) -> queue:delete_r(1,{[4],[1,2|tail]});
probe(member_float_exact) -> queue:member(1,{[1.0],[2]});
probe(member_float_reverse_exact) -> queue:member(1.0,{[1],[2]});
probe(member_tuple_exact) -> queue:member({key,1},{[{key,1.0}],[{key,1}]});
probe(member_map_key) -> queue:member(#{key=>1},{[#{key=>1}],[#{key=>1.0}]});
probe(member_duplicates) -> queue:member(1,{[1,2,1],[1,2,1]});
probe(member_front_hit_bad_rear) -> queue:member(1,{[2|tail],[1]});
probe(member_rear_hit_bad_front) -> queue:member(1,{[1],[2|tail]});
probe(member_false_tail_first) -> queue:member(x,{[],[x|false]});
probe(member_false_tail_later) -> queue:member(x,{[],[prefix,x|false]});
probe(member_opaque_tail_first) -> queue:member(x,{[],[x|tail]});
probe(member_rear_improper_hit) -> queue:member(x,{[x|tail],[]});
probe(delete_float_exact) -> queue:delete(1,{[1.0],[2]});
probe(delete_float_reverse_exact) -> queue:delete(1.0,{[1],[2]});
probe(delete_tuple_exact) -> queue:delete({key,1},{[{key,1.0}],[{key,1}]});
probe(delete_map_key) -> queue:delete(#{key=>1},{[#{key=>1}],[#{key=>1.0}]});
probe(delete_duplicates) -> queue:delete(1,{[1,2,1],[1,2,1]});
probe(delete_front_hit_bad_rear) -> queue:delete(1,{[2|tail],[1]});
probe(delete_rear_hit_bad_front) -> queue:delete(1,{[1],[2|tail]});
probe(delete_false_tail_first) -> queue:delete(x,{[],[x|false]});
probe(delete_false_tail_later) -> queue:delete(x,{[],[prefix,x|false]});
probe(delete_opaque_tail_first) -> queue:delete(x,{[],[x|tail]});
probe(delete_rear_improper_hit) -> queue:delete(x,{[x|tail],[]});
probe(delete_r_float_exact) -> queue:delete_r(1,{[1.0],[2]});
probe(delete_r_float_reverse_exact) -> queue:delete_r(1.0,{[1],[2]});
probe(delete_r_tuple_exact) -> queue:delete_r({key,1},{[{key,1.0}],[{key,1}]});
probe(delete_r_map_key) -> queue:delete_r(#{key=>1},{[#{key=>1}],[#{key=>1.0}]});
probe(delete_r_duplicates) -> queue:delete_r(1,{[1,2,1],[1,2,1]});
probe(delete_r_front_hit_bad_rear) -> queue:delete_r(1,{[2|tail],[1]});
probe(delete_r_rear_hit_bad_front) -> queue:delete_r(1,{[1],[2|tail]});
probe(delete_r_false_tail_first) -> queue:delete_r(x,{[],[x|false]});
probe(delete_r_false_tail_later) -> queue:delete_r(x,{[],[prefix,x|false]});
probe(delete_r_opaque_tail_first) -> queue:delete_r(x,{[],[x|tail]});
probe(delete_r_rear_improper_hit) -> queue:delete_r(x,{[x|tail],[]});
probe(join_empty_empty) -> queue:join({[],[]},{[],[]});
probe(join_left_empty) -> queue:join({[],[]},{[4,3],[1,2]});
probe(join_right_empty) -> queue:join({[4,3],[1,2]},{[],[]});
probe(join_two_full) -> queue:join({[2],[1]},{[4],[3]});
probe(join_both_bad) -> queue:join(bad,bad);
probe(join_left_bad) -> queue:join(bad,{[],[]});
probe(join_right_bad) -> queue:join({[],[]},bad);
probe(join_left_bad_field) -> queue:join({bad,[]},{[],[]});
probe(join_right_bad_field) -> queue:join({[],[]},{[],bad});
probe(join_left_improper_empty) -> queue:join({[1|tail],[]},{[],[]});
probe(join_empty_right_improper) -> queue:join({[],[]},{[],[1|tail]});
probe(join_left_rear_improper) -> queue:join({[1,2|rear_tail],[front]},{[],[right]});
probe(join_left_front_improper) -> queue:join({[rear],[1,2|front_tail]},{[],[right]});
probe(join_right_front_improper_tail) -> queue:join({[2],[1]},{[4],[3|tail]});
probe(join_both_improper_precedence) -> queue:join({[1|rear_tail],[2|front_tail]},{[3|r2],[4|f2]});
probe(split_empty_0) -> queue:split(0,{[],[]});
probe(split_empty_1) -> queue:split(1,{[],[]});
probe(split_empty_2) -> queue:split(2,{[],[]});
probe(split_empty_3) -> queue:split(3,{[],[]});
probe(split_empty_4) -> queue:split(4,{[],[]});
probe(split_empty_5) -> queue:split(5,{[],[]});
probe(split_empty_minus1) -> queue:split(-1,{[],[]});
probe(split_empty_1p0) -> queue:split(1.0,{[],[]});
probe(split_empty_bad) -> queue:split(bad,{[],[]});
probe(split_empty_1_bsl_128) -> queue:split(1 bsl 128,{[],[]});
probe(split_balanced_0) -> queue:split(0,{[4,3],[1,2]});
probe(split_balanced_1) -> queue:split(1,{[4,3],[1,2]});
probe(split_balanced_2) -> queue:split(2,{[4,3],[1,2]});
probe(split_balanced_3) -> queue:split(3,{[4,3],[1,2]});
probe(split_balanced_4) -> queue:split(4,{[4,3],[1,2]});
probe(split_balanced_5) -> queue:split(5,{[4,3],[1,2]});
probe(split_balanced_minus1) -> queue:split(-1,{[4,3],[1,2]});
probe(split_balanced_1p0) -> queue:split(1.0,{[4,3],[1,2]});
probe(split_balanced_bad) -> queue:split(bad,{[4,3],[1,2]});
probe(split_balanced_1_bsl_128) -> queue:split(1 bsl 128,{[4,3],[1,2]});
probe(split_rear_only_0) -> queue:split(0,{[4,3,2,1],[]});
probe(split_rear_only_1) -> queue:split(1,{[4,3,2,1],[]});
probe(split_rear_only_2) -> queue:split(2,{[4,3,2,1],[]});
probe(split_rear_only_3) -> queue:split(3,{[4,3,2,1],[]});
probe(split_rear_only_4) -> queue:split(4,{[4,3,2,1],[]});
probe(split_rear_only_5) -> queue:split(5,{[4,3,2,1],[]});
probe(split_rear_only_minus1) -> queue:split(-1,{[4,3,2,1],[]});
probe(split_rear_only_1p0) -> queue:split(1.0,{[4,3,2,1],[]});
probe(split_rear_only_bad) -> queue:split(bad,{[4,3,2,1],[]});
probe(split_rear_only_1_bsl_128) -> queue:split(1 bsl 128,{[4,3,2,1],[]});
probe(split_front_only_0) -> queue:split(0,{[],[1,2,3,4]});
probe(split_front_only_1) -> queue:split(1,{[],[1,2,3,4]});
probe(split_front_only_2) -> queue:split(2,{[],[1,2,3,4]});
probe(split_front_only_3) -> queue:split(3,{[],[1,2,3,4]});
probe(split_front_only_4) -> queue:split(4,{[],[1,2,3,4]});
probe(split_front_only_5) -> queue:split(5,{[],[1,2,3,4]});
probe(split_front_only_minus1) -> queue:split(-1,{[],[1,2,3,4]});
probe(split_front_only_1p0) -> queue:split(1.0,{[],[1,2,3,4]});
probe(split_front_only_bad) -> queue:split(bad,{[],[1,2,3,4]});
probe(split_front_only_1_bsl_128) -> queue:split(1 bsl 128,{[],[1,2,3,4]});
probe(split_front_improper_0) -> queue:split(0,{[4],[1,2,3|tail]});
probe(split_front_improper_1) -> queue:split(1,{[4],[1,2,3|tail]});
probe(split_front_improper_2) -> queue:split(2,{[4],[1,2,3|tail]});
probe(split_front_improper_3) -> queue:split(3,{[4],[1,2,3|tail]});
probe(split_front_improper_4) -> queue:split(4,{[4],[1,2,3|tail]});
probe(split_front_improper_5) -> queue:split(5,{[4],[1,2,3|tail]});
probe(split_front_improper_minus1) -> queue:split(-1,{[4],[1,2,3|tail]});
probe(split_front_improper_1p0) -> queue:split(1.0,{[4],[1,2,3|tail]});
probe(split_front_improper_bad) -> queue:split(bad,{[4],[1,2,3|tail]});
probe(split_front_improper_1_bsl_128) -> queue:split(1 bsl 128,{[4],[1,2,3|tail]});
probe(split_rear_improper_0) -> queue:split(0,{[4,3|tail],[1,2]});
probe(split_rear_improper_1) -> queue:split(1,{[4,3|tail],[1,2]});
probe(split_rear_improper_2) -> queue:split(2,{[4,3|tail],[1,2]});
probe(split_rear_improper_3) -> queue:split(3,{[4,3|tail],[1,2]});
probe(split_rear_improper_4) -> queue:split(4,{[4,3|tail],[1,2]});
probe(split_rear_improper_5) -> queue:split(5,{[4,3|tail],[1,2]});
probe(split_rear_improper_minus1) -> queue:split(-1,{[4,3|tail],[1,2]});
probe(split_rear_improper_1p0) -> queue:split(1.0,{[4,3|tail],[1,2]});
probe(split_rear_improper_bad) -> queue:split(bad,{[4,3|tail],[1,2]});
probe(split_rear_improper_1_bsl_128) -> queue:split(1 bsl 128,{[4,3|tail],[1,2]});
probe(split_bad_0) -> queue:split(0,bad);
probe(split_bad_1) -> queue:split(1,bad);
probe(split_bad_2) -> queue:split(2,bad);
probe(split_bad_3) -> queue:split(3,bad);
probe(split_bad_4) -> queue:split(4,bad);
probe(split_bad_5) -> queue:split(5,bad);
probe(split_bad_minus1) -> queue:split(-1,bad);
probe(split_bad_1p0) -> queue:split(1.0,bad);
probe(split_bad_bad) -> queue:split(bad,bad);
probe(split_bad_1_bsl_128) -> queue:split(1 bsl 128,bad);
probe(member_caller_bad) -> queue_all368_support:caller(queue,member,[1,bad]);
probe(reverse_caller_bad) -> queue_all368_support:caller(queue,reverse,[bad]);
probe(join_caller_bad) -> queue_all368_support:caller(queue,join,[bad,bad]);
probe(split_caller_bad) -> queue_all368_support:caller(queue,split,[1,bad]);
probe(delete_caller_bad) -> queue_all368_support:caller(queue,delete,[1,bad]);
probe(delete_r_caller_bad) -> queue_all368_support:caller(queue,delete_r,[1,bad]).
