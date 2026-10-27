-module(contract_errors506).
-export([run/0]).

capture(Fun) ->
    try Fun() of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.

run() ->
    Caller = self(),
    Cases = [
        {process_info_1_pid_type, fun() -> erlang:process_info(not_a_pid) end},
        {process_info_2_item_type, fun() -> erlang:process_info(Caller, contract_unknown_item506) end},
        {spawn_opt_2_fun_type, fun() -> erlang:spawn_opt(not_a_fun, []) end},
        {spawn_opt_3_fun_type, fun() -> erlang:spawn_opt(node(), not_a_fun, []) end},
        {spawn_opt_4_module_type, fun() -> erlang:spawn_opt(42, entry, [], []) end},
        {spawn_opt_5_node_type, fun() -> erlang:spawn_opt(42, contract_subject506, entry0, [], []) end},
        {spawn_request_2_fun_type, fun() -> erlang:spawn_request(not_a_fun, []) end},
        {spawn_request_3_module_type, fun() -> erlang:spawn_request(42, entry, []) end},
        {spawn_request_4_module_type, fun() -> erlang:spawn_request(42, entry, [], []) end},
        {spawn_request_5_node_type, fun() -> erlang:spawn_request(42, contract_subject506, entry0, [], []) end},
        {system_flag_2_negative_heap, fun() -> erlang:system_flag(min_heap_size, -1) end},
        {statistics_1_unknown, fun() -> erlang:statistics(contract_unknown_stat506) end},
        {load_nif_2_filename_type, fun() -> erlang:load_nif(42, []) end},
        {proc_spawn_opt_2_fun_type, fun() -> proc_lib:spawn_opt(not_a_fun, []) end},
        {proc_spawn_opt_3_fun_type, fun() -> proc_lib:spawn_opt(node(), not_a_fun, []) end},
        {proc_spawn_opt_4_module_type, fun() -> proc_lib:spawn_opt(42, entry, [], []) end},
        {proc_spawn_opt_5_module_type, fun() -> proc_lib:spawn_opt(node(), 42, entry, [], []) end},
        {proc_start_5_module_type, fun() -> proc_lib:start(42, entry, [], 10, []) end},
        {proc_start_link_5_module_type, fun() -> proc_lib:start_link(42, entry, [], 10, []) end},
        {proc_start_monitor_5_module_type, fun() -> proc_lib:start_monitor(42, entry, [], 10, []) end},
        {proc_init_p_3_fun_type, fun() -> proc_lib:init_p(Caller, [], 42) end},
        {proc_init_p_5_module_type, fun() -> proc_lib:init_p(Caller, [], 42, entry, []) end},
        {proc_wake_up_3_module_type, fun() -> proc_lib:wake_up(42, entry, []) end},
        {proc_hibernate_3_module_type, fun() -> proc_lib:hibernate(42, entry, []) end}
    ],
    {#{caller => Caller, node => node()}, [{Id, capture(Fun)} || {Id, Fun} <- Cases]}.
