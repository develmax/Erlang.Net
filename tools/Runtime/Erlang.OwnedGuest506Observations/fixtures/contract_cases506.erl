-module(contract_cases506).
-export([options/0, flag_values/0, statistics_keys/0, facades/0]).

% Data for the coordinated dispatcher. No resource result is precomputed here.
options() ->
    [
     {baseline, []},
     {heap_zero, [{min_heap_size, 0}]},
     {heap_small, [{min_heap_size, 17}]},
     {heap_larger, [{min_heap_size, 4096}]},
     {bin_heap_zero, [{min_bin_vheap_size, 0}]},
     {bin_heap_larger, [{min_bin_vheap_size, 8192}]},
     {max_zero, [{max_heap_size, 0}]},
     {max_map_flags, [{max_heap_size, #{size => 1048576, kill => false,
                                      error_logger => false, include_shared_binaries => true}}]},
     {max_map_extra_key, [{max_heap_size, #{size => 1048576, extra506 => ignored}}]},
     {fullsweep_zero, [{fullsweep_after, 0}]},
     {fullsweep_u16_edge, [{fullsweep_after, 65535}]},
     {fullsweep_wrap, [{fullsweep_after, 65536}]},
     {last_heap_wins, [{min_heap_size, 17}, {min_heap_size, 4096}]},
     {invalid_then_valid, [{min_heap_size, -1}, {min_heap_size, 4096}]},
     {valid_then_invalid, [{min_heap_size, 4096}, {min_heap_size, -1}]},
     {invalid_float, [{min_heap_size, 1.0}]},
     {invalid_bignum, [{min_heap_size, 1 bsl 128}]},
     {invalid_max_map_size_missing, [{max_heap_size, #{kill => false}}]},
     {invalid_max_map_bool, [{max_heap_size, #{size => 1048576, kill => 1}}]},
     {invalid_max_below_min, [{min_heap_size, 4096}, {max_heap_size, 1}]},
     {on_heap, [{message_queue_data, on_heap}]},
     {off_heap, [{message_queue_data, off_heap}]},
     {async_false_true, [{async_dist, false}, {async_dist, true}]},
     {monitor, [monitor]},
     {monitor_custom_then_atom, [{monitor, [{tag, contract_down506}]}, monitor]},
     {monitor_atom_then_custom, [monitor, {monitor, [{tag, contract_down506}]}]},
     {priority_last, [{priority, low}, {priority, high}]},
     {scheduler_zero, [{scheduler, 0}]},
     {unknown_option, [{unknown_option506, 1}]},
     {invalid_option_list, [monitor | improper]},
     {invalid_option_container, options_not_list}
    ].

flag_values() ->
    [{min_heap_size, [0, 17, 4096, -1, 1.0, 1 bsl 128]},
     {min_bin_vheap_size, [0, 17, 8192, -1, 1.0]},
     {max_heap_size, [0, #{size => 1048576, kill => false, error_logger => false},
                      #{size => 1048576, include_shared_binaries => true, extra506 => ignored},
                      #{kill => false}, #{size => -1}, #{size => 1, kill => 1}]},
     {fullsweep_after, [0, 65535, 65536, 65537, -1, 1.0]}].

statistics_keys() ->
    [active_tasks, active_tasks_all, context_switches, exact_reductions,
     garbage_collection, io, microstate_accounting, reductions, run_queue,
     run_queue_lengths, run_queue_lengths_all, run_queues, runtime,
     scheduler_wall_time, scheduler_wall_time_all, total_active_tasks,
     total_active_tasks_all, total_run_queue_lengths, total_run_queue_lengths_all,
     wall_clock].

facades() ->
    [erlang_spawn_opt_2_fun, erlang_spawn_opt_2_tuple,
     erlang_spawn_opt_3_local_node, erlang_spawn_opt_3_remote_node,
     erlang_spawn_opt_4_mfa, erlang_spawn_opt_5_local_node, erlang_spawn_opt_5_remote_node,
     erlang_spawn_request_2_fun_options, erlang_spawn_request_2_node_fun,
     erlang_spawn_request_3_node_fun_options, erlang_spawn_request_3_mfa,
     erlang_spawn_request_4_node_mfa, erlang_spawn_request_4_mfa_options,
     erlang_spawn_request_5_local_node, erlang_spawn_request_5_remote_node,
     proc_spawn_opt_2_fun, proc_spawn_opt_3_node_fun, proc_spawn_opt_4_mfa,
     proc_spawn_opt_5_node_mfa, proc_start_5, proc_start_link_5, proc_start_monitor_5].
