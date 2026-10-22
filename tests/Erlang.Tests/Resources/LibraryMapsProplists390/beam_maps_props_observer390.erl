-module(beam_maps_props_observer390).
-export([cases/0, run/0, run_stacks/0]).

cases() ->
    [maps_get2,
     maps_get3,
     maps_find,
     maps_from_list,
     maps_from_keys,
     maps_is_key,
     maps_keys,
     maps_merge,
     maps_put,
     maps_remove,
     maps_take,
     maps_to_list,
     maps_update,
     maps_values,
     maps_new,
     maps_size,
     maps_filter,
     maps_filtermap,
     maps_fold,
     maps_foreach,
     maps_map,
     maps_update_with3,
     maps_update_with4,
     maps_without,
     maps_with,
     maps_iterator1,
     maps_iterator2,
     maps_next,
     maps_intersect,
     maps_intersect_with,
     maps_merge_with,
     maps_groups2,
     maps_groups3,
     maps_is_iterator_valid,
     props_property1,
     props_property2,
     props_unfold,
     props_compact,
     props_lookup,
     props_lookup_all,
     props_is_defined,
     props_get_value2,
     props_get_value3,
     props_get_all_values,
     props_append_values,
     props_get_bool,
     props_get_keys,
     props_delete,
     props_aliases,
     props_negations,
     props_expand,
     props_normalize,
     props_split,
     props_from_map,
     props_to_map1,
     props_to_map2,
     maps_get2_missing,
     maps_update_missing,
     maps_take_missing,
     maps_find_missing,
     maps_from_list_spine,
     maps_from_list_element,
     maps_from_keys_spine,
     maps_with_spine,
     maps_without_spine,
     maps_iterator_order,
     maps_next_shape,
     maps_next_order_missing,
     maps_next_order_spine,
     maps_next_offset_large,
     maps_next_offset_negative,
     maps_next_offset_one,
     maps_next_none,
     maps_next_tuple,
     maps_iterator_false,
     maps_next_hamt_zero,
     maps_next_hamt_one,
     maps_next_hamt_big,
     maps_iterator_reverse,
     maps_iterator_compare,
     maps_to_list_ordered,
     maps_update_with_missing,
     maps_groups2_spine,
     maps_groups3_spine,
     maps_badmap_get2,
     maps_badmap_get3,
     maps_badmap_find2,
     maps_badmap_is_key2,
     maps_badmap_keys1,
     maps_badmap_merge2,
     maps_badmap_put3,
     maps_badmap_remove2,
     maps_badmap_take2,
     maps_badmap_to_list1,
     maps_badmap_update3,
     maps_badmap_values1,
     maps_badmap_size1,
     maps_badmap_iterator1,
     maps_badmap_with2,
     maps_badmap_without2,
     maps_badmap_intersect2,
     maps_badmap_intersect_with3,
     maps_badmap_merge_with3,
     maps_callback_filter2,
     maps_callback_filtermap2,
     maps_callback_fold3,
     maps_callback_foreach2,
     maps_callback_map2,
     maps_callback_update_with3,
     maps_callback_update_with4,
     maps_callback_groups_from_list2,
     maps_callback_groups_from_list3,
     maps_callback_intersect_with3,
     maps_callback_merge_with3,
     maps_badfun_filter2,
     maps_badfun_filtermap2,
     maps_badfun_foreach2,
     maps_badfun_map2,
     maps_badfun_fold3,
     maps_badfun_update_with3,
     maps_badfun_update_with4,
     maps_badfun_groups_from_list2,
     maps_badfun_groups_from_list3,
     props_boundary_unfold1,
     props_boundary_compact1,
     props_boundary_lookup2,
     props_boundary_lookup_all2,
     props_boundary_is_defined2,
     props_boundary_get_value2,
     props_boundary_get_value3,
     props_boundary_get_all_values2,
     props_boundary_append_values2,
     props_boundary_get_bool2,
     props_boundary_get_keys1,
     props_boundary_delete2,
     props_boundary_substitute_aliases2,
     props_boundary_substitute_negations2,
     props_boundary_expand2,
     props_boundary_normalize2,
     props_boundary_split2,
     props_boundary_from_map1,
     props_boundary_to_map1,
     props_boundary_to_map2,
     primitive_list_mode,
     primitive_improper_accumulator,
     primitive_list_mode_ordered_rejected,
     primitive_hamt_list_initial,
     primitive_hamt_list_one,
     primitive_iterator_mode,
     primitive_ordered_mode,
     primitive_bad_accumulator,
     primitive_bad_map,
     primitive_ordered_improper,
     primitive_ordered_missing,
     primitive_flat_offset,
     primitive_flat_end,
     primitive_flat_negative,
     primitive_flat_too_large,
     primitive_hamt_initial,
     primitive_hamt_one,
     primitive_hamt_big,
     primitive_cmp_numeric,
     primitive_cmp_reverse_numeric,
     primitive_cmp_equal,
     primitive_cmp_nested].

run() -> rows(cases()).

rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, Old, run()},
        Zero = at_depth(0),
        One = at_depth(1),
        Two = at_depth(2),
        Eight = at_depth(8),
        [Default, Zero, One, Two, Eight]
    after
        erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    Effective = erlang:system_flag(backtrace_depth, Depth),
    {Depth, Effective, run()}.

observe(Case) ->
    {Child, Monitor} = erlang:spawn_monitor(beam_maps_props_subject390, worker, [self(), Case]),
    receive
        {Case, Outcome} -> wait_down(Child, Monitor, Case, Outcome);
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_without_report, Case, Reason})
    after 30000 ->
        abort(Child, Monitor, Case)
    end.

wait_down(Child, Monitor, Case, Outcome) ->
    receive
        {'DOWN', Monitor, process, Child, normal} -> Outcome;
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({reported_but_abnormal_child, Case, Reason})
    after 5000 ->
        abort(Child, Monitor, Case)
    end.

abort(Child, Monitor, Case) ->
    erlang:exit(Child, kill),
    receive
        {'DOWN', Monitor, process, Child, _} -> erlang:error({child_timeout, Case})
    after 5000 ->
        erlang:error({unsettled_child, Case})
    end.
