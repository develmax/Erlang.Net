-module(queue_all368_queue_callbacks).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [filter_empty,
            filter_front,
            filter_rear,
            filter_balanced,
            filter_bad,
            filter_bad_rear,
            filter_bad_front,
            filter_not_fun_empty,
            filter_not_fun_bad_queue,
            filter_tuple_fun_empty,
            filter_tuple_fun_bad_queue,
            filter_arity0_empty,
            filter_arity0_bad_queue,
            filter_wrong_arity_empty,
            filter_wrong_arity_bad_queue,
            filter_callback_throw,
            filter_callback_error,
            filter_callback_exit,
            filter_callback_badfun,
            filter_callback_badarity,
            filter_improper_front,
            filter_improper_rear,
            filter_improper_both,
            filter_improper_front257,
            filtermap_empty,
            filtermap_front,
            filtermap_rear,
            filtermap_balanced,
            filtermap_bad,
            filtermap_bad_rear,
            filtermap_bad_front,
            filtermap_not_fun_empty,
            filtermap_not_fun_bad_queue,
            filtermap_tuple_fun_empty,
            filtermap_tuple_fun_bad_queue,
            filtermap_arity0_empty,
            filtermap_arity0_bad_queue,
            filtermap_wrong_arity_empty,
            filtermap_wrong_arity_bad_queue,
            filtermap_callback_throw,
            filtermap_callback_error,
            filtermap_callback_exit,
            filtermap_callback_badfun,
            filtermap_callback_badarity,
            filtermap_improper_front,
            filtermap_improper_rear,
            filtermap_improper_both,
            filtermap_improper_front257,
            fold_empty,
            fold_front,
            fold_rear,
            fold_balanced,
            fold_bad,
            fold_bad_rear,
            fold_bad_front,
            fold_not_fun_empty,
            fold_not_fun_bad_queue,
            fold_tuple_fun_empty,
            fold_tuple_fun_bad_queue,
            fold_arity0_empty,
            fold_arity0_bad_queue,
            fold_wrong_arity_empty,
            fold_wrong_arity_bad_queue,
            fold_callback_throw,
            fold_callback_error,
            fold_callback_exit,
            fold_improper_front,
            fold_improper_rear,
            fold_improper_both,
            fold_improper_front257,
            any_empty,
            any_front,
            any_rear,
            any_balanced,
            any_bad,
            any_bad_rear,
            any_bad_front,
            any_not_fun_empty,
            any_not_fun_bad_queue,
            any_tuple_fun_empty,
            any_tuple_fun_bad_queue,
            any_arity0_empty,
            any_arity0_bad_queue,
            any_wrong_arity_empty,
            any_wrong_arity_bad_queue,
            any_callback_throw,
            any_callback_error,
            any_callback_exit,
            any_callback_badfun,
            any_callback_badarity,
            any_improper_front,
            any_improper_rear,
            any_improper_both,
            any_improper_front257,
            all_empty,
            all_front,
            all_rear,
            all_balanced,
            all_bad,
            all_bad_rear,
            all_bad_front,
            all_not_fun_empty,
            all_not_fun_bad_queue,
            all_tuple_fun_empty,
            all_tuple_fun_bad_queue,
            all_arity0_empty,
            all_arity0_bad_queue,
            all_wrong_arity_empty,
            all_wrong_arity_bad_queue,
            all_callback_throw,
            all_callback_error,
            all_callback_exit,
            all_callback_badfun,
            all_callback_badarity,
            all_improper_front,
            all_improper_rear,
            all_improper_both,
            all_improper_front257,
            delete_with_empty,
            delete_with_front,
            delete_with_rear,
            delete_with_balanced,
            delete_with_bad,
            delete_with_bad_rear,
            delete_with_bad_front,
            delete_with_not_fun_empty,
            delete_with_not_fun_bad_queue,
            delete_with_tuple_fun_empty,
            delete_with_tuple_fun_bad_queue,
            delete_with_arity0_empty,
            delete_with_arity0_bad_queue,
            delete_with_wrong_arity_empty,
            delete_with_wrong_arity_bad_queue,
            delete_with_callback_throw,
            delete_with_callback_error,
            delete_with_callback_exit,
            delete_with_callback_badfun,
            delete_with_callback_badarity,
            delete_with_improper_front,
            delete_with_improper_rear,
            delete_with_improper_both,
            delete_with_improper_front257,
            delete_with_r_empty,
            delete_with_r_front,
            delete_with_r_rear,
            delete_with_r_balanced,
            delete_with_r_bad,
            delete_with_r_bad_rear,
            delete_with_r_bad_front,
            delete_with_r_not_fun_empty,
            delete_with_r_not_fun_bad_queue,
            delete_with_r_tuple_fun_empty,
            delete_with_r_tuple_fun_bad_queue,
            delete_with_r_arity0_empty,
            delete_with_r_arity0_bad_queue,
            delete_with_r_wrong_arity_empty,
            delete_with_r_wrong_arity_bad_queue,
            delete_with_r_callback_throw,
            delete_with_r_callback_error,
            delete_with_r_callback_exit,
            delete_with_r_callback_badfun,
            delete_with_r_callback_badarity,
            delete_with_r_improper_front,
            delete_with_r_improper_rear,
            delete_with_r_improper_both,
            delete_with_r_improper_front257,
            filter_result_true_front,
            filter_result_true_rear,
            filter_result_false_front,
            filter_result_false_rear,
            filter_result_select_front,
            filter_result_select_rear,
            filter_result_replace_front,
            filter_result_replace_rear,
            filter_result_empty_front,
            filter_result_empty_rear,
            filter_result_one_front,
            filter_result_one_rear,
            filter_result_many_front,
            filter_result_many_rear,
            filter_result_improper_front,
            filter_result_improper_rear,
            filter_result_bad_front,
            filter_result_bad_rear,
            filter_result_error_later_front,
            filter_result_error_later_rear,
            filtermap_result_true_front,
            filtermap_result_true_rear,
            filtermap_result_false_front,
            filtermap_result_false_rear,
            filtermap_result_select_front,
            filtermap_result_select_rear,
            filtermap_result_replace_front,
            filtermap_result_replace_rear,
            filtermap_result_empty_front,
            filtermap_result_empty_rear,
            filtermap_result_one_front,
            filtermap_result_one_rear,
            filtermap_result_many_front,
            filtermap_result_many_rear,
            filtermap_result_improper_front,
            filtermap_result_improper_rear,
            filtermap_result_bad_front,
            filtermap_result_bad_rear,
            filtermap_result_error_later_front,
            filtermap_result_error_later_rear,
            any_predicate_true,
            any_predicate_false,
            any_predicate_select,
            any_predicate_bad,
            any_predicate_error_later,
            any_sentinel_false_tail_first,
            any_sentinel_false_tail_later,
            any_sentinel_other_tail,
            any_sentinel_rear_improper,
            all_predicate_true,
            all_predicate_false,
            all_predicate_select,
            all_predicate_bad,
            all_predicate_error_later,
            all_sentinel_false_tail_first,
            all_sentinel_false_tail_later,
            all_sentinel_other_tail,
            all_sentinel_rear_improper,
            delete_with_predicate_true,
            delete_with_predicate_false,
            delete_with_predicate_select,
            delete_with_predicate_bad,
            delete_with_predicate_error_later,
            delete_with_sentinel_false_tail_first,
            delete_with_sentinel_false_tail_later,
            delete_with_sentinel_other_tail,
            delete_with_sentinel_rear_improper,
            delete_with_r_predicate_true,
            delete_with_r_predicate_false,
            delete_with_r_predicate_select,
            delete_with_r_predicate_bad,
            delete_with_r_predicate_error_later,
            delete_with_r_sentinel_false_tail_first,
            delete_with_r_sentinel_false_tail_later,
            delete_with_r_sentinel_other_tail,
            delete_with_r_sentinel_rear_improper,
            any_shortcircuit_front_improper,
            any_shortcircuit_bad_rear_skipped,
            all_shortcircuit_front_improper,
            all_shortcircuit_bad_rear_skipped,
            fold_accumulator_empty_tuple,
            fold_accumulator_empty_map,
            fold_accumulator_nonempty_atom,
            filter_caller_bad,
            filtermap_caller_bad,
            fold_caller_bad,
            any_caller_bad,
            all_caller_bad,
            delete_with_caller_bad,
            delete_with_r_caller_bad].
