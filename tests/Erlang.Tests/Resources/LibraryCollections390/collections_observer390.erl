-module(collections_observer390).
-export([run/0, run/1, run_stacks/0, run_stacks/1, worker/2]).

run() -> run(all).

run(Module) ->
    {Child, Ref} = erlang:spawn_monitor(?MODULE, worker, [self(), Module]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Ref, process, Child, normal} -> Values;
                {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
            after 5000 -> erlang:error(child_not_settled)
            end;
        {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
    after 30000 -> erlang:exit(Child, kill), erlang:error(child_timeout)
    end.

worker(Owner, Module) ->
    Owner ! {self(), rows(cases(Module))},
    ok.

rows([]) -> [];
rows([Case|Rest]) -> [{Case, observe(Case)}|rows(Rest)].

run_stacks() -> run_stacks(all).

run_stacks(Module) ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run(Module)},
        Zero = at_depth(0, Module),
        One = at_depth(1, Module),
        Two = at_depth(2, Module),
        Eight = at_depth(8, Module),
        [Default, Zero, One, Two, Eight]
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth, Module) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run(Module)}.

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

cases(queue) -> [queue_new_0, queue_is_queue_1, queue_is_empty_1, queue_len_1, queue_to_list_1, queue_from_list_1, queue_member_2, queue_in_2, queue_in_r_2, queue_out_1, queue_out_r_1, queue_get_1, queue_get_r_1, queue_peek_1, queue_peek_r_1, queue_drop_1, queue_drop_r_1, queue_reverse_1, queue_join_2, queue_split_2, queue_filter_2, queue_filtermap_2, queue_fold_3, queue_any_2, queue_all_2, queue_delete_2, queue_delete_r_2, queue_delete_with_2, queue_delete_with_r_2, queue_cons_2, queue_head_1, queue_tail_1, queue_snoc_2, queue_last_1, queue_daeh_1, queue_init_1, queue_liat_1, queue_lait_1, queue_failure_malformed, queue_failure_empty, queue_failure_bad_callback, queue_failure_callback_error, queue_failure_improper];
cases(ordsets) -> [ordsets_new_0, ordsets_is_set_1, ordsets_size_1, ordsets_is_empty_1, ordsets_to_list_1, ordsets_from_list_1, ordsets_is_element_2, ordsets_add_element_2, ordsets_del_element_2, ordsets_union_2, ordsets_union_1, ordsets_intersection_2, ordsets_intersection_1, ordsets_is_equal_2, ordsets_is_disjoint_2, ordsets_subtract_2, ordsets_is_subset_2, ordsets_fold_3, ordsets_filter_2, ordsets_map_2, ordsets_filtermap_2, ordsets_failure_malformed, ordsets_failure_bad_callback, ordsets_failure_callback_error, ordsets_failure_improper];
cases(sets) -> [sets_new_0, sets_is_set_1, sets_size_1, sets_is_empty_1, sets_to_list_1, sets_from_list_1, sets_is_element_2, sets_add_element_2, sets_del_element_2, sets_union_2, sets_union_1, sets_intersection_2, sets_intersection_1, sets_is_equal_2, sets_is_disjoint_2, sets_subtract_2, sets_is_subset_2, sets_fold_3, sets_filter_2, sets_map_2, sets_filtermap_2, sets_new_1, sets_from_list_2, sets_failure_malformed, sets_failure_bad_callback, sets_failure_callback_error, sets_failure_improper, sets_failure_version_option];
cases(dict) -> [dict_new_0, dict_is_key_2, dict_to_list_1, dict_from_list_1, dict_size_1, dict_is_empty_1, dict_fetch_2, dict_find_2, dict_fetch_keys_1, dict_erase_2, dict_take_2, dict_store_3, dict_append_3, dict_append_list_3, dict_update_3, dict_update_4, dict_update_counter_3, dict_fold_3, dict_map_2, dict_filter_2, dict_merge_3, dict_failure_missing_fetch, dict_failure_callback_error, dict_failure_bad_callback, dict_failure_improper];
cases(orddict) -> [orddict_new_0, orddict_is_key_2, orddict_to_list_1, orddict_from_list_1, orddict_size_1, orddict_is_empty_1, orddict_fetch_2, orddict_find_2, orddict_fetch_keys_1, orddict_erase_2, orddict_take_2, orddict_store_3, orddict_append_3, orddict_append_list_3, orddict_update_3, orddict_update_4, orddict_update_counter_3, orddict_fold_3, orddict_map_2, orddict_filter_2, orddict_merge_3, orddict_failure_missing_fetch, orddict_failure_callback_error, orddict_failure_bad_callback, orddict_failure_improper];
cases(gb_sets) -> [gb_sets_empty_0, gb_sets_is_empty_1, gb_sets_size_1, gb_sets_singleton_1, gb_sets_is_member_2, gb_sets_insert_2, gb_sets_add_2, gb_sets_delete_2, gb_sets_delete_any_2, gb_sets_balance_1, gb_sets_union_2, gb_sets_union_1, gb_sets_intersection_2, gb_sets_intersection_1, gb_sets_is_equal_2, gb_sets_is_disjoint_2, gb_sets_difference_2, gb_sets_is_subset_2, gb_sets_to_list_1, gb_sets_from_list_1, gb_sets_from_ordset_1, gb_sets_smallest_1, gb_sets_largest_1, gb_sets_take_smallest_1, gb_sets_take_largest_1, gb_sets_smaller_2, gb_sets_larger_2, gb_sets_iterator_1, gb_sets_iterator_2, gb_sets_iterator_from_2, gb_sets_iterator_from_3, gb_sets_next_1, gb_sets_filter_2, gb_sets_fold_3, gb_sets_map_2, gb_sets_filtermap_2, gb_sets_is_set_1, gb_sets_new_0, gb_sets_is_element_2, gb_sets_add_element_2, gb_sets_del_element_2, gb_sets_subtract_2, gb_sets_failure_malformed, gb_sets_failure_bad_callback, gb_sets_failure_callback_error, gb_sets_failure_improper, gb_sets_failure_duplicate_insert, gb_sets_failure_missing_delete, gb_sets_failure_empty, gb_sets_failure_iterator_order];
cases(gb_trees) -> [gb_trees_empty_0, gb_trees_is_empty_1, gb_trees_size_1, gb_trees_lookup_2, gb_trees_get_2, gb_trees_insert_3, gb_trees_update_3, gb_trees_enter_3, gb_trees_delete_2, gb_trees_delete_any_2, gb_trees_balance_1, gb_trees_is_defined_2, gb_trees_keys_1, gb_trees_values_1, gb_trees_to_list_1, gb_trees_from_list_1, gb_trees_from_orddict_1, gb_trees_smallest_1, gb_trees_largest_1, gb_trees_take_2, gb_trees_take_any_2, gb_trees_take_smallest_1, gb_trees_take_largest_1, gb_trees_smaller_2, gb_trees_larger_2, gb_trees_iterator_1, gb_trees_iterator_2, gb_trees_iterator_from_2, gb_trees_iterator_from_3, gb_trees_next_1, gb_trees_map_2, gb_trees_failure_missing_get, gb_trees_failure_duplicate_insert, gb_trees_failure_missing_delete, gb_trees_failure_empty, gb_trees_failure_iterator_order, gb_trees_failure_callback_error];
cases(all) -> [queue_new_0, queue_is_queue_1, queue_is_empty_1, queue_len_1, queue_to_list_1, queue_from_list_1, queue_member_2, queue_in_2, queue_in_r_2, queue_out_1, queue_out_r_1, queue_get_1, queue_get_r_1, queue_peek_1, queue_peek_r_1, queue_drop_1, queue_drop_r_1, queue_reverse_1, queue_join_2, queue_split_2, queue_filter_2, queue_filtermap_2, queue_fold_3, queue_any_2, queue_all_2, queue_delete_2, queue_delete_r_2, queue_delete_with_2, queue_delete_with_r_2, queue_cons_2, queue_head_1, queue_tail_1, queue_snoc_2, queue_last_1, queue_daeh_1, queue_init_1, queue_liat_1, queue_lait_1, queue_failure_malformed, queue_failure_empty, queue_failure_bad_callback, queue_failure_callback_error, queue_failure_improper, ordsets_new_0, ordsets_is_set_1, ordsets_size_1, ordsets_is_empty_1, ordsets_to_list_1, ordsets_from_list_1, ordsets_is_element_2, ordsets_add_element_2, ordsets_del_element_2, ordsets_union_2, ordsets_union_1, ordsets_intersection_2, ordsets_intersection_1, ordsets_is_equal_2, ordsets_is_disjoint_2, ordsets_subtract_2, ordsets_is_subset_2, ordsets_fold_3, ordsets_filter_2, ordsets_map_2, ordsets_filtermap_2, ordsets_failure_malformed, ordsets_failure_bad_callback, ordsets_failure_callback_error, ordsets_failure_improper, sets_new_0, sets_is_set_1, sets_size_1, sets_is_empty_1, sets_to_list_1, sets_from_list_1, sets_is_element_2, sets_add_element_2, sets_del_element_2, sets_union_2, sets_union_1, sets_intersection_2, sets_intersection_1, sets_is_equal_2, sets_is_disjoint_2, sets_subtract_2, sets_is_subset_2, sets_fold_3, sets_filter_2, sets_map_2, sets_filtermap_2, sets_new_1, sets_from_list_2, sets_failure_malformed, sets_failure_bad_callback, sets_failure_callback_error, sets_failure_improper, sets_failure_version_option, dict_new_0, dict_is_key_2, dict_to_list_1, dict_from_list_1, dict_size_1, dict_is_empty_1, dict_fetch_2, dict_find_2, dict_fetch_keys_1, dict_erase_2, dict_take_2, dict_store_3, dict_append_3, dict_append_list_3, dict_update_3, dict_update_4, dict_update_counter_3, dict_fold_3, dict_map_2, dict_filter_2, dict_merge_3, dict_failure_missing_fetch, dict_failure_callback_error, dict_failure_bad_callback, dict_failure_improper, orddict_new_0, orddict_is_key_2, orddict_to_list_1, orddict_from_list_1, orddict_size_1, orddict_is_empty_1, orddict_fetch_2, orddict_find_2, orddict_fetch_keys_1, orddict_erase_2, orddict_take_2, orddict_store_3, orddict_append_3, orddict_append_list_3, orddict_update_3, orddict_update_4, orddict_update_counter_3, orddict_fold_3, orddict_map_2, orddict_filter_2, orddict_merge_3, orddict_failure_missing_fetch, orddict_failure_callback_error, orddict_failure_bad_callback, orddict_failure_improper, gb_sets_empty_0, gb_sets_is_empty_1, gb_sets_size_1, gb_sets_singleton_1, gb_sets_is_member_2, gb_sets_insert_2, gb_sets_add_2, gb_sets_delete_2, gb_sets_delete_any_2, gb_sets_balance_1, gb_sets_union_2, gb_sets_union_1, gb_sets_intersection_2, gb_sets_intersection_1, gb_sets_is_equal_2, gb_sets_is_disjoint_2, gb_sets_difference_2, gb_sets_is_subset_2, gb_sets_to_list_1, gb_sets_from_list_1, gb_sets_from_ordset_1, gb_sets_smallest_1, gb_sets_largest_1, gb_sets_take_smallest_1, gb_sets_take_largest_1, gb_sets_smaller_2, gb_sets_larger_2, gb_sets_iterator_1, gb_sets_iterator_2, gb_sets_iterator_from_2, gb_sets_iterator_from_3, gb_sets_next_1, gb_sets_filter_2, gb_sets_fold_3, gb_sets_map_2, gb_sets_filtermap_2, gb_sets_is_set_1, gb_sets_new_0, gb_sets_is_element_2, gb_sets_add_element_2, gb_sets_del_element_2, gb_sets_subtract_2, gb_sets_failure_malformed, gb_sets_failure_bad_callback, gb_sets_failure_callback_error, gb_sets_failure_improper, gb_sets_failure_duplicate_insert, gb_sets_failure_missing_delete, gb_sets_failure_empty, gb_sets_failure_iterator_order, gb_trees_empty_0, gb_trees_is_empty_1, gb_trees_size_1, gb_trees_lookup_2, gb_trees_get_2, gb_trees_insert_3, gb_trees_update_3, gb_trees_enter_3, gb_trees_delete_2, gb_trees_delete_any_2, gb_trees_balance_1, gb_trees_is_defined_2, gb_trees_keys_1, gb_trees_values_1, gb_trees_to_list_1, gb_trees_from_list_1, gb_trees_from_orddict_1, gb_trees_smallest_1, gb_trees_largest_1, gb_trees_take_2, gb_trees_take_any_2, gb_trees_take_smallest_1, gb_trees_take_largest_1, gb_trees_smaller_2, gb_trees_larger_2, gb_trees_iterator_1, gb_trees_iterator_2, gb_trees_iterator_from_2, gb_trees_iterator_from_3, gb_trees_next_1, gb_trees_map_2, gb_trees_failure_missing_get, gb_trees_failure_duplicate_insert, gb_trees_failure_missing_delete, gb_trees_failure_empty, gb_trees_failure_iterator_order, gb_trees_failure_callback_error].

