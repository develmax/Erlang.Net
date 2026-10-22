-module(queue_all368_queue_ends_right).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [head_bad_atom,
            head_bad_tuple0,
            head_bad_tuple1,
            head_bad_tuple3,
            head_bad_rear,
            head_bad_front,
            head_empty,
            head_rear_one,
            head_rear_two,
            head_rear_three,
            head_front_one,
            head_front_two,
            head_front_three,
            head_split,
            head_rear_improper_one,
            head_rear_improper_three,
            head_front_improper_one,
            head_front_improper_three,
            head_both_improper,
            head_rear_improper_front_one,
            head_rear_one_front_improper,
            head_last_improper_1,
            head_last_improper_3,
            head_last_improper_257,
            head_caller_bad,
            tail_bad_atom,
            tail_bad_tuple0,
            tail_bad_tuple1,
            tail_bad_tuple3,
            tail_bad_rear,
            tail_bad_front,
            tail_empty,
            tail_rear_one,
            tail_rear_two,
            tail_rear_three,
            tail_front_one,
            tail_front_two,
            tail_front_three,
            tail_split,
            tail_rear_improper_one,
            tail_rear_improper_three,
            tail_front_improper_one,
            tail_front_improper_three,
            tail_both_improper,
            tail_rear_improper_front_one,
            tail_rear_one_front_improper,
            tail_caller_bad,
            snoc_bad_atom,
            snoc_bad_tuple0,
            snoc_bad_tuple1,
            snoc_bad_tuple3,
            snoc_bad_rear,
            snoc_bad_front,
            snoc_empty,
            snoc_rear_one,
            snoc_rear_two,
            snoc_rear_three,
            snoc_front_one,
            snoc_front_two,
            snoc_front_three,
            snoc_split,
            snoc_rear_improper_one,
            snoc_rear_improper_three,
            snoc_front_improper_one,
            snoc_front_improper_three,
            snoc_both_improper,
            snoc_rear_improper_front_one,
            snoc_rear_one_front_improper,
            snoc_caller_bad,
            last_bad_atom,
            last_bad_tuple0,
            last_bad_tuple1,
            last_bad_tuple3,
            last_bad_rear,
            last_bad_front,
            last_empty,
            last_rear_one,
            last_rear_two,
            last_rear_three,
            last_front_one,
            last_front_two,
            last_front_three,
            last_split,
            last_rear_improper_one,
            last_rear_improper_three,
            last_front_improper_one,
            last_front_improper_three,
            last_both_improper,
            last_rear_improper_front_one,
            last_rear_one_front_improper,
            last_last_improper_1,
            last_last_improper_3,
            last_last_improper_257,
            last_caller_bad,
            daeh_bad_atom,
            daeh_bad_tuple0,
            daeh_bad_tuple1,
            daeh_bad_tuple3,
            daeh_bad_rear,
            daeh_bad_front,
            daeh_empty,
            daeh_rear_one,
            daeh_rear_two,
            daeh_rear_three,
            daeh_front_one,
            daeh_front_two,
            daeh_front_three,
            daeh_split,
            daeh_rear_improper_one,
            daeh_rear_improper_three,
            daeh_front_improper_one,
            daeh_front_improper_three,
            daeh_both_improper,
            daeh_rear_improper_front_one,
            daeh_rear_one_front_improper,
            daeh_last_improper_1,
            daeh_last_improper_3,
            daeh_last_improper_257,
            daeh_caller_bad,
            init_bad_atom,
            init_bad_tuple0,
            init_bad_tuple1,
            init_bad_tuple3,
            init_bad_rear,
            init_bad_front,
            init_empty,
            init_rear_one,
            init_rear_two,
            init_rear_three,
            init_front_one,
            init_front_two,
            init_front_three,
            init_split,
            init_rear_improper_one,
            init_rear_improper_three,
            init_front_improper_one,
            init_front_improper_three,
            init_both_improper,
            init_rear_improper_front_one,
            init_rear_one_front_improper,
            init_caller_bad,
            liat_bad_atom,
            liat_bad_tuple0,
            liat_bad_tuple1,
            liat_bad_tuple3,
            liat_bad_rear,
            liat_bad_front,
            liat_empty,
            liat_rear_one,
            liat_rear_two,
            liat_rear_three,
            liat_front_one,
            liat_front_two,
            liat_front_three,
            liat_split,
            liat_rear_improper_one,
            liat_rear_improper_three,
            liat_front_improper_one,
            liat_front_improper_three,
            liat_both_improper,
            liat_rear_improper_front_one,
            liat_rear_one_front_improper,
            liat_caller_bad,
            lait_bad_atom,
            lait_bad_tuple0,
            lait_bad_tuple1,
            lait_bad_tuple3,
            lait_bad_rear,
            lait_bad_front,
            lait_empty,
            lait_rear_one,
            lait_rear_two,
            lait_rear_three,
            lait_front_one,
            lait_front_two,
            lait_front_three,
            lait_split,
            lait_rear_improper_one,
            lait_rear_improper_three,
            lait_front_improper_one,
            lait_front_improper_three,
            lait_both_improper,
            lait_rear_improper_front_one,
            lait_rear_one_front_improper,
            lait_caller_bad].