run() -> queue_all368_support:run(queue_all368_queue_callbacks,cases()).
run_stacks() -> queue_all368_support:profiles(queue_all368_queue_callbacks,cases()).
probe(filter_empty) -> queue_all368_support:logged(filter,fun cb_true/1,{[],[]});
probe(filter_front) -> queue_all368_support:logged(filter,fun cb_true/1,{[],[1,2,3]});
probe(filter_rear) -> queue_all368_support:logged(filter,fun cb_true/1,{[3,2,1],[]});
probe(filter_balanced) -> queue_all368_support:logged(filter,fun cb_true/1,{[4,3],[1,2]});
probe(filter_bad) -> queue_all368_support:logged(filter,fun cb_true/1,bad);
probe(filter_bad_rear) -> queue_all368_support:logged(filter,fun cb_true/1,{bad,[1]});
probe(filter_bad_front) -> queue_all368_support:logged(filter,fun cb_true/1,{[1],bad});
probe(filter_not_fun_empty) -> queue:filter(bad,{[],[]});
probe(filter_not_fun_bad_queue) -> queue:filter(bad,bad);
probe(filter_tuple_fun_empty) -> queue:filter({bad,fun_value},{[],[]});
probe(filter_tuple_fun_bad_queue) -> queue:filter({bad,fun_value},bad);
probe(filter_arity0_empty) -> queue:filter(fun queue_all368_support:cb_zero/0,{[],[]});
probe(filter_arity0_bad_queue) -> queue:filter(fun queue_all368_support:cb_zero/0,bad);
probe(filter_wrong_arity_empty) -> queue:filter(fun queue_all368_support:cb_fold/2,{[],[]});
probe(filter_wrong_arity_bad_queue) -> queue:filter(fun queue_all368_support:cb_fold/2,bad);
probe(filter_callback_throw) -> queue:filter(fun cb_throw/1,{[4,3],[1,2]});
probe(filter_callback_error) -> queue:filter(fun cb_error/1,{[4,3],[1,2]});
probe(filter_callback_exit) -> queue:filter(fun cb_exit/1,{[4,3],[1,2]});
probe(filter_callback_badfun) -> queue:filter(fun cb_badfun/1,{[4,3],[1,2]});
probe(filter_callback_badarity) -> queue:filter(fun cb_badarity/1,{[4,3],[1,2]});
probe(filter_improper_front) -> queue:filter(fun cb_false/1,{[],[1,2,3|tail]});
probe(filter_improper_rear) -> queue:filter(fun cb_false/1,{[3,2,1|tail],[]});
probe(filter_improper_both) -> queue:filter(fun cb_false/1,{[4,3|rear_tail],[1,2|front_tail]});
probe(filter_improper_front257) -> queue:filter(fun cb_false/1,{[],queue_all368_support:bad_list(257)});
probe(filtermap_empty) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[],[]});
probe(filtermap_front) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[],[1,2,3]});
probe(filtermap_rear) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[3,2,1],[]});
probe(filtermap_balanced) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[4,3],[1,2]});
probe(filtermap_bad) -> queue_all368_support:logged(filtermap,fun cb_true/1,bad);
probe(filtermap_bad_rear) -> queue_all368_support:logged(filtermap,fun cb_true/1,{bad,[1]});
probe(filtermap_bad_front) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[1],bad});
probe(filtermap_not_fun_empty) -> queue:filtermap(bad,{[],[]});
probe(filtermap_not_fun_bad_queue) -> queue:filtermap(bad,bad);
probe(filtermap_tuple_fun_empty) -> queue:filtermap({bad,fun_value},{[],[]});
probe(filtermap_tuple_fun_bad_queue) -> queue:filtermap({bad,fun_value},bad);
probe(filtermap_arity0_empty) -> queue:filtermap(fun queue_all368_support:cb_zero/0,{[],[]});
probe(filtermap_arity0_bad_queue) -> queue:filtermap(fun queue_all368_support:cb_zero/0,bad);
probe(filtermap_wrong_arity_empty) -> queue:filtermap(fun queue_all368_support:cb_fold/2,{[],[]});
probe(filtermap_wrong_arity_bad_queue) -> queue:filtermap(fun queue_all368_support:cb_fold/2,bad);
probe(filtermap_callback_throw) -> queue:filtermap(fun cb_throw/1,{[4,3],[1,2]});
probe(filtermap_callback_error) -> queue:filtermap(fun cb_error/1,{[4,3],[1,2]});
probe(filtermap_callback_exit) -> queue:filtermap(fun cb_exit/1,{[4,3],[1,2]});
probe(filtermap_callback_badfun) -> queue:filtermap(fun cb_badfun/1,{[4,3],[1,2]});
probe(filtermap_callback_badarity) -> queue:filtermap(fun cb_badarity/1,{[4,3],[1,2]});
probe(filtermap_improper_front) -> queue:filtermap(fun cb_false/1,{[],[1,2,3|tail]});
probe(filtermap_improper_rear) -> queue:filtermap(fun cb_false/1,{[3,2,1|tail],[]});
probe(filtermap_improper_both) -> queue:filtermap(fun cb_false/1,{[4,3|rear_tail],[1,2|front_tail]});
probe(filtermap_improper_front257) -> queue:filtermap(fun cb_false/1,{[],queue_all368_support:bad_list(257)});
probe(fold_empty) -> queue_all368_support:logged_fold(fun cb_fold/2,[],{[],[]});
probe(fold_front) -> queue_all368_support:logged_fold(fun cb_fold/2,[],{[],[1,2,3]});
probe(fold_rear) -> queue_all368_support:logged_fold(fun cb_fold/2,[],{[3,2,1],[]});
probe(fold_balanced) -> queue_all368_support:logged_fold(fun cb_fold/2,[],{[4,3],[1,2]});
probe(fold_bad) -> queue_all368_support:logged_fold(fun cb_fold/2,[],bad);
probe(fold_bad_rear) -> queue_all368_support:logged_fold(fun cb_fold/2,[],{bad,[1]});
probe(fold_bad_front) -> queue_all368_support:logged_fold(fun cb_fold/2,[],{[1],bad});
probe(fold_not_fun_empty) -> queue:fold(bad,seed,{[],[]});
probe(fold_not_fun_bad_queue) -> queue:fold(bad,seed,bad);
probe(fold_tuple_fun_empty) -> queue:fold({bad,fun_value},seed,{[],[]});
probe(fold_tuple_fun_bad_queue) -> queue:fold({bad,fun_value},seed,bad);
probe(fold_arity0_empty) -> queue:fold(fun queue_all368_support:cb_zero/0,seed,{[],[]});
probe(fold_arity0_bad_queue) -> queue:fold(fun queue_all368_support:cb_zero/0,seed,bad);
probe(fold_wrong_arity_empty) -> queue:fold(fun queue_all368_support:cb_true/1,seed,{[],[]});
probe(fold_wrong_arity_bad_queue) -> queue:fold(fun queue_all368_support:cb_true/1,seed,bad);
probe(fold_callback_throw) -> queue:fold(fun cb_fold_throw/2,seed,{[4,3],[1,2]});
probe(fold_callback_error) -> queue:fold(fun cb_fold_error/2,seed,{[4,3],[1,2]});
probe(fold_callback_exit) -> queue:fold(fun cb_fold_exit/2,seed,{[4,3],[1,2]});
probe(fold_improper_front) -> queue:fold(fun cb_fold/2,[],{[],[1,2,3|tail]});
probe(fold_improper_rear) -> queue:fold(fun cb_fold/2,[],{[3,2,1|tail],[]});
probe(fold_improper_both) -> queue:fold(fun cb_fold/2,[],{[4,3|rear_tail],[1,2|front_tail]});
probe(fold_improper_front257) -> queue:fold(fun cb_fold/2,[],{[],queue_all368_support:bad_list(257)});
probe(any_empty) -> queue_all368_support:logged(any,fun cb_true/1,{[],[]});
probe(any_front) -> queue_all368_support:logged(any,fun cb_true/1,{[],[1,2,3]});
probe(any_rear) -> queue_all368_support:logged(any,fun cb_true/1,{[3,2,1],[]});
probe(any_balanced) -> queue_all368_support:logged(any,fun cb_true/1,{[4,3],[1,2]});
probe(any_bad) -> queue_all368_support:logged(any,fun cb_true/1,bad);
probe(any_bad_rear) -> queue_all368_support:logged(any,fun cb_true/1,{bad,[1]});
probe(any_bad_front) -> queue_all368_support:logged(any,fun cb_true/1,{[1],bad});
probe(any_not_fun_empty) -> queue:any(bad,{[],[]});
probe(any_not_fun_bad_queue) -> queue:any(bad,bad);
probe(any_tuple_fun_empty) -> queue:any({bad,fun_value},{[],[]});
probe(any_tuple_fun_bad_queue) -> queue:any({bad,fun_value},bad);
probe(any_arity0_empty) -> queue:any(fun queue_all368_support:cb_zero/0,{[],[]});
probe(any_arity0_bad_queue) -> queue:any(fun queue_all368_support:cb_zero/0,bad);
probe(any_wrong_arity_empty) -> queue:any(fun queue_all368_support:cb_fold/2,{[],[]});
probe(any_wrong_arity_bad_queue) -> queue:any(fun queue_all368_support:cb_fold/2,bad);
probe(any_callback_throw) -> queue:any(fun cb_throw/1,{[4,3],[1,2]});
probe(any_callback_error) -> queue:any(fun cb_error/1,{[4,3],[1,2]});
probe(any_callback_exit) -> queue:any(fun cb_exit/1,{[4,3],[1,2]});
probe(any_callback_badfun) -> queue:any(fun cb_badfun/1,{[4,3],[1,2]});
probe(any_callback_badarity) -> queue:any(fun cb_badarity/1,{[4,3],[1,2]});
probe(any_improper_front) -> queue:any(fun cb_false/1,{[],[1,2,3|tail]});
probe(any_improper_rear) -> queue:any(fun cb_false/1,{[3,2,1|tail],[]});
probe(any_improper_both) -> queue:any(fun cb_false/1,{[4,3|rear_tail],[1,2|front_tail]});
probe(any_improper_front257) -> queue:any(fun cb_false/1,{[],queue_all368_support:bad_list(257)});
probe(all_empty) -> queue_all368_support:logged(all,fun cb_true/1,{[],[]});
probe(all_front) -> queue_all368_support:logged(all,fun cb_true/1,{[],[1,2,3]});
probe(all_rear) -> queue_all368_support:logged(all,fun cb_true/1,{[3,2,1],[]});
probe(all_balanced) -> queue_all368_support:logged(all,fun cb_true/1,{[4,3],[1,2]});
probe(all_bad) -> queue_all368_support:logged(all,fun cb_true/1,bad);
probe(all_bad_rear) -> queue_all368_support:logged(all,fun cb_true/1,{bad,[1]});
probe(all_bad_front) -> queue_all368_support:logged(all,fun cb_true/1,{[1],bad});
probe(all_not_fun_empty) -> queue:all(bad,{[],[]});
probe(all_not_fun_bad_queue) -> queue:all(bad,bad);
probe(all_tuple_fun_empty) -> queue:all({bad,fun_value},{[],[]});
probe(all_tuple_fun_bad_queue) -> queue:all({bad,fun_value},bad);
probe(all_arity0_empty) -> queue:all(fun queue_all368_support:cb_zero/0,{[],[]});
probe(all_arity0_bad_queue) -> queue:all(fun queue_all368_support:cb_zero/0,bad);
probe(all_wrong_arity_empty) -> queue:all(fun queue_all368_support:cb_fold/2,{[],[]});
probe(all_wrong_arity_bad_queue) -> queue:all(fun queue_all368_support:cb_fold/2,bad);
probe(all_callback_throw) -> queue:all(fun cb_throw/1,{[4,3],[1,2]});
probe(all_callback_error) -> queue:all(fun cb_error/1,{[4,3],[1,2]});
probe(all_callback_exit) -> queue:all(fun cb_exit/1,{[4,3],[1,2]});
probe(all_callback_badfun) -> queue:all(fun cb_badfun/1,{[4,3],[1,2]});
probe(all_callback_badarity) -> queue:all(fun cb_badarity/1,{[4,3],[1,2]});
probe(all_improper_front) -> queue:all(fun cb_false/1,{[],[1,2,3|tail]});
probe(all_improper_rear) -> queue:all(fun cb_false/1,{[3,2,1|tail],[]});
probe(all_improper_both) -> queue:all(fun cb_false/1,{[4,3|rear_tail],[1,2|front_tail]});
probe(all_improper_front257) -> queue:all(fun cb_false/1,{[],queue_all368_support:bad_list(257)});
probe(delete_with_empty) -> queue_all368_support:logged(delete_with,fun cb_true/1,{[],[]});
probe(delete_with_front) -> queue_all368_support:logged(delete_with,fun cb_true/1,{[],[1,2,3]});
probe(delete_with_rear) -> queue_all368_support:logged(delete_with,fun cb_true/1,{[3,2,1],[]});
probe(delete_with_balanced) -> queue_all368_support:logged(delete_with,fun cb_true/1,{[4,3],[1,2]});
probe(delete_with_bad) -> queue_all368_support:logged(delete_with,fun cb_true/1,bad);
probe(delete_with_bad_rear) -> queue_all368_support:logged(delete_with,fun cb_true/1,{bad,[1]});
probe(delete_with_bad_front) -> queue_all368_support:logged(delete_with,fun cb_true/1,{[1],bad});
probe(delete_with_not_fun_empty) -> queue:delete_with(bad,{[],[]});
probe(delete_with_not_fun_bad_queue) -> queue:delete_with(bad,bad);
probe(delete_with_tuple_fun_empty) -> queue:delete_with({bad,fun_value},{[],[]});
probe(delete_with_tuple_fun_bad_queue) -> queue:delete_with({bad,fun_value},bad);
probe(delete_with_arity0_empty) -> queue:delete_with(fun queue_all368_support:cb_zero/0,{[],[]});
probe(delete_with_arity0_bad_queue) -> queue:delete_with(fun queue_all368_support:cb_zero/0,bad);
probe(delete_with_wrong_arity_empty) -> queue:delete_with(fun queue_all368_support:cb_fold/2,{[],[]});
probe(delete_with_wrong_arity_bad_queue) -> queue:delete_with(fun queue_all368_support:cb_fold/2,bad);
probe(delete_with_callback_throw) -> queue:delete_with(fun cb_throw/1,{[4,3],[1,2]});
probe(delete_with_callback_error) -> queue:delete_with(fun cb_error/1,{[4,3],[1,2]});
probe(delete_with_callback_exit) -> queue:delete_with(fun cb_exit/1,{[4,3],[1,2]});
probe(delete_with_callback_badfun) -> queue:delete_with(fun cb_badfun/1,{[4,3],[1,2]});
probe(delete_with_callback_badarity) -> queue:delete_with(fun cb_badarity/1,{[4,3],[1,2]});
probe(delete_with_improper_front) -> queue:delete_with(fun cb_false/1,{[],[1,2,3|tail]});
probe(delete_with_improper_rear) -> queue:delete_with(fun cb_false/1,{[3,2,1|tail],[]});
probe(delete_with_improper_both) -> queue:delete_with(fun cb_false/1,{[4,3|rear_tail],[1,2|front_tail]});
probe(delete_with_improper_front257) -> queue:delete_with(fun cb_false/1,{[],queue_all368_support:bad_list(257)});
probe(delete_with_r_empty) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{[],[]});
probe(delete_with_r_front) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{[],[1,2,3]});
probe(delete_with_r_rear) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{[3,2,1],[]});
probe(delete_with_r_balanced) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{[4,3],[1,2]});
probe(delete_with_r_bad) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,bad);
probe(delete_with_r_bad_rear) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{bad,[1]});
probe(delete_with_r_bad_front) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{[1],bad});
probe(delete_with_r_not_fun_empty) -> queue:delete_with_r(bad,{[],[]});
probe(delete_with_r_not_fun_bad_queue) -> queue:delete_with_r(bad,bad);
probe(delete_with_r_tuple_fun_empty) -> queue:delete_with_r({bad,fun_value},{[],[]});
probe(delete_with_r_tuple_fun_bad_queue) -> queue:delete_with_r({bad,fun_value},bad);
probe(delete_with_r_arity0_empty) -> queue:delete_with_r(fun queue_all368_support:cb_zero/0,{[],[]});
probe(delete_with_r_arity0_bad_queue) -> queue:delete_with_r(fun queue_all368_support:cb_zero/0,bad);
probe(delete_with_r_wrong_arity_empty) -> queue:delete_with_r(fun queue_all368_support:cb_fold/2,{[],[]});
probe(delete_with_r_wrong_arity_bad_queue) -> queue:delete_with_r(fun queue_all368_support:cb_fold/2,bad);
probe(delete_with_r_callback_throw) -> queue:delete_with_r(fun cb_throw/1,{[4,3],[1,2]});
probe(delete_with_r_callback_error) -> queue:delete_with_r(fun cb_error/1,{[4,3],[1,2]});
probe(delete_with_r_callback_exit) -> queue:delete_with_r(fun cb_exit/1,{[4,3],[1,2]});
probe(delete_with_r_callback_badfun) -> queue:delete_with_r(fun cb_badfun/1,{[4,3],[1,2]});
probe(delete_with_r_callback_badarity) -> queue:delete_with_r(fun cb_badarity/1,{[4,3],[1,2]});
probe(delete_with_r_improper_front) -> queue:delete_with_r(fun cb_false/1,{[],[1,2,3|tail]});
probe(delete_with_r_improper_rear) -> queue:delete_with_r(fun cb_false/1,{[3,2,1|tail],[]});
probe(delete_with_r_improper_both) -> queue:delete_with_r(fun cb_false/1,{[4,3|rear_tail],[1,2|front_tail]});
probe(delete_with_r_improper_front257) -> queue:delete_with_r(fun cb_false/1,{[],queue_all368_support:bad_list(257)});
probe(filter_result_true_front) -> queue_all368_support:logged(filter,fun cb_true/1,{[],[1,2,3]});
probe(filter_result_true_rear) -> queue_all368_support:logged(filter,fun cb_true/1,{[3,2,1],[]});
probe(filter_result_false_front) -> queue_all368_support:logged(filter,fun cb_false/1,{[],[1,2,3]});
probe(filter_result_false_rear) -> queue_all368_support:logged(filter,fun cb_false/1,{[3,2,1],[]});
probe(filter_result_select_front) -> queue_all368_support:logged(filter,fun cb_select/1,{[],[1,2,3]});
probe(filter_result_select_rear) -> queue_all368_support:logged(filter,fun cb_select/1,{[3,2,1],[]});
probe(filter_result_replace_front) -> queue_all368_support:logged(filter,fun cb_replace/1,{[],[1,2,3]});
probe(filter_result_replace_rear) -> queue_all368_support:logged(filter,fun cb_replace/1,{[3,2,1],[]});
probe(filter_result_empty_front) -> queue_all368_support:logged(filter,fun cb_empty/1,{[],[1,2,3]});
probe(filter_result_empty_rear) -> queue_all368_support:logged(filter,fun cb_empty/1,{[3,2,1],[]});
probe(filter_result_one_front) -> queue_all368_support:logged(filter,fun cb_one/1,{[],[1,2,3]});
probe(filter_result_one_rear) -> queue_all368_support:logged(filter,fun cb_one/1,{[3,2,1],[]});
probe(filter_result_many_front) -> queue_all368_support:logged(filter,fun cb_many/1,{[],[1,2,3]});
probe(filter_result_many_rear) -> queue_all368_support:logged(filter,fun cb_many/1,{[3,2,1],[]});
probe(filter_result_improper_front) -> queue_all368_support:logged(filter,fun cb_improper/1,{[],[1,2,3]});
probe(filter_result_improper_rear) -> queue_all368_support:logged(filter,fun cb_improper/1,{[3,2,1],[]});
probe(filter_result_bad_front) -> queue_all368_support:logged(filter,fun cb_bad/1,{[],[1,2,3]});
probe(filter_result_bad_rear) -> queue_all368_support:logged(filter,fun cb_bad/1,{[3,2,1],[]});
probe(filter_result_error_later_front) -> queue_all368_support:logged(filter,fun cb_error_later/1,{[],[1,2,3]});
probe(filter_result_error_later_rear) -> queue_all368_support:logged(filter,fun cb_error_later/1,{[3,2,1],[]});
probe(filtermap_result_true_front) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[],[1,2,3]});
probe(filtermap_result_true_rear) -> queue_all368_support:logged(filtermap,fun cb_true/1,{[3,2,1],[]});
probe(filtermap_result_false_front) -> queue_all368_support:logged(filtermap,fun cb_false/1,{[],[1,2,3]});
probe(filtermap_result_false_rear) -> queue_all368_support:logged(filtermap,fun cb_false/1,{[3,2,1],[]});
probe(filtermap_result_select_front) -> queue_all368_support:logged(filtermap,fun cb_select/1,{[],[1,2,3]});
probe(filtermap_result_select_rear) -> queue_all368_support:logged(filtermap,fun cb_select/1,{[3,2,1],[]});
probe(filtermap_result_replace_front) -> queue_all368_support:logged(filtermap,fun cb_replace/1,{[],[1,2,3]});
probe(filtermap_result_replace_rear) -> queue_all368_support:logged(filtermap,fun cb_replace/1,{[3,2,1],[]});
probe(filtermap_result_empty_front) -> queue_all368_support:logged(filtermap,fun cb_empty/1,{[],[1,2,3]});
probe(filtermap_result_empty_rear) -> queue_all368_support:logged(filtermap,fun cb_empty/1,{[3,2,1],[]});
probe(filtermap_result_one_front) -> queue_all368_support:logged(filtermap,fun cb_one/1,{[],[1,2,3]});
probe(filtermap_result_one_rear) -> queue_all368_support:logged(filtermap,fun cb_one/1,{[3,2,1],[]});
probe(filtermap_result_many_front) -> queue_all368_support:logged(filtermap,fun cb_many/1,{[],[1,2,3]});
probe(filtermap_result_many_rear) -> queue_all368_support:logged(filtermap,fun cb_many/1,{[3,2,1],[]});
probe(filtermap_result_improper_front) -> queue_all368_support:logged(filtermap,fun cb_improper/1,{[],[1,2,3]});
probe(filtermap_result_improper_rear) -> queue_all368_support:logged(filtermap,fun cb_improper/1,{[3,2,1],[]});
probe(filtermap_result_bad_front) -> queue_all368_support:logged(filtermap,fun cb_bad/1,{[],[1,2,3]});
probe(filtermap_result_bad_rear) -> queue_all368_support:logged(filtermap,fun cb_bad/1,{[3,2,1],[]});
probe(filtermap_result_error_later_front) -> queue_all368_support:logged(filtermap,fun cb_error_later/1,{[],[1,2,3]});
probe(filtermap_result_error_later_rear) -> queue_all368_support:logged(filtermap,fun cb_error_later/1,{[3,2,1],[]});
probe(any_predicate_true) -> queue_all368_support:logged(any,fun cb_true/1,{[4,3],[1,2]});
probe(any_predicate_false) -> queue_all368_support:logged(any,fun cb_false/1,{[4,3],[1,2]});
probe(any_predicate_select) -> queue_all368_support:logged(any,fun cb_select/1,{[4,3],[1,2]});
probe(any_predicate_bad) -> queue_all368_support:logged(any,fun cb_bad/1,{[4,3],[1,2]});
probe(any_predicate_error_later) -> queue_all368_support:logged(any,fun cb_error_later/1,{[4,3],[1,2]});
probe(any_sentinel_false_tail_first) -> queue:any(fun cb_is_x/1,{[],[x|false]});
probe(any_sentinel_false_tail_later) -> queue:any(fun cb_is_x/1,{[],[prefix,x|false]});
probe(any_sentinel_other_tail) -> queue:any(fun cb_is_x/1,{[],[x|tail]});
probe(any_sentinel_rear_improper) -> queue:any(fun cb_is_x/1,{[x|tail],[]});
probe(all_predicate_true) -> queue_all368_support:logged(all,fun cb_true/1,{[4,3],[1,2]});
probe(all_predicate_false) -> queue_all368_support:logged(all,fun cb_false/1,{[4,3],[1,2]});
probe(all_predicate_select) -> queue_all368_support:logged(all,fun cb_select/1,{[4,3],[1,2]});
probe(all_predicate_bad) -> queue_all368_support:logged(all,fun cb_bad/1,{[4,3],[1,2]});
probe(all_predicate_error_later) -> queue_all368_support:logged(all,fun cb_error_later/1,{[4,3],[1,2]});
probe(all_sentinel_false_tail_first) -> queue:all(fun cb_is_x/1,{[],[x|false]});
probe(all_sentinel_false_tail_later) -> queue:all(fun cb_is_x/1,{[],[prefix,x|false]});
probe(all_sentinel_other_tail) -> queue:all(fun cb_is_x/1,{[],[x|tail]});
probe(all_sentinel_rear_improper) -> queue:all(fun cb_is_x/1,{[x|tail],[]});
probe(delete_with_predicate_true) -> queue_all368_support:logged(delete_with,fun cb_true/1,{[4,3],[1,2]});
probe(delete_with_predicate_false) -> queue_all368_support:logged(delete_with,fun cb_false/1,{[4,3],[1,2]});
probe(delete_with_predicate_select) -> queue_all368_support:logged(delete_with,fun cb_select/1,{[4,3],[1,2]});
probe(delete_with_predicate_bad) -> queue_all368_support:logged(delete_with,fun cb_bad/1,{[4,3],[1,2]});
probe(delete_with_predicate_error_later) -> queue_all368_support:logged(delete_with,fun cb_error_later/1,{[4,3],[1,2]});
probe(delete_with_sentinel_false_tail_first) -> queue:delete_with(fun cb_is_x/1,{[],[x|false]});
probe(delete_with_sentinel_false_tail_later) -> queue:delete_with(fun cb_is_x/1,{[],[prefix,x|false]});
probe(delete_with_sentinel_other_tail) -> queue:delete_with(fun cb_is_x/1,{[],[x|tail]});
probe(delete_with_sentinel_rear_improper) -> queue:delete_with(fun cb_is_x/1,{[x|tail],[]});
probe(delete_with_r_predicate_true) -> queue_all368_support:logged(delete_with_r,fun cb_true/1,{[4,3],[1,2]});
probe(delete_with_r_predicate_false) -> queue_all368_support:logged(delete_with_r,fun cb_false/1,{[4,3],[1,2]});
probe(delete_with_r_predicate_select) -> queue_all368_support:logged(delete_with_r,fun cb_select/1,{[4,3],[1,2]});
probe(delete_with_r_predicate_bad) -> queue_all368_support:logged(delete_with_r,fun cb_bad/1,{[4,3],[1,2]});
probe(delete_with_r_predicate_error_later) -> queue_all368_support:logged(delete_with_r,fun cb_error_later/1,{[4,3],[1,2]});
probe(delete_with_r_sentinel_false_tail_first) -> queue:delete_with_r(fun cb_is_x/1,{[],[x|false]});
probe(delete_with_r_sentinel_false_tail_later) -> queue:delete_with_r(fun cb_is_x/1,{[],[prefix,x|false]});
probe(delete_with_r_sentinel_other_tail) -> queue:delete_with_r(fun cb_is_x/1,{[],[x|tail]});
probe(delete_with_r_sentinel_rear_improper) -> queue:delete_with_r(fun cb_is_x/1,{[x|tail],[]});
probe(any_shortcircuit_front_improper) -> queue:any(fun cb_true/1,{[],[1|tail]});
probe(any_shortcircuit_bad_rear_skipped) -> queue:any(fun cb_true/1,{[2|tail],[1]});
probe(all_shortcircuit_front_improper) -> queue:all(fun cb_false/1,{[],[1|tail]});
probe(all_shortcircuit_bad_rear_skipped) -> queue:all(fun cb_false/1,{[2|tail],[1]});
probe(fold_accumulator_empty_tuple) -> queue:fold(fun cb_fold/2,{seed,1},{[],[]});
probe(fold_accumulator_empty_map) -> queue:fold(fun cb_fold/2,#{seed=>1},{[],[]});
probe(fold_accumulator_nonempty_atom) -> queue:fold(fun cb_fold/2,seed,{[3],[1,2]});
probe(filter_caller_bad) -> queue_all368_support:caller(queue,filter,[fun cb_true/1,bad]);
probe(filtermap_caller_bad) -> queue_all368_support:caller(queue,filtermap,[fun cb_true/1,bad]);
probe(fold_caller_bad) -> queue_all368_support:caller(queue,fold,[fun cb_fold/2,seed,bad]);
probe(any_caller_bad) -> queue_all368_support:caller(queue,any,[fun cb_true/1,bad]);
probe(all_caller_bad) -> queue_all368_support:caller(queue,all,[fun cb_true/1,bad]);
probe(delete_with_caller_bad) -> queue_all368_support:caller(queue,delete_with,[fun cb_true/1,bad]);
probe(delete_with_r_caller_bad) -> queue_all368_support:caller(queue,delete_with_r,[fun cb_true/1,bad]).
cb_true(Value) -> queue_all368_support:cb_true(Value).
cb_false(Value) -> queue_all368_support:cb_false(Value).
cb_select(Value) -> queue_all368_support:cb_select(Value).
cb_replace(Value) -> queue_all368_support:cb_replace(Value).
cb_empty(Value) -> queue_all368_support:cb_empty(Value).
cb_one(Value) -> queue_all368_support:cb_one(Value).
cb_many(Value) -> queue_all368_support:cb_many(Value).
cb_improper(Value) -> queue_all368_support:cb_improper(Value).
cb_bad(Value) -> queue_all368_support:cb_bad(Value).
cb_throw(Value) -> queue_all368_support:cb_throw(Value).
cb_error(Value) -> queue_all368_support:cb_error(Value).
cb_exit(Value) -> queue_all368_support:cb_exit(Value).
cb_error_later(Value) -> queue_all368_support:cb_error_later(Value).
cb_badfun(Value) -> queue_all368_support:cb_badfun(Value).
cb_badarity(Value) -> queue_all368_support:invoke(fun cb_zero/0,[Value]).
cb_is_x(Value) -> queue_all368_support:cb_is_x(Value).
cb_zero() -> zero.
cb_fold(Value,Accumulator) -> queue_all368_support:cb_fold(Value,Accumulator).
cb_fold_throw(Value,Accumulator) -> queue_all368_support:cb_fold_throw(Value,Accumulator).
cb_fold_error(Value,Accumulator) -> queue_all368_support:cb_fold_error(Value,Accumulator).
cb_fold_exit(Value,Accumulator) -> queue_all368_support:cb_fold_exit(Value,Accumulator).
