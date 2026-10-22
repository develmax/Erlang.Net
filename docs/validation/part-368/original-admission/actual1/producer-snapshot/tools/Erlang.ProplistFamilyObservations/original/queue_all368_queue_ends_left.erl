-module(queue_all368_queue_ends_left).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [in_r_bad_atom,
            in_r_bad_tuple0,
            in_r_bad_tuple1,
            in_r_bad_tuple3,
            in_r_bad_rear,
            in_r_bad_front,
            in_r_empty,
            in_r_rear_one,
            in_r_rear_two,
            in_r_rear_three,
            in_r_front_one,
            in_r_front_two,
            in_r_front_three,
            in_r_split,
            in_r_rear_improper_one,
            in_r_rear_improper_three,
            in_r_front_improper_one,
            in_r_front_improper_three,
            in_r_both_improper,
            in_r_rear_improper_front_one,
            in_r_rear_one_front_improper,
            in_r_caller_bad,
            out_r_bad_atom,
            out_r_bad_tuple0,
            out_r_bad_tuple1,
            out_r_bad_tuple3,
            out_r_bad_rear,
            out_r_bad_front,
            out_r_empty,
            out_r_rear_one,
            out_r_rear_two,
            out_r_rear_three,
            out_r_front_one,
            out_r_front_two,
            out_r_front_three,
            out_r_split,
            out_r_rear_improper_one,
            out_r_rear_improper_three,
            out_r_front_improper_one,
            out_r_front_improper_three,
            out_r_both_improper,
            out_r_rear_improper_front_one,
            out_r_rear_one_front_improper,
            out_r_inline_split_2,
            out_r_inline_split_3,
            out_r_inline_split_4,
            out_r_inline_split_5,
            out_r_inline_split_32,
            out_r_inline_split_33,
            out_r_inline_split_257,
            out_r_caller_bad,
            get_bad_atom,
            get_bad_tuple0,
            get_bad_tuple1,
            get_bad_tuple3,
            get_bad_rear,
            get_bad_front,
            get_empty,
            get_rear_one,
            get_rear_two,
            get_rear_three,
            get_front_one,
            get_front_two,
            get_front_three,
            get_split,
            get_rear_improper_one,
            get_rear_improper_three,
            get_front_improper_one,
            get_front_improper_three,
            get_both_improper,
            get_rear_improper_front_one,
            get_rear_one_front_improper,
            get_last_improper_1,
            get_last_improper_3,
            get_last_improper_257,
            get_caller_bad,
            get_r_bad_atom,
            get_r_bad_tuple0,
            get_r_bad_tuple1,
            get_r_bad_tuple3,
            get_r_bad_rear,
            get_r_bad_front,
            get_r_empty,
            get_r_rear_one,
            get_r_rear_two,
            get_r_rear_three,
            get_r_front_one,
            get_r_front_two,
            get_r_front_three,
            get_r_split,
            get_r_rear_improper_one,
            get_r_rear_improper_three,
            get_r_front_improper_one,
            get_r_front_improper_three,
            get_r_both_improper,
            get_r_rear_improper_front_one,
            get_r_rear_one_front_improper,
            get_r_last_improper_1,
            get_r_last_improper_3,
            get_r_last_improper_257,
            get_r_caller_bad,
            peek_bad_atom,
            peek_bad_tuple0,
            peek_bad_tuple1,
            peek_bad_tuple3,
            peek_bad_rear,
            peek_bad_front,
            peek_empty,
            peek_rear_one,
            peek_rear_two,
            peek_rear_three,
            peek_front_one,
            peek_front_two,
            peek_front_three,
            peek_split,
            peek_rear_improper_one,
            peek_rear_improper_three,
            peek_front_improper_one,
            peek_front_improper_three,
            peek_both_improper,
            peek_rear_improper_front_one,
            peek_rear_one_front_improper,
            peek_last_improper_1,
            peek_last_improper_3,
            peek_last_improper_257,
            peek_caller_bad,
            peek_r_bad_atom,
            peek_r_bad_tuple0,
            peek_r_bad_tuple1,
            peek_r_bad_tuple3,
            peek_r_bad_rear,
            peek_r_bad_front,
            peek_r_empty,
            peek_r_rear_one,
            peek_r_rear_two,
            peek_r_rear_three,
            peek_r_front_one,
            peek_r_front_two,
            peek_r_front_three,
            peek_r_split,
            peek_r_rear_improper_one,
            peek_r_rear_improper_three,
            peek_r_front_improper_one,
            peek_r_front_improper_three,
            peek_r_both_improper,
            peek_r_rear_improper_front_one,
            peek_r_rear_one_front_improper,
            peek_r_last_improper_1,
            peek_r_last_improper_3,
            peek_r_last_improper_257,
            peek_r_caller_bad,
            drop_bad_atom,
            drop_bad_tuple0,
            drop_bad_tuple1,
            drop_bad_tuple3,
            drop_bad_rear,
            drop_bad_front,
            drop_empty,
            drop_rear_one,
            drop_rear_two,
            drop_rear_three,
            drop_front_one,
            drop_front_two,
            drop_front_three,
            drop_split,
            drop_rear_improper_one,
            drop_rear_improper_three,
            drop_front_improper_one,
            drop_front_improper_three,
            drop_both_improper,
            drop_rear_improper_front_one,
            drop_rear_one_front_improper,
            drop_inline_split_2,
            drop_inline_split_3,
            drop_inline_split_4,
            drop_inline_split_5,
            drop_inline_split_32,
            drop_inline_split_33,
            drop_inline_split_257,
            drop_caller_bad,
            drop_r_bad_atom,
            drop_r_bad_tuple0,
            drop_r_bad_tuple1,
            drop_r_bad_tuple3,
            drop_r_bad_rear,
            drop_r_bad_front,
            drop_r_empty,
            drop_r_rear_one,
            drop_r_rear_two,
            drop_r_rear_three,
            drop_r_front_one,
            drop_r_front_two,
            drop_r_front_three,
            drop_r_split,
            drop_r_rear_improper_one,
            drop_r_rear_improper_three,
            drop_r_front_improper_one,
            drop_r_front_improper_three,
            drop_r_both_improper,
            drop_r_rear_improper_front_one,
            drop_r_rear_one_front_improper,
            drop_r_inline_split_2,
            drop_r_inline_split_3,
            drop_r_inline_split_4,
            drop_r_inline_split_5,
            drop_r_inline_split_32,
            drop_r_inline_split_33,
            drop_r_inline_split_257,
            drop_r_caller_bad,
            cons_bad_atom,
            cons_bad_tuple0,
            cons_bad_tuple1,
            cons_bad_tuple3,
            cons_bad_rear,
            cons_bad_front,
            cons_empty,
            cons_rear_one,
            cons_rear_two,
            cons_rear_three,
            cons_front_one,
            cons_front_two,
            cons_front_three,
            cons_split,
            cons_rear_improper_one,
            cons_rear_improper_three,
            cons_front_improper_one,
            cons_front_improper_three,
            cons_both_improper,
            cons_rear_improper_front_one,
            cons_rear_one_front_improper,
            cons_caller_bad].