run() -> queue_all368_support:run(queue_all368_queue_ends_right,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_queue_ends_right,cases()).
probe(head_bad_atom) -> queue:head(bad);
probe(head_bad_tuple0) -> queue:head({});
probe(head_bad_tuple1) -> queue:head({[]});
probe(head_bad_tuple3) -> queue:head({[],[],[]});
probe(head_bad_rear) -> queue:head({bad,[]});
probe(head_bad_front) -> queue:head({[],bad});
probe(head_empty) -> queue:head({[],[]});
probe(head_rear_one) -> queue:head({[1],[]});
probe(head_rear_two) -> queue:head({[2,1],[]});
probe(head_rear_three) -> queue:head({[3,2,1],[]});
probe(head_front_one) -> queue:head({[],[1]});
probe(head_front_two) -> queue:head({[],[1,2]});
probe(head_front_three) -> queue:head({[],[1,2,3]});
probe(head_split) -> queue:head({[4,3],[1,2]});
probe(head_rear_improper_one) -> queue:head({[1|tail],[]});
probe(head_rear_improper_three) -> queue:head({[1,2,3|tail],[]});
probe(head_front_improper_one) -> queue:head({[],[1|tail]});
probe(head_front_improper_three) -> queue:head({[],[1,2,3|tail]});
probe(head_both_improper) -> queue:head({[4,3|rear_tail],[1,2|front_tail]});
probe(head_rear_improper_front_one) -> queue:head({[4,3|tail],[1]});
probe(head_rear_one_front_improper) -> queue:head({[4],[1,2|tail]});
probe(head_last_improper_1) -> queue:head({queue_all368_support:bad_list(1),[]});
probe(head_last_improper_3) -> queue:head({queue_all368_support:bad_list(3),[]});
probe(head_last_improper_257) -> queue:head({queue_all368_support:bad_list(257),[]});
probe(head_caller_bad) -> queue_all368_support:caller(queue,head,[bad]);
probe(tail_bad_atom) -> queue:tail(bad);
probe(tail_bad_tuple0) -> queue:tail({});
probe(tail_bad_tuple1) -> queue:tail({[]});
probe(tail_bad_tuple3) -> queue:tail({[],[],[]});
probe(tail_bad_rear) -> queue:tail({bad,[]});
probe(tail_bad_front) -> queue:tail({[],bad});
probe(tail_empty) -> queue:tail({[],[]});
probe(tail_rear_one) -> queue:tail({[1],[]});
probe(tail_rear_two) -> queue:tail({[2,1],[]});
probe(tail_rear_three) -> queue:tail({[3,2,1],[]});
probe(tail_front_one) -> queue:tail({[],[1]});
probe(tail_front_two) -> queue:tail({[],[1,2]});
probe(tail_front_three) -> queue:tail({[],[1,2,3]});
probe(tail_split) -> queue:tail({[4,3],[1,2]});
probe(tail_rear_improper_one) -> queue:tail({[1|tail],[]});
probe(tail_rear_improper_three) -> queue:tail({[1,2,3|tail],[]});
probe(tail_front_improper_one) -> queue:tail({[],[1|tail]});
probe(tail_front_improper_three) -> queue:tail({[],[1,2,3|tail]});
probe(tail_both_improper) -> queue:tail({[4,3|rear_tail],[1,2|front_tail]});
probe(tail_rear_improper_front_one) -> queue:tail({[4,3|tail],[1]});
probe(tail_rear_one_front_improper) -> queue:tail({[4],[1,2|tail]});
probe(tail_caller_bad) -> queue_all368_support:caller(queue,tail,[bad]);
probe(snoc_bad_atom) -> queue:snoc(bad,item);
probe(snoc_bad_tuple0) -> queue:snoc({},item);
probe(snoc_bad_tuple1) -> queue:snoc({[]},item);
probe(snoc_bad_tuple3) -> queue:snoc({[],[],[]},item);
probe(snoc_bad_rear) -> queue:snoc({bad,[]},item);
probe(snoc_bad_front) -> queue:snoc({[],bad},item);
probe(snoc_empty) -> queue:snoc({[],[]},item);
probe(snoc_rear_one) -> queue:snoc({[1],[]},item);
probe(snoc_rear_two) -> queue:snoc({[2,1],[]},item);
probe(snoc_rear_three) -> queue:snoc({[3,2,1],[]},item);
probe(snoc_front_one) -> queue:snoc({[],[1]},item);
probe(snoc_front_two) -> queue:snoc({[],[1,2]},item);
probe(snoc_front_three) -> queue:snoc({[],[1,2,3]},item);
probe(snoc_split) -> queue:snoc({[4,3],[1,2]},item);
probe(snoc_rear_improper_one) -> queue:snoc({[1|tail],[]},item);
probe(snoc_rear_improper_three) -> queue:snoc({[1,2,3|tail],[]},item);
probe(snoc_front_improper_one) -> queue:snoc({[],[1|tail]},item);
probe(snoc_front_improper_three) -> queue:snoc({[],[1,2,3|tail]},item);
probe(snoc_both_improper) -> queue:snoc({[4,3|rear_tail],[1,2|front_tail]},item);
probe(snoc_rear_improper_front_one) -> queue:snoc({[4,3|tail],[1]},item);
probe(snoc_rear_one_front_improper) -> queue:snoc({[4],[1,2|tail]},item);
probe(snoc_caller_bad) -> queue_all368_support:caller(queue,snoc,[bad,item]);
probe(last_bad_atom) -> queue:last(bad);
probe(last_bad_tuple0) -> queue:last({});
probe(last_bad_tuple1) -> queue:last({[]});
probe(last_bad_tuple3) -> queue:last({[],[],[]});
probe(last_bad_rear) -> queue:last({bad,[]});
probe(last_bad_front) -> queue:last({[],bad});
probe(last_empty) -> queue:last({[],[]});
probe(last_rear_one) -> queue:last({[1],[]});
probe(last_rear_two) -> queue:last({[2,1],[]});
probe(last_rear_three) -> queue:last({[3,2,1],[]});
probe(last_front_one) -> queue:last({[],[1]});
probe(last_front_two) -> queue:last({[],[1,2]});
probe(last_front_three) -> queue:last({[],[1,2,3]});
probe(last_split) -> queue:last({[4,3],[1,2]});
probe(last_rear_improper_one) -> queue:last({[1|tail],[]});
probe(last_rear_improper_three) -> queue:last({[1,2,3|tail],[]});
probe(last_front_improper_one) -> queue:last({[],[1|tail]});
probe(last_front_improper_three) -> queue:last({[],[1,2,3|tail]});
probe(last_both_improper) -> queue:last({[4,3|rear_tail],[1,2|front_tail]});
probe(last_rear_improper_front_one) -> queue:last({[4,3|tail],[1]});
probe(last_rear_one_front_improper) -> queue:last({[4],[1,2|tail]});
probe(last_last_improper_1) -> queue:last({[],queue_all368_support:bad_list(1)});
probe(last_last_improper_3) -> queue:last({[],queue_all368_support:bad_list(3)});
probe(last_last_improper_257) -> queue:last({[],queue_all368_support:bad_list(257)});
probe(last_caller_bad) -> queue_all368_support:caller(queue,last,[bad]);
probe(daeh_bad_atom) -> queue:daeh(bad);
probe(daeh_bad_tuple0) -> queue:daeh({});
probe(daeh_bad_tuple1) -> queue:daeh({[]});
probe(daeh_bad_tuple3) -> queue:daeh({[],[],[]});
probe(daeh_bad_rear) -> queue:daeh({bad,[]});
probe(daeh_bad_front) -> queue:daeh({[],bad});
probe(daeh_empty) -> queue:daeh({[],[]});
probe(daeh_rear_one) -> queue:daeh({[1],[]});
probe(daeh_rear_two) -> queue:daeh({[2,1],[]});
probe(daeh_rear_three) -> queue:daeh({[3,2,1],[]});
probe(daeh_front_one) -> queue:daeh({[],[1]});
probe(daeh_front_two) -> queue:daeh({[],[1,2]});
probe(daeh_front_three) -> queue:daeh({[],[1,2,3]});
probe(daeh_split) -> queue:daeh({[4,3],[1,2]});
probe(daeh_rear_improper_one) -> queue:daeh({[1|tail],[]});
probe(daeh_rear_improper_three) -> queue:daeh({[1,2,3|tail],[]});
probe(daeh_front_improper_one) -> queue:daeh({[],[1|tail]});
probe(daeh_front_improper_three) -> queue:daeh({[],[1,2,3|tail]});
probe(daeh_both_improper) -> queue:daeh({[4,3|rear_tail],[1,2|front_tail]});
probe(daeh_rear_improper_front_one) -> queue:daeh({[4,3|tail],[1]});
probe(daeh_rear_one_front_improper) -> queue:daeh({[4],[1,2|tail]});
probe(daeh_last_improper_1) -> queue:daeh({[],queue_all368_support:bad_list(1)});
probe(daeh_last_improper_3) -> queue:daeh({[],queue_all368_support:bad_list(3)});
probe(daeh_last_improper_257) -> queue:daeh({[],queue_all368_support:bad_list(257)});
probe(daeh_caller_bad) -> queue_all368_support:caller(queue,daeh,[bad]);
probe(init_bad_atom) -> queue:init(bad);
probe(init_bad_tuple0) -> queue:init({});
probe(init_bad_tuple1) -> queue:init({[]});
probe(init_bad_tuple3) -> queue:init({[],[],[]});
probe(init_bad_rear) -> queue:init({bad,[]});
probe(init_bad_front) -> queue:init({[],bad});
probe(init_empty) -> queue:init({[],[]});
probe(init_rear_one) -> queue:init({[1],[]});
probe(init_rear_two) -> queue:init({[2,1],[]});
probe(init_rear_three) -> queue:init({[3,2,1],[]});
probe(init_front_one) -> queue:init({[],[1]});
probe(init_front_two) -> queue:init({[],[1,2]});
probe(init_front_three) -> queue:init({[],[1,2,3]});
probe(init_split) -> queue:init({[4,3],[1,2]});
probe(init_rear_improper_one) -> queue:init({[1|tail],[]});
probe(init_rear_improper_three) -> queue:init({[1,2,3|tail],[]});
probe(init_front_improper_one) -> queue:init({[],[1|tail]});
probe(init_front_improper_three) -> queue:init({[],[1,2,3|tail]});
probe(init_both_improper) -> queue:init({[4,3|rear_tail],[1,2|front_tail]});
probe(init_rear_improper_front_one) -> queue:init({[4,3|tail],[1]});
probe(init_rear_one_front_improper) -> queue:init({[4],[1,2|tail]});
probe(init_caller_bad) -> queue_all368_support:caller(queue,init,[bad]);
probe(liat_bad_atom) -> queue:liat(bad);
probe(liat_bad_tuple0) -> queue:liat({});
probe(liat_bad_tuple1) -> queue:liat({[]});
probe(liat_bad_tuple3) -> queue:liat({[],[],[]});
probe(liat_bad_rear) -> queue:liat({bad,[]});
probe(liat_bad_front) -> queue:liat({[],bad});
probe(liat_empty) -> queue:liat({[],[]});
probe(liat_rear_one) -> queue:liat({[1],[]});
probe(liat_rear_two) -> queue:liat({[2,1],[]});
probe(liat_rear_three) -> queue:liat({[3,2,1],[]});
probe(liat_front_one) -> queue:liat({[],[1]});
probe(liat_front_two) -> queue:liat({[],[1,2]});
probe(liat_front_three) -> queue:liat({[],[1,2,3]});
probe(liat_split) -> queue:liat({[4,3],[1,2]});
probe(liat_rear_improper_one) -> queue:liat({[1|tail],[]});
probe(liat_rear_improper_three) -> queue:liat({[1,2,3|tail],[]});
probe(liat_front_improper_one) -> queue:liat({[],[1|tail]});
probe(liat_front_improper_three) -> queue:liat({[],[1,2,3|tail]});
probe(liat_both_improper) -> queue:liat({[4,3|rear_tail],[1,2|front_tail]});
probe(liat_rear_improper_front_one) -> queue:liat({[4,3|tail],[1]});
probe(liat_rear_one_front_improper) -> queue:liat({[4],[1,2|tail]});
probe(liat_caller_bad) -> queue_all368_support:caller(queue,liat,[bad]);
probe(lait_bad_atom) -> erlang:apply(queue,lait,[bad]);
probe(lait_bad_tuple0) -> erlang:apply(queue,lait,[{}]);
probe(lait_bad_tuple1) -> erlang:apply(queue,lait,[{[]}]);
probe(lait_bad_tuple3) -> erlang:apply(queue,lait,[{[],[],[]}]);
probe(lait_bad_rear) -> erlang:apply(queue,lait,[{bad,[]}]);
probe(lait_bad_front) -> erlang:apply(queue,lait,[{[],bad}]);
probe(lait_empty) -> erlang:apply(queue,lait,[{[],[]}]);
probe(lait_rear_one) -> erlang:apply(queue,lait,[{[1],[]}]);
probe(lait_rear_two) -> erlang:apply(queue,lait,[{[2,1],[]}]);
probe(lait_rear_three) -> erlang:apply(queue,lait,[{[3,2,1],[]}]);
probe(lait_front_one) -> erlang:apply(queue,lait,[{[],[1]}]);
probe(lait_front_two) -> erlang:apply(queue,lait,[{[],[1,2]}]);
probe(lait_front_three) -> erlang:apply(queue,lait,[{[],[1,2,3]}]);
probe(lait_split) -> erlang:apply(queue,lait,[{[4,3],[1,2]}]);
probe(lait_rear_improper_one) -> erlang:apply(queue,lait,[{[1|tail],[]}]);
probe(lait_rear_improper_three) -> erlang:apply(queue,lait,[{[1,2,3|tail],[]}]);
probe(lait_front_improper_one) -> erlang:apply(queue,lait,[{[],[1|tail]}]);
probe(lait_front_improper_three) -> erlang:apply(queue,lait,[{[],[1,2,3|tail]}]);
probe(lait_both_improper) -> erlang:apply(queue,lait,[{[4,3|rear_tail],[1,2|front_tail]}]);
probe(lait_rear_improper_front_one) -> erlang:apply(queue,lait,[{[4,3|tail],[1]}]);
probe(lait_rear_one_front_improper) -> erlang:apply(queue,lait,[{[4],[1,2|tail]}]);
probe(lait_caller_bad) -> queue_all368_support:caller(queue,lait,[bad]).
