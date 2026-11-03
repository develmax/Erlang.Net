internal static class MfaCompatibilityTopics
{
    private static readonly (string Topic, string Functions)[] Groups =
    [
        ("numbers", "+ - * / div rem band bor bxor bsl bsr abs float round trunc floor ceil bnot"),
        ("time", "convert_time_unit monotonic_time system_time time_offset timestamp now unique_integer date time localtime universaltime localtime_to_universaltime universaltime_to_localtime universaltime_to_posixtime posixtime_to_universaltime send_after start_timer read_timer cancel_timer"),
        ("distribution", "node is_alive nodes get_cookie set_cookie monitor_node dmonitor_node disconnect_node setnode dist_get_stat dist_ctrl_put_data dist_ctrl_get_opt dist_ctrl_set_opt dist_ctrl_get_data_notification dist_ctrl_get_data dist_ctrl_input_handler"),
        ("ports", "open_port ports port_close port_connect port_get_data port_set_data port_info port_command port_call port_control port_to_list list_to_port decode_packet"),
        ("code-and-native", "apply module_loaded loaded pre_loaded get_module_info delete_module check_old_code purge_module check_process_code function_exported is_builtin make_fun fun_info fun_info_mfa fun_to_list nif_error load_nif prepare_loading has_prepared_code_on_load finish_loading call_on_load_function finish_after_on_load load_module"),
        ("diagnostics-and-memory", "garbage_collect garbage_collect_message_area gather_gc_info_result dt_get_tag dt_get_tag_data dt_put_tag dt_append_vm_tag_data dt_prepend_vm_tag_data dt_spread_tag dt_restore_tag seq_trace seq_trace_info seq_trace_print system_flag system_info system_monitor system_profile trace trace_info trace_pattern trace_delivered match_spec_test statistics alloc_info alloc_sizes memory display display_string halt"),
        ("processes", "self make_ref register whereis unregister link unlink monitor demonitor process_flag exit error throw raise put get erase get_keys spawn spawn_link spawn_monitor send ! processes registered is_process_alive yield bump_reductions process_info process_display pid_to_list list_to_pid processes_iterator processes_next ref_to_list list_to_ref suspend_process resume_process alias unalias send_nosuspend group_leader spawn_opt spawn_request spawn_request_abandon hibernate delay_trap format_cpu_topology set_cpu_topology exit_signal"),
        ("binary-and-encoding", "bit_size byte_size integer_to_list integer_to_binary list_to_integer binary_to_integer list_to_binary iolist_to_binary list_to_bitstring iolist_to_iovec iolist_size binary_to_list bitstring_to_list binary_part split_binary list_to_float binary_to_float adler32 crc32 adler32_combine crc32_combine md5 md5_init md5_update md5_final atom_to_list list_to_atom list_to_existing_atom atom_to_binary binary_to_atom binary_to_existing_atom float_to_list float_to_binary term_to_binary term_to_iovec external_size binary_to_term")
    ];

    public static string For(MfaCompatibilityEntry entry)
    {
        if (entry.Module != "erlang")
            return "commands";

        foreach (var group in Groups)
        {
            if (group.Functions.Split(' ').Contains(entry.Function, StringComparer.Ordinal))
                return group.Topic;
        }

        return "terms-and-collections";
    }
}