value(queue_new_0) -> queue:new();
value(queue_is_queue_1) -> queue:is_queue(queue:from_list([1,2,3]));
value(queue_is_empty_1) -> queue:is_empty(queue:from_list([1,2,3]));
value(queue_len_1) -> queue:len(queue:from_list([1,2,3]));
value(queue_to_list_1) -> queue:to_list(queue:from_list([1,2,3]));
value(queue_from_list_1) -> queue:from_list([1,2,3]);
value(queue_member_2) -> queue:member(2,queue:from_list([1,2,3]));
value(queue_in_2) -> queue:in(2,queue:from_list([1,2,3]));
value(queue_in_r_2) -> queue:in_r(2,queue:from_list([1,2,3]));
value(queue_out_1) -> queue:out(queue:from_list([1,2,3]));
value(queue_out_r_1) -> queue:out_r(queue:from_list([1,2,3]));
value(queue_get_1) -> queue:get(queue:from_list([1,2,3]));
value(queue_get_r_1) -> queue:get_r(queue:from_list([1,2,3]));
value(queue_peek_1) -> queue:peek(queue:from_list([1,2,3]));
value(queue_peek_r_1) -> queue:peek_r(queue:from_list([1,2,3]));
value(queue_drop_1) -> queue:drop(queue:from_list([1,2,3]));
value(queue_drop_r_1) -> queue:drop_r(queue:from_list([1,2,3]));
value(queue_reverse_1) -> queue:reverse(queue:from_list([1,2,3]));
value(queue_join_2) -> queue:join(queue:from_list([1,2,3]),queue:from_list([4,5]));
value(queue_split_2) -> queue:split(2,queue:from_list([1,2,3]));
value(queue_filter_2) -> queue:filter(fun(X) -> X rem 2 =:= 0 end,queue:from_list([1,2,3]));
value(queue_filtermap_2) -> queue:filtermap(fun(X) -> {true,X*2} end,queue:from_list([1,2,3]));
value(queue_fold_3) -> queue:fold(fun(X,A) -> X+A end,0,queue:from_list([1,2,3]));
value(queue_any_2) -> queue:any(fun(X) -> X rem 2 =:= 0 end,queue:from_list([1,2,3]));
value(queue_all_2) -> queue:all(fun(X) -> X rem 2 =:= 0 end,queue:from_list([1,2,3]));
value(queue_delete_2) -> queue:delete(2,queue:from_list([1,2,3]));
value(queue_delete_r_2) -> queue:delete_r(2,queue:from_list([1,2,3]));
value(queue_delete_with_2) -> queue:delete_with(fun(X) -> X rem 2 =:= 0 end,queue:from_list([1,2,3]));
value(queue_delete_with_r_2) -> queue:delete_with_r(fun(X) -> X rem 2 =:= 0 end,queue:from_list([1,2,3]));
value(queue_cons_2) -> queue:cons(2,queue:from_list([1,2,3]));
value(queue_head_1) -> queue:head(queue:from_list([1,2,3]));
value(queue_tail_1) -> queue:tail(queue:from_list([1,2,3]));
value(queue_snoc_2) -> queue:snoc(queue:from_list([1,2,3]),4);
value(queue_last_1) -> queue:last(queue:from_list([1,2,3]));
value(queue_daeh_1) -> queue:daeh(queue:from_list([1,2,3]));
value(queue_init_1) -> queue:init(queue:from_list([1,2,3]));
value(queue_liat_1) -> queue:liat(queue:from_list([1,2,3]));
value(queue_lait_1) -> queue:lait(queue:from_list([1,2,3]));
value(queue_failure_malformed) -> queue:out(invalid);
value(queue_failure_empty) -> queue:get(queue:new());
value(queue_failure_bad_callback) -> queue:filter(fun(_) -> invalid_boolean end,queue:from_list([1,2,3]));
value(queue_failure_callback_error) -> queue:fold(fun(_,_) -> erlang:error(callback_failure) end,0,queue:from_list([1,2,3]));
value(queue_failure_improper) -> queue:from_list([1|invalid]);
value(ordsets_new_0) -> ordsets:new();
value(ordsets_is_set_1) -> ordsets:is_set(ordsets:from_list([1,2,3]));
value(ordsets_size_1) -> ordsets:size(ordsets:from_list([1,2,3]));
value(ordsets_is_empty_1) -> ordsets:is_empty(ordsets:from_list([1,2,3]));
value(ordsets_to_list_1) -> ordsets:to_list(ordsets:from_list([1,2,3]));
value(ordsets_from_list_1) -> ordsets:from_list([3,1,2,1]);
value(ordsets_is_element_2) -> ordsets:is_element(2,ordsets:from_list([1,2,3]));
value(ordsets_add_element_2) -> ordsets:add_element(2,ordsets:from_list([1,2,3]));
value(ordsets_del_element_2) -> ordsets:del_element(2,ordsets:from_list([1,2,3]));
value(ordsets_union_2) -> ordsets:union(ordsets:from_list([1,2,3]),ordsets:from_list([2,4]));
value(ordsets_union_1) -> ordsets:union([ordsets:from_list([1,2,3]),ordsets:from_list([2,4])]);
value(ordsets_intersection_2) -> ordsets:intersection(ordsets:from_list([1,2,3]),ordsets:from_list([2,4]));
value(ordsets_intersection_1) -> ordsets:intersection([ordsets:from_list([1,2,3]),ordsets:from_list([2,4])]);
value(ordsets_is_equal_2) -> ordsets:is_equal(ordsets:from_list([1,2,3]),ordsets:from_list([2,4]));
value(ordsets_is_disjoint_2) -> ordsets:is_disjoint(ordsets:from_list([1,2,3]),ordsets:from_list([2,4]));
value(ordsets_subtract_2) -> ordsets:subtract(ordsets:from_list([1,2,3]),ordsets:from_list([2,4]));
value(ordsets_is_subset_2) -> ordsets:is_subset(ordsets:from_list([1,2,3]),ordsets:from_list([2,4]));
value(ordsets_fold_3) -> ordsets:fold(fun(X,A) -> X+A end,0,ordsets:from_list([1,2,3]));
value(ordsets_filter_2) -> ordsets:filter(fun(X) -> X rem 2 =:= 0 end,ordsets:from_list([1,2,3]));
value(ordsets_map_2) -> ordsets:map(fun(X) -> X*2 end,ordsets:from_list([1,2,3]));
value(ordsets_filtermap_2) -> ordsets:filtermap(fun(X) -> {true,X*2} end,ordsets:from_list([1,2,3]));
value(ordsets_failure_malformed) -> ordsets:size(invalid);
value(ordsets_failure_bad_callback) -> ordsets:filter(fun(_) -> invalid_boolean end,ordsets:from_list([1,2,3]));
value(ordsets_failure_callback_error) -> ordsets:fold(fun(_,_) -> erlang:error(callback_failure) end,0,ordsets:from_list([1,2,3]));
value(ordsets_failure_improper) -> ordsets:from_list([1|invalid]);
value(sets_new_0) -> sets:new();
value(sets_is_set_1) -> sets:is_set(sets:from_list([1,2,3]));
value(sets_size_1) -> sets:size(sets:from_list([1,2,3]));
value(sets_is_empty_1) -> sets:is_empty(sets:from_list([1,2,3]));
value(sets_to_list_1) -> sets:to_list(sets:from_list([1,2,3]));
value(sets_from_list_1) -> sets:from_list([3,1,2,1]);
value(sets_is_element_2) -> sets:is_element(2,sets:from_list([1,2,3]));
value(sets_add_element_2) -> sets:add_element(2,sets:from_list([1,2,3]));
value(sets_del_element_2) -> sets:del_element(2,sets:from_list([1,2,3]));
value(sets_union_2) -> sets:union(sets:from_list([1,2,3]),sets:from_list([2,4]));
value(sets_union_1) -> sets:union([sets:from_list([1,2,3]),sets:from_list([2,4])]);
value(sets_intersection_2) -> sets:intersection(sets:from_list([1,2,3]),sets:from_list([2,4]));
value(sets_intersection_1) -> sets:intersection([sets:from_list([1,2,3]),sets:from_list([2,4])]);
value(sets_is_equal_2) -> sets:is_equal(sets:from_list([1,2,3]),sets:from_list([2,4]));
value(sets_is_disjoint_2) -> sets:is_disjoint(sets:from_list([1,2,3]),sets:from_list([2,4]));
value(sets_subtract_2) -> sets:subtract(sets:from_list([1,2,3]),sets:from_list([2,4]));
value(sets_is_subset_2) -> sets:is_subset(sets:from_list([1,2,3]),sets:from_list([2,4]));
value(sets_fold_3) -> sets:fold(fun(X,A) -> X+A end,0,sets:from_list([1,2,3]));
value(sets_filter_2) -> sets:filter(fun(X) -> X rem 2 =:= 0 end,sets:from_list([1,2,3]));
value(sets_map_2) -> sets:map(fun(X) -> X*2 end,sets:from_list([1,2,3]));
value(sets_filtermap_2) -> sets:filtermap(fun(X) -> {true,X*2} end,sets:from_list([1,2,3]));
value(sets_new_1) -> sets:new([{version,1}]);
value(sets_from_list_2) -> sets:from_list([3,1,2,1],[{version,1}]);
value(sets_failure_malformed) -> sets:size(invalid);
value(sets_failure_bad_callback) -> sets:filter(fun(_) -> invalid_boolean end,sets:from_list([1,2,3]));
value(sets_failure_callback_error) -> sets:fold(fun(_,_) -> erlang:error(callback_failure) end,0,sets:from_list([1,2,3]));
value(sets_failure_improper) -> sets:from_list([1|invalid]);
value(sets_failure_version_option) -> sets:new([{version,999}]);
value(dict_new_0) -> dict:new();
value(dict_is_key_2) -> dict:is_key(b,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_to_list_1) -> dict:to_list(dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_from_list_1) -> dict:from_list([{a,1},{b,2},{a,3}]);
value(dict_size_1) -> dict:size(dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_is_empty_1) -> dict:is_empty(dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_fetch_2) -> dict:fetch(b,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_find_2) -> dict:find(b,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_fetch_keys_1) -> dict:fetch_keys(dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_erase_2) -> dict:erase(b,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_take_2) -> dict:take(b,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_store_3) -> dict:store(d,4,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_append_3) -> dict:append(a,2,dict:from_list([{a,[1]}]));
value(dict_append_list_3) -> dict:append_list(a,[2,3],dict:from_list([{a,[1]}]));
value(dict_update_3) -> dict:update(a,fun(X) -> X+10 end,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_update_4) -> dict:update(d,fun(X) -> X+10 end,42,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_update_counter_3) -> dict:update_counter(a,10,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_fold_3) -> dict:fold(fun(_K,V,A) -> V+A end,0,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_map_2) -> dict:map(fun(_K,V) -> V*2 end,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_filter_2) -> dict:filter(fun(_K,V) -> V rem 2 =:= 1 end,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_merge_3) -> dict:merge(fun(_K,V,W) -> V+W end,dict:from_list([{a,1},{b,2},{c,3}]),dict:from_list([{b,10},{d,4}]));
value(dict_failure_missing_fetch) -> dict:fetch(missing,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_failure_callback_error) -> dict:fold(fun(_,_,_) -> erlang:error(callback_failure) end,0,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_failure_bad_callback) -> dict:filter(fun(_,_) -> invalid_boolean end,dict:from_list([{a,1},{b,2},{c,3}]));
value(dict_failure_improper) -> dict:from_list([{a,1}|invalid]);
value(orddict_new_0) -> orddict:new();
value(orddict_is_key_2) -> orddict:is_key(b,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_to_list_1) -> orddict:to_list(orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_from_list_1) -> orddict:from_list([{a,1},{b,2},{a,3}]);
value(orddict_size_1) -> orddict:size(orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_is_empty_1) -> orddict:is_empty(orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_fetch_2) -> orddict:fetch(b,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_find_2) -> orddict:find(b,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_fetch_keys_1) -> orddict:fetch_keys(orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_erase_2) -> orddict:erase(b,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_take_2) -> orddict:take(b,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_store_3) -> orddict:store(d,4,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_append_3) -> orddict:append(a,2,orddict:from_list([{a,[1]}]));
value(orddict_append_list_3) -> orddict:append_list(a,[2,3],orddict:from_list([{a,[1]}]));
value(orddict_update_3) -> orddict:update(a,fun(X) -> X+10 end,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_update_4) -> orddict:update(d,fun(X) -> X+10 end,42,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_update_counter_3) -> orddict:update_counter(a,10,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_fold_3) -> orddict:fold(fun(_K,V,A) -> V+A end,0,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_map_2) -> orddict:map(fun(_K,V) -> V*2 end,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_filter_2) -> orddict:filter(fun(_K,V) -> V rem 2 =:= 1 end,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_merge_3) -> orddict:merge(fun(_K,V,W) -> V+W end,orddict:from_list([{a,1},{b,2},{c,3}]),orddict:from_list([{b,10},{d,4}]));
value(orddict_failure_missing_fetch) -> orddict:fetch(missing,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_failure_callback_error) -> orddict:fold(fun(_,_,_) -> erlang:error(callback_failure) end,0,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_failure_bad_callback) -> orddict:filter(fun(_,_) -> invalid_boolean end,orddict:from_list([{a,1},{b,2},{c,3}]));
value(orddict_failure_improper) -> orddict:from_list([{a,1}|invalid]);
value(gb_sets_empty_0) -> gb_sets:empty();
value(gb_sets_is_empty_1) -> gb_sets:is_empty(gb_sets:from_list([1,2,3]));
value(gb_sets_size_1) -> gb_sets:size(gb_sets:from_list([1,2,3]));
value(gb_sets_singleton_1) -> gb_sets:singleton(7);
value(gb_sets_is_member_2) -> gb_sets:is_member(2,gb_sets:from_list([1,2,3]));
value(gb_sets_insert_2) -> gb_sets:insert(4,gb_sets:from_list([1,2,3]));
value(gb_sets_add_2) -> gb_sets:add(2,gb_sets:from_list([1,2,3]));
value(gb_sets_delete_2) -> gb_sets:delete(2,gb_sets:from_list([1,2,3]));
value(gb_sets_delete_any_2) -> gb_sets:delete_any(2,gb_sets:from_list([1,2,3]));
value(gb_sets_balance_1) -> gb_sets:balance(gb_sets:from_list([1,2,3]));
value(gb_sets_union_2) -> gb_sets:union(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_union_1) -> gb_sets:union([gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4])]);
value(gb_sets_intersection_2) -> gb_sets:intersection(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_intersection_1) -> gb_sets:intersection([gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4])]);
value(gb_sets_is_equal_2) -> gb_sets:is_equal(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_is_disjoint_2) -> gb_sets:is_disjoint(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_difference_2) -> gb_sets:difference(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_is_subset_2) -> gb_sets:is_subset(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_to_list_1) -> gb_sets:to_list(gb_sets:from_list([1,2,3]));
value(gb_sets_from_list_1) -> gb_sets:from_list([3,1,2,1]);
value(gb_sets_from_ordset_1) -> gb_sets:from_ordset([1,2,3]);
value(gb_sets_smallest_1) -> gb_sets:smallest(gb_sets:from_list([1,2,3]));
value(gb_sets_largest_1) -> gb_sets:largest(gb_sets:from_list([1,2,3]));
value(gb_sets_take_smallest_1) -> gb_sets:take_smallest(gb_sets:from_list([1,2,3]));
value(gb_sets_take_largest_1) -> gb_sets:take_largest(gb_sets:from_list([1,2,3]));
value(gb_sets_smaller_2) -> gb_sets:smaller(2,gb_sets:from_list([1,2,3]));
value(gb_sets_larger_2) -> gb_sets:larger(2,gb_sets:from_list([1,2,3]));
value(gb_sets_iterator_1) -> gb_sets:iterator(gb_sets:from_list([1,2,3]));
value(gb_sets_iterator_2) -> gb_sets:iterator(gb_sets:from_list([1,2,3]),reversed);
value(gb_sets_iterator_from_2) -> gb_sets:iterator_from(2,gb_sets:from_list([1,2,3]));
value(gb_sets_iterator_from_3) -> gb_sets:iterator_from(2,gb_sets:from_list([1,2,3]),reversed);
value(gb_sets_next_1) -> gb_sets:next(gb_sets:iterator(gb_sets:from_list([1,2,3])));
value(gb_sets_filter_2) -> gb_sets:filter(fun(X) -> X rem 2 =:= 0 end,gb_sets:from_list([1,2,3]));
value(gb_sets_fold_3) -> gb_sets:fold(fun(X,A) -> X+A end,0,gb_sets:from_list([1,2,3]));
value(gb_sets_map_2) -> gb_sets:map(fun(X) -> X*2 end,gb_sets:from_list([1,2,3]));
value(gb_sets_filtermap_2) -> gb_sets:filtermap(fun(X) -> {true,X*2} end,gb_sets:from_list([1,2,3]));
value(gb_sets_is_set_1) -> gb_sets:is_set(gb_sets:from_list([1,2,3]));
value(gb_sets_new_0) -> gb_sets:new();
value(gb_sets_is_element_2) -> gb_sets:is_element(2,gb_sets:from_list([1,2,3]));
value(gb_sets_add_element_2) -> gb_sets:add_element(2,gb_sets:from_list([1,2,3]));
value(gb_sets_del_element_2) -> gb_sets:del_element(2,gb_sets:from_list([1,2,3]));
value(gb_sets_subtract_2) -> gb_sets:subtract(gb_sets:from_list([1,2,3]),gb_sets:from_list([2,4]));
value(gb_sets_failure_malformed) -> gb_sets:size(invalid);
value(gb_sets_failure_bad_callback) -> gb_sets:filter(fun(_) -> invalid_boolean end,gb_sets:from_list([1,2,3]));
value(gb_sets_failure_callback_error) -> gb_sets:fold(fun(_,_) -> erlang:error(callback_failure) end,0,gb_sets:from_list([1,2,3]));
value(gb_sets_failure_improper) -> gb_sets:from_list([1|invalid]);
value(gb_sets_failure_duplicate_insert) -> gb_sets:insert(2,gb_sets:from_list([1,2,3]));
value(gb_sets_failure_missing_delete) -> gb_sets:delete(9,gb_sets:from_list([1,2,3]));
value(gb_sets_failure_empty) -> gb_sets:smallest(gb_sets:empty());
value(gb_sets_failure_iterator_order) -> gb_sets:iterator(gb_sets:from_list([1,2,3]),invalid);
value(gb_trees_empty_0) -> gb_trees:empty();
value(gb_trees_is_empty_1) -> gb_trees:is_empty(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_size_1) -> gb_trees:size(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_lookup_2) -> gb_trees:lookup(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_get_2) -> gb_trees:get(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_insert_3) -> gb_trees:insert(d,4,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_update_3) -> gb_trees:update(b,20,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_enter_3) -> gb_trees:enter(b,20,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_delete_2) -> gb_trees:delete(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_delete_any_2) -> gb_trees:delete_any(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_balance_1) -> gb_trees:balance(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_is_defined_2) -> gb_trees:is_defined(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_keys_1) -> gb_trees:keys(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_values_1) -> gb_trees:values(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_to_list_1) -> gb_trees:to_list(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_from_list_1) -> gb_trees:from_list([{c,3},{a,1},{b,2}]);
value(gb_trees_from_orddict_1) -> gb_trees:from_orddict([{a,1},{b,2},{c,3}]);
value(gb_trees_smallest_1) -> gb_trees:smallest(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_largest_1) -> gb_trees:largest(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_take_2) -> gb_trees:take(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_take_any_2) -> gb_trees:take_any(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_take_smallest_1) -> gb_trees:take_smallest(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_take_largest_1) -> gb_trees:take_largest(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_smaller_2) -> gb_trees:smaller(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_larger_2) -> gb_trees:larger(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_iterator_1) -> gb_trees:iterator(gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_iterator_2) -> gb_trees:iterator(gb_trees:from_orddict([{a,1},{b,2},{c,3}]),reversed);
value(gb_trees_iterator_from_2) -> gb_trees:iterator_from(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_iterator_from_3) -> gb_trees:iterator_from(b,gb_trees:from_orddict([{a,1},{b,2},{c,3}]),reversed);
value(gb_trees_next_1) -> gb_trees:next(gb_trees:iterator(gb_trees:from_orddict([{a,1},{b,2},{c,3}])));
value(gb_trees_map_2) -> gb_trees:map(fun(_K,V) -> V*2 end,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_failure_missing_get) -> gb_trees:get(missing,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_failure_duplicate_insert) -> gb_trees:insert(b,4,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_failure_missing_delete) -> gb_trees:delete(missing,gb_trees:from_orddict([{a,1},{b,2},{c,3}]));
value(gb_trees_failure_empty) -> gb_trees:smallest(gb_trees:empty());
value(gb_trees_failure_iterator_order) -> gb_trees:iterator(gb_trees:from_orddict([{a,1},{b,2},{c,3}]),invalid);
value(gb_trees_failure_callback_error) -> gb_trees:map(fun(_,_) -> erlang:error(callback_failure) end,gb_trees:from_orddict([{a,1},{b,2},{c,3}])).
