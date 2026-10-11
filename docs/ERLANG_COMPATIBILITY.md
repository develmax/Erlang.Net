# Erlang API and compatibility scope

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5.351 explicit erlang.erl/bif.tab union exports.170 registered,181 missing,total320 MFAs. Local1172959/full; original2523/same-head CI118 pending. Presence is not semantic completion. Whole-module goal active.

Part117 adds24 real clock/calendar/timer-option MFAs and115 compiled contracts; erlang170/351,181 missing,total320; local2959/full. [Evidence](validation/part-117/tests.json),[source scope](validation/part-117/reference-audit.json). Original2523 and same-head2959/full CI118 pending; historical116 remains2408/2408 at44ca437. User goal remains active; no full compatibility/performance claim.

| Export | Current state |
| --- | --- |
| `--/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `-/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `-/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `!/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `*/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `//2` | Registered; bounded functional evidence, see supported-mfas.json |
| `/=/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `+/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `+/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `++/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `</2` | Registered; bounded functional evidence, see supported-mfas.json |
| `=:=/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `=/=/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `=</2` | Registered; bounded functional evidence, see supported-mfas.json |
| `==/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `>/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `>=/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `abs/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `adler32_combine/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `adler32/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `adler32/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `alias/0` | Missing; real implementation/runtime prerequisites required |
| `alias/1` | Missing; real implementation/runtime prerequisites required |
| `alloc_info/1` | Missing; real implementation/runtime prerequisites required |
| `alloc_sizes/1` | Missing; real implementation/runtime prerequisites required |
| `and/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `append_element/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `append/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `apply/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `apply/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `atom_to_binary/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `atom_to_binary/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `atom_to_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `band/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_part/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_part/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_atom/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_atom/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_existing_atom/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_existing_atom/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_float/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_integer/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_integer/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_list/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `binary_to_term/1` | Missing; real implementation/runtime prerequisites required |
| `binary_to_term/2` | Missing; real implementation/runtime prerequisites required |
| `bit_size/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `bitstring_to_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `bnot/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `bor/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `bsl/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `bsr/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `bump_reductions/1` | Missing; real implementation/runtime prerequisites required |
| `bxor/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `byte_size/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `call_on_load_function/1` | Missing; real implementation/runtime prerequisites required |
| `cancel_timer/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `cancel_timer/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `ceil/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `check_old_code/1` | Missing; real implementation/runtime prerequisites required |
| `check_process_code/2` | Missing; real implementation/runtime prerequisites required |
| `check_process_code/3` | Missing; real implementation/runtime prerequisites required |
| `convert_time_unit/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `crc32_combine/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `crc32/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `crc32/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `date/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `decode_packet/3` | Missing; real implementation/runtime prerequisites required |
| `delay_trap/2` | Missing; real implementation/runtime prerequisites required |
| `delete_element/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `delete_module/1` | Missing; real implementation/runtime prerequisites required |
| `demonitor/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `demonitor/2` | Missing; real implementation/runtime prerequisites required |
| `disconnect_node/1` | Missing; real implementation/runtime prerequisites required |
| `display_string/1` | Missing; real implementation/runtime prerequisites required |
| `display_string/2` | Missing; real implementation/runtime prerequisites required |
| `display/1` | Missing; real implementation/runtime prerequisites required |
| `dist_ctrl_get_data_notification/1` | Missing; real implementation/runtime prerequisites required |
| `dist_ctrl_get_data/1` | Missing; real implementation/runtime prerequisites required |
| `dist_ctrl_get_opt/2` | Missing; real implementation/runtime prerequisites required |
| `dist_ctrl_input_handler/2` | Missing; real implementation/runtime prerequisites required |
| `dist_ctrl_put_data/2` | Missing; real implementation/runtime prerequisites required |
| `dist_ctrl_set_opt/3` | Missing; real implementation/runtime prerequisites required |
| `dist_get_stat/1` | Missing; real implementation/runtime prerequisites required |
| `div/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `dmonitor_node/3` | Missing; real implementation/runtime prerequisites required |
| `dt_append_vm_tag_data/1` | Missing; real implementation/runtime prerequisites required |
| `dt_get_tag_data/0` | Missing; real implementation/runtime prerequisites required |
| `dt_get_tag/0` | Missing; real implementation/runtime prerequisites required |
| `dt_prepend_vm_tag_data/1` | Missing; real implementation/runtime prerequisites required |
| `dt_put_tag/1` | Missing; real implementation/runtime prerequisites required |
| `dt_restore_tag/1` | Missing; real implementation/runtime prerequisites required |
| `dt_spread_tag/1` | Missing; real implementation/runtime prerequisites required |
| `element/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `erase/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `erase/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `error/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `error/2` | Missing; real implementation/runtime prerequisites required |
| `error/3` | Missing; real implementation/runtime prerequisites required |
| `exit_signal/2` | Missing; real implementation/runtime prerequisites required |
| `exit_signal/3` | Missing; real implementation/runtime prerequisites required |
| `exit/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `exit/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `exit/3` | Missing; real implementation/runtime prerequisites required |
| `external_size/1` | Missing; real implementation/runtime prerequisites required |
| `external_size/2` | Missing; real implementation/runtime prerequisites required |
| `finish_after_on_load/2` | Missing; real implementation/runtime prerequisites required |
| `finish_loading/1` | Missing; real implementation/runtime prerequisites required |
| `float_to_binary/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `float_to_binary/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `float_to_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `float_to_list/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `float/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `floor/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `format_cpu_topology/1` | Missing; real implementation/runtime prerequisites required |
| `fun_info_mfa/1` | Missing; real implementation/runtime prerequisites required |
| `fun_info/1` | Missing; real implementation/runtime prerequisites required |
| `fun_info/2` | Missing; real implementation/runtime prerequisites required |
| `fun_to_list/1` | Missing; real implementation/runtime prerequisites required |
| `function_exported/3` | Missing; real implementation/runtime prerequisites required |
| `garbage_collect_message_area/0` | Missing; real implementation/runtime prerequisites required |
| `garbage_collect/0` | Missing; real implementation/runtime prerequisites required |
| `garbage_collect/1` | Missing; real implementation/runtime prerequisites required |
| `garbage_collect/2` | Missing; real implementation/runtime prerequisites required |
| `gather_gc_info_result/1` | Missing; real implementation/runtime prerequisites required |
| `get_cookie/0` | Missing; real implementation/runtime prerequisites required |
| `get_cookie/1` | Missing; real implementation/runtime prerequisites required |
| `get_keys/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `get_keys/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `get_module_info/1` | Missing; real implementation/runtime prerequisites required |
| `get_module_info/2` | Missing; real implementation/runtime prerequisites required |
| `get/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `get/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `group_leader/0` | Missing; real implementation/runtime prerequisites required |
| `group_leader/2` | Missing; real implementation/runtime prerequisites required |
| `halt/0` | Missing; real implementation/runtime prerequisites required |
| `halt/1` | Missing; real implementation/runtime prerequisites required |
| `halt/2` | Missing; real implementation/runtime prerequisites required |
| `has_prepared_code_on_load/1` | Missing; real implementation/runtime prerequisites required |
| `hd/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `hibernate/0` | Missing; real implementation/runtime prerequisites required |
| `hibernate/3` | Missing; real implementation/runtime prerequisites required |
| `insert_element/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `integer_to_binary/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `integer_to_binary/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `integer_to_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `integer_to_list/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `iolist_size/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `iolist_to_binary/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `iolist_to_iovec/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_alive/0` | Missing; real implementation/runtime prerequisites required |
| `is_atom/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_binary/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_bitstring/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_boolean/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_builtin/3` | Missing; real implementation/runtime prerequisites required |
| `is_float/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_function/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_function/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_integer/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_integer/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_map_key/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_map/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_number/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_pid/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_port/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_process_alive/1` | Missing; real implementation/runtime prerequisites required |
| `is_record/1` | Missing; real implementation/runtime prerequisites required |
| `is_record/2` | Missing; real implementation/runtime prerequisites required |
| `is_record/3` | Missing; real implementation/runtime prerequisites required |
| `is_reference/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `is_tuple/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `length/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `link/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `link/2` | Missing; real implementation/runtime prerequisites required |
| `list_to_atom/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_binary/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_bitstring/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_existing_atom/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_float/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_integer/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_integer/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `list_to_pid/1` | Missing; real implementation/runtime prerequisites required |
| `list_to_port/1` | Missing; real implementation/runtime prerequisites required |
| `list_to_ref/1` | Missing; real implementation/runtime prerequisites required |
| `list_to_tuple/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `load_module/2` | Missing; real implementation/runtime prerequisites required |
| `load_nif/2` | Missing; real implementation/runtime prerequisites required |
| `loaded/0` | Missing; real implementation/runtime prerequisites required |
| `localtime_to_universaltime/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `localtime_to_universaltime/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `localtime/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `make_fun/3` | Missing; real implementation/runtime prerequisites required |
| `make_ref/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `make_tuple/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `make_tuple/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `map_get/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `map_size/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `match_spec_test/3` | Missing; real implementation/runtime prerequisites required |
| `max/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `md5_final/1` | Missing; real implementation/runtime prerequisites required |
| `md5_init/0` | Missing; real implementation/runtime prerequisites required |
| `md5_update/2` | Missing; real implementation/runtime prerequisites required |
| `md5/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `memory/0` | Missing; real implementation/runtime prerequisites required |
| `memory/1` | Missing; real implementation/runtime prerequisites required |
| `min/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `module_loaded/1` | Missing; real implementation/runtime prerequisites required |
| `monitor_node/2` | Missing; real implementation/runtime prerequisites required |
| `monitor_node/3` | Missing; real implementation/runtime prerequisites required |
| `monitor/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `monitor/3` | Missing; real implementation/runtime prerequisites required |
| `monotonic_time/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `monotonic_time/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `nif_error/1` | Missing; real implementation/runtime prerequisites required |
| `nif_error/2` | Missing; real implementation/runtime prerequisites required |
| `node/0` | Missing; real implementation/runtime prerequisites required |
| `node/1` | Missing; real implementation/runtime prerequisites required |
| `nodes/0` | Missing; real implementation/runtime prerequisites required |
| `nodes/1` | Missing; real implementation/runtime prerequisites required |
| `nodes/2` | Missing; real implementation/runtime prerequisites required |
| `not/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `now/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `open_port/2` | Missing; real implementation/runtime prerequisites required |
| `or/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `phash/2` | Missing; real implementation/runtime prerequisites required |
| `phash2/1` | Missing; real implementation/runtime prerequisites required |
| `phash2/2` | Missing; real implementation/runtime prerequisites required |
| `pid_to_list/1` | Missing; real implementation/runtime prerequisites required |
| `port_call/2` | Missing; real implementation/runtime prerequisites required |
| `port_call/3` | Missing; real implementation/runtime prerequisites required |
| `port_close/1` | Missing; real implementation/runtime prerequisites required |
| `port_command/2` | Missing; real implementation/runtime prerequisites required |
| `port_command/3` | Missing; real implementation/runtime prerequisites required |
| `port_connect/2` | Missing; real implementation/runtime prerequisites required |
| `port_control/3` | Missing; real implementation/runtime prerequisites required |
| `port_get_data/1` | Missing; real implementation/runtime prerequisites required |
| `port_info/1` | Missing; real implementation/runtime prerequisites required |
| `port_info/2` | Missing; real implementation/runtime prerequisites required |
| `port_set_data/2` | Missing; real implementation/runtime prerequisites required |
| `port_to_list/1` | Missing; real implementation/runtime prerequisites required |
| `ports/0` | Missing; real implementation/runtime prerequisites required |
| `posixtime_to_universaltime/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `pre_loaded/0` | Missing; real implementation/runtime prerequisites required |
| `prepare_loading/2` | Missing; real implementation/runtime prerequisites required |
| `process_display/2` | Missing; real implementation/runtime prerequisites required |
| `process_flag/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `process_flag/3` | Missing; real implementation/runtime prerequisites required |
| `process_info/1` | Missing; real implementation/runtime prerequisites required |
| `process_info/2` | Missing; real implementation/runtime prerequisites required |
| `processes_iterator/0` | Missing; real implementation/runtime prerequisites required |
| `processes_next/1` | Missing; real implementation/runtime prerequisites required |
| `processes/0` | Missing; real implementation/runtime prerequisites required |
| `purge_module/1` | Missing; real implementation/runtime prerequisites required |
| `put/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `raise/3` | Missing; real implementation/runtime prerequisites required |
| `read_timer/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `read_timer/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `ref_to_list/1` | Missing; real implementation/runtime prerequisites required |
| `register/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `registered/0` | Missing; real implementation/runtime prerequisites required |
| `rem/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `resume_process/1` | Missing; real implementation/runtime prerequisites required |
| `round/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `self/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `send_after/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `send_after/4` | Registered; bounded functional evidence, see supported-mfas.json |
| `send_nosuspend/2` | Missing; real implementation/runtime prerequisites required |
| `send_nosuspend/3` | Missing; real implementation/runtime prerequisites required |
| `send/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `send/3` | Missing; real implementation/runtime prerequisites required |
| `seq_trace_info/1` | Missing; real implementation/runtime prerequisites required |
| `seq_trace_print/1` | Missing; real implementation/runtime prerequisites required |
| `seq_trace_print/2` | Missing; real implementation/runtime prerequisites required |
| `seq_trace/2` | Missing; real implementation/runtime prerequisites required |
| `set_cookie/1` | Missing; real implementation/runtime prerequisites required |
| `set_cookie/2` | Missing; real implementation/runtime prerequisites required |
| `set_cpu_topology/1` | Missing; real implementation/runtime prerequisites required |
| `setelement/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `setnode/2` | Missing; real implementation/runtime prerequisites required |
| `setnode/3` | Missing; real implementation/runtime prerequisites required |
| `size/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `spawn_link/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `spawn_link/2` | Missing; real implementation/runtime prerequisites required |
| `spawn_link/3` | Missing; real implementation/runtime prerequisites required |
| `spawn_link/4` | Missing; real implementation/runtime prerequisites required |
| `spawn_monitor/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `spawn_monitor/2` | Missing; real implementation/runtime prerequisites required |
| `spawn_monitor/3` | Missing; real implementation/runtime prerequisites required |
| `spawn_monitor/4` | Missing; real implementation/runtime prerequisites required |
| `spawn_opt/2` | Missing; real implementation/runtime prerequisites required |
| `spawn_opt/3` | Missing; real implementation/runtime prerequisites required |
| `spawn_opt/4` | Missing; real implementation/runtime prerequisites required |
| `spawn_opt/5` | Missing; real implementation/runtime prerequisites required |
| `spawn_request_abandon/1` | Missing; real implementation/runtime prerequisites required |
| `spawn_request/1` | Missing; real implementation/runtime prerequisites required |
| `spawn_request/2` | Missing; real implementation/runtime prerequisites required |
| `spawn_request/3` | Missing; real implementation/runtime prerequisites required |
| `spawn_request/4` | Missing; real implementation/runtime prerequisites required |
| `spawn_request/5` | Missing; real implementation/runtime prerequisites required |
| `spawn/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `spawn/2` | Missing; real implementation/runtime prerequisites required |
| `spawn/3` | Missing; real implementation/runtime prerequisites required |
| `spawn/4` | Missing; real implementation/runtime prerequisites required |
| `split_binary/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `start_timer/3` | Registered; bounded functional evidence, see supported-mfas.json |
| `start_timer/4` | Registered; bounded functional evidence, see supported-mfas.json |
| `statistics/1` | Missing; real implementation/runtime prerequisites required |
| `subtract/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `suspend_process/1` | Missing; real implementation/runtime prerequisites required |
| `suspend_process/2` | Missing; real implementation/runtime prerequisites required |
| `system_flag/2` | Missing; real implementation/runtime prerequisites required |
| `system_info/1` | Missing; real implementation/runtime prerequisites required |
| `system_monitor/0` | Missing; real implementation/runtime prerequisites required |
| `system_monitor/1` | Missing; real implementation/runtime prerequisites required |
| `system_monitor/2` | Missing; real implementation/runtime prerequisites required |
| `system_profile/0` | Missing; real implementation/runtime prerequisites required |
| `system_profile/2` | Missing; real implementation/runtime prerequisites required |
| `system_time/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `system_time/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `term_to_binary/1` | Missing; real implementation/runtime prerequisites required |
| `term_to_binary/2` | Missing; real implementation/runtime prerequisites required |
| `term_to_iovec/1` | Missing; real implementation/runtime prerequisites required |
| `term_to_iovec/2` | Missing; real implementation/runtime prerequisites required |
| `throw/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `time_offset/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `time_offset/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `time/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `timestamp/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `tl/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `trace_delivered/1` | Missing; real implementation/runtime prerequisites required |
| `trace_info/2` | Missing; real implementation/runtime prerequisites required |
| `trace_pattern/2` | Missing; real implementation/runtime prerequisites required |
| `trace_pattern/3` | Missing; real implementation/runtime prerequisites required |
| `trace/3` | Missing; real implementation/runtime prerequisites required |
| `trunc/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `tuple_size/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `tuple_to_list/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `unalias/1` | Missing; real implementation/runtime prerequisites required |
| `unique_integer/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `unique_integer/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `universaltime_to_localtime/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `universaltime_to_posixtime/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `universaltime/0` | Registered; bounded functional evidence, see supported-mfas.json |
| `unlink/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `unregister/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `whereis/1` | Registered; bounded functional evidence, see supported-mfas.json |
| `xor/2` | Registered; bounded functional evidence, see supported-mfas.json |
| `yield/0` | Missing; real implementation/runtime prerequisites required |