run() -> queue_all368_support:run(queue_all368_queue_ends_left,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_queue_ends_left,cases()).
probe(in_r_bad_atom) -> queue:in_r(item,bad);
probe(in_r_bad_tuple0) -> queue:in_r(item,{});
probe(in_r_bad_tuple1) -> queue:in_r(item,{[]});
probe(in_r_bad_tuple3) -> queue:in_r(item,{[],[],[]});
probe(in_r_bad_rear) -> queue:in_r(item,{bad,[]});
probe(in_r_bad_front) -> queue:in_r(item,{[],bad});
probe(in_r_empty) -> queue:in_r(item,{[],[]});
probe(in_r_rear_one) -> queue:in_r(item,{[1],[]});
probe(in_r_rear_two) -> queue:in_r(item,{[2,1],[]});
probe(in_r_rear_three) -> queue:in_r(item,{[3,2,1],[]});
probe(in_r_front_one) -> queue:in_r(item,{[],[1]});
probe(in_r_front_two) -> queue:in_r(item,{[],[1,2]});
probe(in_r_front_three) -> queue:in_r(item,{[],[1,2,3]});
probe(in_r_split) -> queue:in_r(item,{[4,3],[1,2]});
probe(in_r_rear_improper_one) -> queue:in_r(item,{[1|tail],[]});
probe(in_r_rear_improper_three) -> queue:in_r(item,{[1,2,3|tail],[]});
probe(in_r_front_improper_one) -> queue:in_r(item,{[],[1|tail]});
probe(in_r_front_improper_three) -> queue:in_r(item,{[],[1,2,3|tail]});
probe(in_r_both_improper) -> queue:in_r(item,{[4,3|rear_tail],[1,2|front_tail]});
probe(in_r_rear_improper_front_one) -> queue:in_r(item,{[4,3|tail],[1]});
probe(in_r_rear_one_front_improper) -> queue:in_r(item,{[4],[1,2|tail]});
probe(in_r_caller_bad) -> queue_all368_support:caller(queue,in_r,[item,bad]);
probe(out_r_bad_atom) -> queue:out_r(bad);
probe(out_r_bad_tuple0) -> queue:out_r({});
probe(out_r_bad_tuple1) -> queue:out_r({[]});
probe(out_r_bad_tuple3) -> queue:out_r({[],[],[]});
probe(out_r_bad_rear) -> queue:out_r({bad,[]});
probe(out_r_bad_front) -> queue:out_r({[],bad});
probe(out_r_empty) -> queue:out_r({[],[]});
probe(out_r_rear_one) -> queue:out_r({[1],[]});
probe(out_r_rear_two) -> queue:out_r({[2,1],[]});
probe(out_r_rear_three) -> queue:out_r({[3,2,1],[]});
probe(out_r_front_one) -> queue:out_r({[],[1]});
probe(out_r_front_two) -> queue:out_r({[],[1,2]});
probe(out_r_front_three) -> queue:out_r({[],[1,2,3]});
probe(out_r_split) -> queue:out_r({[4,3],[1,2]});
probe(out_r_rear_improper_one) -> queue:out_r({[1|tail],[]});
probe(out_r_rear_improper_three) -> queue:out_r({[1,2,3|tail],[]});
probe(out_r_front_improper_one) -> queue:out_r({[],[1|tail]});
probe(out_r_front_improper_three) -> queue:out_r({[],[1,2,3|tail]});
probe(out_r_both_improper) -> queue:out_r({[4,3|rear_tail],[1,2|front_tail]});
probe(out_r_rear_improper_front_one) -> queue:out_r({[4,3|tail],[1]});
probe(out_r_rear_one_front_improper) -> queue:out_r({[4],[1,2|tail]});
probe(out_r_inline_split_2) -> queue:out_r({[front],lists:seq(1,2)});
probe(out_r_inline_split_3) -> queue:out_r({[front],lists:seq(1,3)});
probe(out_r_inline_split_4) -> queue:out_r({[front],lists:seq(1,4)});
probe(out_r_inline_split_5) -> queue:out_r({[front],lists:seq(1,5)});
probe(out_r_inline_split_32) -> queue:out_r({[front],lists:seq(1,32)});
probe(out_r_inline_split_33) -> queue:out_r({[front],lists:seq(1,33)});
probe(out_r_inline_split_257) -> queue:out_r({[front],lists:seq(1,257)});
probe(out_r_caller_bad) -> queue_all368_support:caller(queue,out_r,[bad]);
probe(get_bad_atom) -> queue:get(bad);
probe(get_bad_tuple0) -> queue:get({});
probe(get_bad_tuple1) -> queue:get({[]});
probe(get_bad_tuple3) -> queue:get({[],[],[]});
probe(get_bad_rear) -> queue:get({bad,[]});
probe(get_bad_front) -> queue:get({[],bad});
probe(get_empty) -> queue:get({[],[]});
probe(get_rear_one) -> queue:get({[1],[]});
probe(get_rear_two) -> queue:get({[2,1],[]});
probe(get_rear_three) -> queue:get({[3,2,1],[]});
probe(get_front_one) -> queue:get({[],[1]});
probe(get_front_two) -> queue:get({[],[1,2]});
probe(get_front_three) -> queue:get({[],[1,2,3]});
probe(get_split) -> queue:get({[4,3],[1,2]});
probe(get_rear_improper_one) -> queue:get({[1|tail],[]});
probe(get_rear_improper_three) -> queue:get({[1,2,3|tail],[]});
probe(get_front_improper_one) -> queue:get({[],[1|tail]});
probe(get_front_improper_three) -> queue:get({[],[1,2,3|tail]});
probe(get_both_improper) -> queue:get({[4,3|rear_tail],[1,2|front_tail]});
probe(get_rear_improper_front_one) -> queue:get({[4,3|tail],[1]});
probe(get_rear_one_front_improper) -> queue:get({[4],[1,2|tail]});
probe(get_last_improper_1) -> queue:get({queue_all368_support:bad_list(1),[]});
probe(get_last_improper_3) -> queue:get({queue_all368_support:bad_list(3),[]});
probe(get_last_improper_257) -> queue:get({queue_all368_support:bad_list(257),[]});
probe(get_caller_bad) -> queue_all368_support:caller(queue,get,[bad]);
probe(get_r_bad_atom) -> queue:get_r(bad);
probe(get_r_bad_tuple0) -> queue:get_r({});
probe(get_r_bad_tuple1) -> queue:get_r({[]});
probe(get_r_bad_tuple3) -> queue:get_r({[],[],[]});
probe(get_r_bad_rear) -> queue:get_r({bad,[]});
probe(get_r_bad_front) -> queue:get_r({[],bad});
probe(get_r_empty) -> queue:get_r({[],[]});
probe(get_r_rear_one) -> queue:get_r({[1],[]});
probe(get_r_rear_two) -> queue:get_r({[2,1],[]});
probe(get_r_rear_three) -> queue:get_r({[3,2,1],[]});
probe(get_r_front_one) -> queue:get_r({[],[1]});
probe(get_r_front_two) -> queue:get_r({[],[1,2]});
probe(get_r_front_three) -> queue:get_r({[],[1,2,3]});
probe(get_r_split) -> queue:get_r({[4,3],[1,2]});
probe(get_r_rear_improper_one) -> queue:get_r({[1|tail],[]});
probe(get_r_rear_improper_three) -> queue:get_r({[1,2,3|tail],[]});
probe(get_r_front_improper_one) -> queue:get_r({[],[1|tail]});
probe(get_r_front_improper_three) -> queue:get_r({[],[1,2,3|tail]});
probe(get_r_both_improper) -> queue:get_r({[4,3|rear_tail],[1,2|front_tail]});
probe(get_r_rear_improper_front_one) -> queue:get_r({[4,3|tail],[1]});
probe(get_r_rear_one_front_improper) -> queue:get_r({[4],[1,2|tail]});
probe(get_r_last_improper_1) -> queue:get_r({[],queue_all368_support:bad_list(1)});
probe(get_r_last_improper_3) -> queue:get_r({[],queue_all368_support:bad_list(3)});
probe(get_r_last_improper_257) -> queue:get_r({[],queue_all368_support:bad_list(257)});
probe(get_r_caller_bad) -> queue_all368_support:caller(queue,get_r,[bad]);
probe(peek_bad_atom) -> queue:peek(bad);
probe(peek_bad_tuple0) -> queue:peek({});
probe(peek_bad_tuple1) -> queue:peek({[]});
probe(peek_bad_tuple3) -> queue:peek({[],[],[]});
probe(peek_bad_rear) -> queue:peek({bad,[]});
probe(peek_bad_front) -> queue:peek({[],bad});
probe(peek_empty) -> queue:peek({[],[]});
probe(peek_rear_one) -> queue:peek({[1],[]});
probe(peek_rear_two) -> queue:peek({[2,1],[]});
probe(peek_rear_three) -> queue:peek({[3,2,1],[]});
probe(peek_front_one) -> queue:peek({[],[1]});
probe(peek_front_two) -> queue:peek({[],[1,2]});
probe(peek_front_three) -> queue:peek({[],[1,2,3]});
probe(peek_split) -> queue:peek({[4,3],[1,2]});
probe(peek_rear_improper_one) -> queue:peek({[1|tail],[]});
probe(peek_rear_improper_three) -> queue:peek({[1,2,3|tail],[]});
probe(peek_front_improper_one) -> queue:peek({[],[1|tail]});
probe(peek_front_improper_three) -> queue:peek({[],[1,2,3|tail]});
probe(peek_both_improper) -> queue:peek({[4,3|rear_tail],[1,2|front_tail]});
probe(peek_rear_improper_front_one) -> queue:peek({[4,3|tail],[1]});
probe(peek_rear_one_front_improper) -> queue:peek({[4],[1,2|tail]});
probe(peek_last_improper_1) -> queue:peek({queue_all368_support:bad_list(1),[]});
probe(peek_last_improper_3) -> queue:peek({queue_all368_support:bad_list(3),[]});
probe(peek_last_improper_257) -> queue:peek({queue_all368_support:bad_list(257),[]});
probe(peek_caller_bad) -> queue_all368_support:caller(queue,peek,[bad]);
probe(peek_r_bad_atom) -> queue:peek_r(bad);
probe(peek_r_bad_tuple0) -> queue:peek_r({});
probe(peek_r_bad_tuple1) -> queue:peek_r({[]});
probe(peek_r_bad_tuple3) -> queue:peek_r({[],[],[]});
probe(peek_r_bad_rear) -> queue:peek_r({bad,[]});
probe(peek_r_bad_front) -> queue:peek_r({[],bad});
probe(peek_r_empty) -> queue:peek_r({[],[]});
probe(peek_r_rear_one) -> queue:peek_r({[1],[]});
probe(peek_r_rear_two) -> queue:peek_r({[2,1],[]});
probe(peek_r_rear_three) -> queue:peek_r({[3,2,1],[]});
probe(peek_r_front_one) -> queue:peek_r({[],[1]});
probe(peek_r_front_two) -> queue:peek_r({[],[1,2]});
probe(peek_r_front_three) -> queue:peek_r({[],[1,2,3]});
probe(peek_r_split) -> queue:peek_r({[4,3],[1,2]});
probe(peek_r_rear_improper_one) -> queue:peek_r({[1|tail],[]});
probe(peek_r_rear_improper_three) -> queue:peek_r({[1,2,3|tail],[]});
probe(peek_r_front_improper_one) -> queue:peek_r({[],[1|tail]});
probe(peek_r_front_improper_three) -> queue:peek_r({[],[1,2,3|tail]});
probe(peek_r_both_improper) -> queue:peek_r({[4,3|rear_tail],[1,2|front_tail]});
probe(peek_r_rear_improper_front_one) -> queue:peek_r({[4,3|tail],[1]});
probe(peek_r_rear_one_front_improper) -> queue:peek_r({[4],[1,2|tail]});
probe(peek_r_last_improper_1) -> queue:peek_r({[],queue_all368_support:bad_list(1)});
probe(peek_r_last_improper_3) -> queue:peek_r({[],queue_all368_support:bad_list(3)});
probe(peek_r_last_improper_257) -> queue:peek_r({[],queue_all368_support:bad_list(257)});
probe(peek_r_caller_bad) -> queue_all368_support:caller(queue,peek_r,[bad]);
probe(drop_bad_atom) -> queue:drop(bad);
probe(drop_bad_tuple0) -> queue:drop({});
probe(drop_bad_tuple1) -> queue:drop({[]});
probe(drop_bad_tuple3) -> queue:drop({[],[],[]});
probe(drop_bad_rear) -> queue:drop({bad,[]});
probe(drop_bad_front) -> queue:drop({[],bad});
probe(drop_empty) -> queue:drop({[],[]});
probe(drop_rear_one) -> queue:drop({[1],[]});
probe(drop_rear_two) -> queue:drop({[2,1],[]});
probe(drop_rear_three) -> queue:drop({[3,2,1],[]});
probe(drop_front_one) -> queue:drop({[],[1]});
probe(drop_front_two) -> queue:drop({[],[1,2]});
probe(drop_front_three) -> queue:drop({[],[1,2,3]});
probe(drop_split) -> queue:drop({[4,3],[1,2]});
probe(drop_rear_improper_one) -> queue:drop({[1|tail],[]});
probe(drop_rear_improper_three) -> queue:drop({[1,2,3|tail],[]});
probe(drop_front_improper_one) -> queue:drop({[],[1|tail]});
probe(drop_front_improper_three) -> queue:drop({[],[1,2,3|tail]});
probe(drop_both_improper) -> queue:drop({[4,3|rear_tail],[1,2|front_tail]});
probe(drop_rear_improper_front_one) -> queue:drop({[4,3|tail],[1]});
probe(drop_rear_one_front_improper) -> queue:drop({[4],[1,2|tail]});
probe(drop_inline_split_2) -> queue:drop({lists:seq(1,2),[front]});
probe(drop_inline_split_3) -> queue:drop({lists:seq(1,3),[front]});
probe(drop_inline_split_4) -> queue:drop({lists:seq(1,4),[front]});
probe(drop_inline_split_5) -> queue:drop({lists:seq(1,5),[front]});
probe(drop_inline_split_32) -> queue:drop({lists:seq(1,32),[front]});
probe(drop_inline_split_33) -> queue:drop({lists:seq(1,33),[front]});
probe(drop_inline_split_257) -> queue:drop({lists:seq(1,257),[front]});
probe(drop_caller_bad) -> queue_all368_support:caller(queue,drop,[bad]);
probe(drop_r_bad_atom) -> queue:drop_r(bad);
probe(drop_r_bad_tuple0) -> queue:drop_r({});
probe(drop_r_bad_tuple1) -> queue:drop_r({[]});
probe(drop_r_bad_tuple3) -> queue:drop_r({[],[],[]});
probe(drop_r_bad_rear) -> queue:drop_r({bad,[]});
probe(drop_r_bad_front) -> queue:drop_r({[],bad});
probe(drop_r_empty) -> queue:drop_r({[],[]});
probe(drop_r_rear_one) -> queue:drop_r({[1],[]});
probe(drop_r_rear_two) -> queue:drop_r({[2,1],[]});
probe(drop_r_rear_three) -> queue:drop_r({[3,2,1],[]});
probe(drop_r_front_one) -> queue:drop_r({[],[1]});
probe(drop_r_front_two) -> queue:drop_r({[],[1,2]});
probe(drop_r_front_three) -> queue:drop_r({[],[1,2,3]});
probe(drop_r_split) -> queue:drop_r({[4,3],[1,2]});
probe(drop_r_rear_improper_one) -> queue:drop_r({[1|tail],[]});
probe(drop_r_rear_improper_three) -> queue:drop_r({[1,2,3|tail],[]});
probe(drop_r_front_improper_one) -> queue:drop_r({[],[1|tail]});
probe(drop_r_front_improper_three) -> queue:drop_r({[],[1,2,3|tail]});
probe(drop_r_both_improper) -> queue:drop_r({[4,3|rear_tail],[1,2|front_tail]});
probe(drop_r_rear_improper_front_one) -> queue:drop_r({[4,3|tail],[1]});
probe(drop_r_rear_one_front_improper) -> queue:drop_r({[4],[1,2|tail]});
probe(drop_r_inline_split_2) -> queue:drop_r({[front],lists:seq(1,2)});
probe(drop_r_inline_split_3) -> queue:drop_r({[front],lists:seq(1,3)});
probe(drop_r_inline_split_4) -> queue:drop_r({[front],lists:seq(1,4)});
probe(drop_r_inline_split_5) -> queue:drop_r({[front],lists:seq(1,5)});
probe(drop_r_inline_split_32) -> queue:drop_r({[front],lists:seq(1,32)});
probe(drop_r_inline_split_33) -> queue:drop_r({[front],lists:seq(1,33)});
probe(drop_r_inline_split_257) -> queue:drop_r({[front],lists:seq(1,257)});
probe(drop_r_caller_bad) -> queue_all368_support:caller(queue,drop_r,[bad]);
probe(cons_bad_atom) -> queue:cons(item,bad);
probe(cons_bad_tuple0) -> queue:cons(item,{});
probe(cons_bad_tuple1) -> queue:cons(item,{[]});
probe(cons_bad_tuple3) -> queue:cons(item,{[],[],[]});
probe(cons_bad_rear) -> queue:cons(item,{bad,[]});
probe(cons_bad_front) -> queue:cons(item,{[],bad});
probe(cons_empty) -> queue:cons(item,{[],[]});
probe(cons_rear_one) -> queue:cons(item,{[1],[]});
probe(cons_rear_two) -> queue:cons(item,{[2,1],[]});
probe(cons_rear_three) -> queue:cons(item,{[3,2,1],[]});
probe(cons_front_one) -> queue:cons(item,{[],[1]});
probe(cons_front_two) -> queue:cons(item,{[],[1,2]});
probe(cons_front_three) -> queue:cons(item,{[],[1,2,3]});
probe(cons_split) -> queue:cons(item,{[4,3],[1,2]});
probe(cons_rear_improper_one) -> queue:cons(item,{[1|tail],[]});
probe(cons_rear_improper_three) -> queue:cons(item,{[1,2,3|tail],[]});
probe(cons_front_improper_one) -> queue:cons(item,{[],[1|tail]});
probe(cons_front_improper_three) -> queue:cons(item,{[],[1,2,3|tail]});
probe(cons_both_improper) -> queue:cons(item,{[4,3|rear_tail],[1,2|front_tail]});
probe(cons_rear_improper_front_one) -> queue:cons(item,{[4,3|tail],[1]});
probe(cons_rear_one_front_improper) -> queue:cons(item,{[4],[1,2|tail]});
probe(cons_caller_bad) -> queue_all368_support:caller(queue,cons,[item,bad]).
