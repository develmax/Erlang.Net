# Erlang API and compatibility scope

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Explicit erlang.erl exports/bif.tab union **351**. Current **146/351 registered**, **205 missing**, **296 total MFAs**; [2816/full local115](validation/part-115/tests.json). New2408 original/same-head2816 CI116 pending; latest completed [2233/2233 and2626/full CI114](validation/part-114/remote-checkpoint.json) at2832c78. API presence is not semantic completion or weighted progress. Whole-module goal stays active; no generic error stubs.

[175 new contracts](validation/part-115/contract-matrix.json), [source/reuse/resource boundaries](validation/part-115/reference-audit.json), [all prior778/1371/84 preserved](validation/part-115/corpus-audit.json). C# production, consolidated notices, no new dependencies or BEAM. No claim of outperforming OTP without measurements.

| Export | Current state |
| --- | --- |
| `--/2` | Registered; bounded functional evidence in registry |
| `-/1` | Registered; bounded functional evidence in registry |
| `-/2` | Registered; bounded functional evidence in registry |
| `!/2` | Registered; bounded functional evidence in registry |
| `*/2` | Registered; bounded functional evidence in registry |
| `//2` | Registered; bounded functional evidence in registry |
| `/=/2` | Registered; bounded functional evidence in registry |
| `+/1` | Registered; bounded functional evidence in registry |
| `+/2` | Registered; bounded functional evidence in registry |
| `++/2` | Registered; bounded functional evidence in registry |
| `</2` | Registered; bounded functional evidence in registry |
| `=:=/2` | Registered; bounded functional evidence in registry |
| `=/=/2` | Registered; bounded functional evidence in registry |
| `=</2` | Registered; bounded functional evidence in registry |
| `==/2` | Registered; bounded functional evidence in registry |
| `>/2` | Registered; bounded functional evidence in registry |
| `>=/2` | Registered; bounded functional evidence in registry |
| `abs/1` | Registered; bounded functional evidence in registry |
| `adler32_combine/3` | Registered; bounded functional evidence in registry |
| `adler32/1` | Registered; bounded functional evidence in registry |
| `adler32/2` | Registered; bounded functional evidence in registry |
| `alias/0` | Missing implementation/runtime support |
| `alias/1` | Missing implementation/runtime support |
| `alloc_info/1` | Missing implementation/runtime support |
| `alloc_sizes/1` | Missing implementation/runtime support |
| `and/2` | Registered; bounded functional evidence in registry |
| `append_element/2` | Registered; bounded functional evidence in registry |
| `append/2` | Registered; bounded functional evidence in registry |
| `apply/2` | Registered; bounded functional evidence in registry |
| `apply/3` | Registered; bounded functional evidence in registry |
| `atom_to_binary/1` | Registered; bounded functional evidence in registry |
| `atom_to_binary/2` | Registered; bounded functional evidence in registry |
| `atom_to_list/1` | Registered; bounded functional evidence in registry |
| `band/2` | Registered; bounded functional evidence in registry |
| `binary_part/2` | Registered; bounded functional evidence in registry |
| `binary_part/3` | Registered; bounded functional evidence in registry |
| `binary_to_atom/1` | Registered; bounded functional evidence in registry |
| `binary_to_atom/2` | Registered; bounded functional evidence in registry |
| `binary_to_existing_atom/1` | Registered; bounded functional evidence in registry |
| `binary_to_existing_atom/2` | Registered; bounded functional evidence in registry |
| `binary_to_float/1` | Registered; bounded functional evidence in registry |
| `binary_to_integer/1` | Registered; bounded functional evidence in registry |
| `binary_to_integer/2` | Registered; bounded functional evidence in registry |
| `binary_to_list/1` | Registered; bounded functional evidence in registry |
| `binary_to_list/3` | Registered; bounded functional evidence in registry |
| `binary_to_term/1` | Missing implementation/runtime support |
| `binary_to_term/2` | Missing implementation/runtime support |
| `bit_size/1` | Registered; bounded functional evidence in registry |
| `bitstring_to_list/1` | Registered; bounded functional evidence in registry |
| `bnot/1` | Registered; bounded functional evidence in registry |
| `bor/2` | Registered; bounded functional evidence in registry |
| `bsl/2` | Registered; bounded functional evidence in registry |
| `bsr/2` | Registered; bounded functional evidence in registry |
| `bump_reductions/1` | Missing implementation/runtime support |
| `bxor/2` | Registered; bounded functional evidence in registry |
| `byte_size/1` | Registered; bounded functional evidence in registry |
| `call_on_load_function/1` | Missing implementation/runtime support |
| `cancel_timer/1` | Registered; bounded functional evidence in registry |
| `cancel_timer/2` | Missing implementation/runtime support |
| `ceil/1` | Registered; bounded functional evidence in registry |
| `check_old_code/1` | Missing implementation/runtime support |
| `check_process_code/2` | Missing implementation/runtime support |
| `check_process_code/3` | Missing implementation/runtime support |
| `convert_time_unit/3` | Missing implementation/runtime support |
| `crc32_combine/3` | Registered; bounded functional evidence in registry |
| `crc32/1` | Registered; bounded functional evidence in registry |
| `crc32/2` | Registered; bounded functional evidence in registry |
| `date/0` | Missing implementation/runtime support |
| `decode_packet/3` | Missing implementation/runtime support |
| `delay_trap/2` | Missing implementation/runtime support |
| `delete_element/2` | Registered; bounded functional evidence in registry |
| `delete_module/1` | Missing implementation/runtime support |
| `demonitor/1` | Registered; bounded functional evidence in registry |
| `demonitor/2` | Missing implementation/runtime support |
| `disconnect_node/1` | Missing implementation/runtime support |
| `display_string/1` | Missing implementation/runtime support |
| `display_string/2` | Missing implementation/runtime support |
| `display/1` | Missing implementation/runtime support |
| `dist_ctrl_get_data_notification/1` | Missing implementation/runtime support |
| `dist_ctrl_get_data/1` | Missing implementation/runtime support |
| `dist_ctrl_get_opt/2` | Missing implementation/runtime support |
| `dist_ctrl_input_handler/2` | Missing implementation/runtime support |
| `dist_ctrl_put_data/2` | Missing implementation/runtime support |
| `dist_ctrl_set_opt/3` | Missing implementation/runtime support |
| `dist_get_stat/1` | Missing implementation/runtime support |
| `div/2` | Registered; bounded functional evidence in registry |
| `dmonitor_node/3` | Missing implementation/runtime support |
| `dt_append_vm_tag_data/1` | Missing implementation/runtime support |
| `dt_get_tag_data/0` | Missing implementation/runtime support |
| `dt_get_tag/0` | Missing implementation/runtime support |
| `dt_prepend_vm_tag_data/1` | Missing implementation/runtime support |
| `dt_put_tag/1` | Missing implementation/runtime support |
| `dt_restore_tag/1` | Missing implementation/runtime support |
| `dt_spread_tag/1` | Missing implementation/runtime support |
| `element/2` | Registered; bounded functional evidence in registry |
| `erase/0` | Registered; bounded functional evidence in registry |
| `erase/1` | Registered; bounded functional evidence in registry |
| `error/1` | Registered; bounded functional evidence in registry |
| `error/2` | Missing implementation/runtime support |
| `error/3` | Missing implementation/runtime support |
| `exit_signal/2` | Missing implementation/runtime support |
| `exit_signal/3` | Missing implementation/runtime support |
| `exit/1` | Registered; bounded functional evidence in registry |
| `exit/2` | Registered; bounded functional evidence in registry |
| `exit/3` | Missing implementation/runtime support |
| `external_size/1` | Missing implementation/runtime support |
| `external_size/2` | Missing implementation/runtime support |
| `finish_after_on_load/2` | Missing implementation/runtime support |
| `finish_loading/1` | Missing implementation/runtime support |
| `float_to_binary/1` | Registered; bounded functional evidence in registry |
| `float_to_binary/2` | Registered; bounded functional evidence in registry |
| `float_to_list/1` | Registered; bounded functional evidence in registry |
| `float_to_list/2` | Registered; bounded functional evidence in registry |
| `float/1` | Registered; bounded functional evidence in registry |
| `floor/1` | Registered; bounded functional evidence in registry |
| `format_cpu_topology/1` | Missing implementation/runtime support |
| `fun_info_mfa/1` | Missing implementation/runtime support |
| `fun_info/1` | Missing implementation/runtime support |
| `fun_info/2` | Missing implementation/runtime support |
| `fun_to_list/1` | Missing implementation/runtime support |
| `function_exported/3` | Missing implementation/runtime support |
| `garbage_collect_message_area/0` | Missing implementation/runtime support |
| `garbage_collect/0` | Missing implementation/runtime support |
| `garbage_collect/1` | Missing implementation/runtime support |
| `garbage_collect/2` | Missing implementation/runtime support |
| `gather_gc_info_result/1` | Missing implementation/runtime support |
| `get_cookie/0` | Missing implementation/runtime support |
| `get_cookie/1` | Missing implementation/runtime support |
| `get_keys/0` | Registered; bounded functional evidence in registry |
| `get_keys/1` | Registered; bounded functional evidence in registry |
| `get_module_info/1` | Missing implementation/runtime support |
| `get_module_info/2` | Missing implementation/runtime support |
| `get/0` | Registered; bounded functional evidence in registry |
| `get/1` | Registered; bounded functional evidence in registry |
| `group_leader/0` | Missing implementation/runtime support |
| `group_leader/2` | Missing implementation/runtime support |
| `halt/0` | Missing implementation/runtime support |
| `halt/1` | Missing implementation/runtime support |
| `halt/2` | Missing implementation/runtime support |
| `has_prepared_code_on_load/1` | Missing implementation/runtime support |
| `hd/1` | Registered; bounded functional evidence in registry |
| `hibernate/0` | Missing implementation/runtime support |
| `hibernate/3` | Missing implementation/runtime support |
| `insert_element/3` | Registered; bounded functional evidence in registry |
| `integer_to_binary/1` | Registered; bounded functional evidence in registry |
| `integer_to_binary/2` | Registered; bounded functional evidence in registry |
| `integer_to_list/1` | Registered; bounded functional evidence in registry |
| `integer_to_list/2` | Registered; bounded functional evidence in registry |
| `iolist_size/1` | Registered; bounded functional evidence in registry |
| `iolist_to_binary/1` | Registered; bounded functional evidence in registry |
| `iolist_to_iovec/1` | Registered; bounded functional evidence in registry |
| `is_alive/0` | Missing implementation/runtime support |
| `is_atom/1` | Registered; bounded functional evidence in registry |
| `is_binary/1` | Registered; bounded functional evidence in registry |
| `is_bitstring/1` | Registered; bounded functional evidence in registry |
| `is_boolean/1` | Registered; bounded functional evidence in registry |
| `is_builtin/3` | Missing implementation/runtime support |
| `is_float/1` | Registered; bounded functional evidence in registry |
| `is_function/1` | Registered; bounded functional evidence in registry |
| `is_function/2` | Registered; bounded functional evidence in registry |
| `is_integer/1` | Registered; bounded functional evidence in registry |
| `is_integer/3` | Registered; bounded functional evidence in registry |
| `is_list/1` | Registered; bounded functional evidence in registry |
| `is_map_key/2` | Registered; bounded functional evidence in registry |
| `is_map/1` | Registered; bounded functional evidence in registry |
| `is_number/1` | Registered; bounded functional evidence in registry |
| `is_pid/1` | Registered; bounded functional evidence in registry |
| `is_port/1` | Registered; bounded functional evidence in registry |
| `is_process_alive/1` | Missing implementation/runtime support |
| `is_record/1` | Missing implementation/runtime support |
| `is_record/2` | Missing implementation/runtime support |
| `is_record/3` | Missing implementation/runtime support |
| `is_reference/1` | Registered; bounded functional evidence in registry |
| `is_tuple/1` | Registered; bounded functional evidence in registry |
| `length/1` | Registered; bounded functional evidence in registry |
| `link/1` | Registered; bounded functional evidence in registry |
| `link/2` | Missing implementation/runtime support |
| `list_to_atom/1` | Registered; bounded functional evidence in registry |
| `list_to_binary/1` | Registered; bounded functional evidence in registry |
| `list_to_bitstring/1` | Registered; bounded functional evidence in registry |
| `list_to_existing_atom/1` | Registered; bounded functional evidence in registry |
| `list_to_float/1` | Registered; bounded functional evidence in registry |
| `list_to_integer/1` | Registered; bounded functional evidence in registry |
| `list_to_integer/2` | Registered; bounded functional evidence in registry |
| `list_to_pid/1` | Missing implementation/runtime support |
| `list_to_port/1` | Missing implementation/runtime support |
| `list_to_ref/1` | Missing implementation/runtime support |
| `list_to_tuple/1` | Registered; bounded functional evidence in registry |
| `load_module/2` | Missing implementation/runtime support |
| `load_nif/2` | Missing implementation/runtime support |
| `loaded/0` | Missing implementation/runtime support |
| `localtime_to_universaltime/1` | Missing implementation/runtime support |
| `localtime_to_universaltime/2` | Missing implementation/runtime support |
| `localtime/0` | Missing implementation/runtime support |
| `make_fun/3` | Missing implementation/runtime support |
| `make_ref/0` | Registered; bounded functional evidence in registry |
| `make_tuple/2` | Registered; bounded functional evidence in registry |
| `make_tuple/3` | Registered; bounded functional evidence in registry |
| `map_get/2` | Registered; bounded functional evidence in registry |
| `map_size/1` | Registered; bounded functional evidence in registry |
| `match_spec_test/3` | Missing implementation/runtime support |
| `max/2` | Registered; bounded functional evidence in registry |
| `md5_final/1` | Missing implementation/runtime support |
| `md5_init/0` | Missing implementation/runtime support |
| `md5_update/2` | Missing implementation/runtime support |
| `md5/1` | Registered; bounded functional evidence in registry |
| `memory/0` | Missing implementation/runtime support |
| `memory/1` | Missing implementation/runtime support |
| `min/2` | Registered; bounded functional evidence in registry |
| `module_loaded/1` | Missing implementation/runtime support |
| `monitor_node/2` | Missing implementation/runtime support |
| `monitor_node/3` | Missing implementation/runtime support |
| `monitor/2` | Registered; bounded functional evidence in registry |
| `monitor/3` | Missing implementation/runtime support |
| `monotonic_time/0` | Missing implementation/runtime support |
| `monotonic_time/1` | Missing implementation/runtime support |
| `nif_error/1` | Missing implementation/runtime support |
| `nif_error/2` | Missing implementation/runtime support |
| `node/0` | Missing implementation/runtime support |
| `node/1` | Missing implementation/runtime support |
| `nodes/0` | Missing implementation/runtime support |
| `nodes/1` | Missing implementation/runtime support |
| `nodes/2` | Missing implementation/runtime support |
| `not/1` | Registered; bounded functional evidence in registry |
| `now/0` | Missing implementation/runtime support |
| `open_port/2` | Missing implementation/runtime support |
| `or/2` | Registered; bounded functional evidence in registry |
| `phash/2` | Missing implementation/runtime support |
| `phash2/1` | Missing implementation/runtime support |
| `phash2/2` | Missing implementation/runtime support |
| `pid_to_list/1` | Missing implementation/runtime support |
| `port_call/2` | Missing implementation/runtime support |
| `port_call/3` | Missing implementation/runtime support |
| `port_close/1` | Missing implementation/runtime support |
| `port_command/2` | Missing implementation/runtime support |
| `port_command/3` | Missing implementation/runtime support |
| `port_connect/2` | Missing implementation/runtime support |
| `port_control/3` | Missing implementation/runtime support |
| `port_get_data/1` | Missing implementation/runtime support |
| `port_info/1` | Missing implementation/runtime support |
| `port_info/2` | Missing implementation/runtime support |
| `port_set_data/2` | Missing implementation/runtime support |
| `port_to_list/1` | Missing implementation/runtime support |
| `ports/0` | Missing implementation/runtime support |
| `posixtime_to_universaltime/1` | Missing implementation/runtime support |
| `pre_loaded/0` | Missing implementation/runtime support |
| `prepare_loading/2` | Missing implementation/runtime support |
| `process_display/2` | Missing implementation/runtime support |
| `process_flag/2` | Registered; bounded functional evidence in registry |
| `process_flag/3` | Missing implementation/runtime support |
| `process_info/1` | Missing implementation/runtime support |
| `process_info/2` | Missing implementation/runtime support |
| `processes_iterator/0` | Missing implementation/runtime support |
| `processes_next/1` | Missing implementation/runtime support |
| `processes/0` | Missing implementation/runtime support |
| `purge_module/1` | Missing implementation/runtime support |
| `put/2` | Registered; bounded functional evidence in registry |
| `raise/3` | Missing implementation/runtime support |
| `read_timer/1` | Registered; bounded functional evidence in registry |
| `read_timer/2` | Missing implementation/runtime support |
| `ref_to_list/1` | Missing implementation/runtime support |
| `register/2` | Registered; bounded functional evidence in registry |
| `registered/0` | Missing implementation/runtime support |
| `rem/2` | Registered; bounded functional evidence in registry |
| `resume_process/1` | Missing implementation/runtime support |
| `round/1` | Registered; bounded functional evidence in registry |
| `self/0` | Registered; bounded functional evidence in registry |
| `send_after/3` | Registered; bounded functional evidence in registry |
| `send_after/4` | Missing implementation/runtime support |
| `send_nosuspend/2` | Missing implementation/runtime support |
| `send_nosuspend/3` | Missing implementation/runtime support |
| `send/2` | Registered; bounded functional evidence in registry |
| `send/3` | Missing implementation/runtime support |
| `seq_trace_info/1` | Missing implementation/runtime support |
| `seq_trace_print/1` | Missing implementation/runtime support |
| `seq_trace_print/2` | Missing implementation/runtime support |
| `seq_trace/2` | Missing implementation/runtime support |
| `set_cookie/1` | Missing implementation/runtime support |
| `set_cookie/2` | Missing implementation/runtime support |
| `set_cpu_topology/1` | Missing implementation/runtime support |
| `setelement/3` | Registered; bounded functional evidence in registry |
| `setnode/2` | Missing implementation/runtime support |
| `setnode/3` | Missing implementation/runtime support |
| `size/1` | Registered; bounded functional evidence in registry |
| `spawn_link/1` | Registered; bounded functional evidence in registry |
| `spawn_link/2` | Missing implementation/runtime support |
| `spawn_link/3` | Missing implementation/runtime support |
| `spawn_link/4` | Missing implementation/runtime support |
| `spawn_monitor/1` | Registered; bounded functional evidence in registry |
| `spawn_monitor/2` | Missing implementation/runtime support |
| `spawn_monitor/3` | Missing implementation/runtime support |
| `spawn_monitor/4` | Missing implementation/runtime support |
| `spawn_opt/2` | Missing implementation/runtime support |
| `spawn_opt/3` | Missing implementation/runtime support |
| `spawn_opt/4` | Missing implementation/runtime support |
| `spawn_opt/5` | Missing implementation/runtime support |
| `spawn_request_abandon/1` | Missing implementation/runtime support |
| `spawn_request/1` | Missing implementation/runtime support |
| `spawn_request/2` | Missing implementation/runtime support |
| `spawn_request/3` | Missing implementation/runtime support |
| `spawn_request/4` | Missing implementation/runtime support |
| `spawn_request/5` | Missing implementation/runtime support |
| `spawn/1` | Registered; bounded functional evidence in registry |
| `spawn/2` | Missing implementation/runtime support |
| `spawn/3` | Missing implementation/runtime support |
| `spawn/4` | Missing implementation/runtime support |
| `split_binary/2` | Registered; bounded functional evidence in registry |
| `start_timer/3` | Registered; bounded functional evidence in registry |
| `start_timer/4` | Missing implementation/runtime support |
| `statistics/1` | Missing implementation/runtime support |
| `subtract/2` | Registered; bounded functional evidence in registry |
| `suspend_process/1` | Missing implementation/runtime support |
| `suspend_process/2` | Missing implementation/runtime support |
| `system_flag/2` | Missing implementation/runtime support |
| `system_info/1` | Missing implementation/runtime support |
| `system_monitor/0` | Missing implementation/runtime support |
| `system_monitor/1` | Missing implementation/runtime support |
| `system_monitor/2` | Missing implementation/runtime support |
| `system_profile/0` | Missing implementation/runtime support |
| `system_profile/2` | Missing implementation/runtime support |
| `system_time/0` | Missing implementation/runtime support |
| `system_time/1` | Missing implementation/runtime support |
| `term_to_binary/1` | Missing implementation/runtime support |
| `term_to_binary/2` | Missing implementation/runtime support |
| `term_to_iovec/1` | Missing implementation/runtime support |
| `term_to_iovec/2` | Missing implementation/runtime support |
| `throw/1` | Registered; bounded functional evidence in registry |
| `time_offset/0` | Missing implementation/runtime support |
| `time_offset/1` | Missing implementation/runtime support |
| `time/0` | Missing implementation/runtime support |
| `timestamp/0` | Missing implementation/runtime support |
| `tl/1` | Registered; bounded functional evidence in registry |
| `trace_delivered/1` | Missing implementation/runtime support |
| `trace_info/2` | Missing implementation/runtime support |
| `trace_pattern/2` | Missing implementation/runtime support |
| `trace_pattern/3` | Missing implementation/runtime support |
| `trace/3` | Missing implementation/runtime support |
| `trunc/1` | Registered; bounded functional evidence in registry |
| `tuple_size/1` | Registered; bounded functional evidence in registry |
| `tuple_to_list/1` | Registered; bounded functional evidence in registry |
| `unalias/1` | Missing implementation/runtime support |
| `unique_integer/0` | Missing implementation/runtime support |
| `unique_integer/1` | Missing implementation/runtime support |
| `universaltime_to_localtime/1` | Missing implementation/runtime support |
| `universaltime_to_posixtime/1` | Missing implementation/runtime support |
| `universaltime/0` | Missing implementation/runtime support |
| `unlink/1` | Registered; bounded functional evidence in registry |
| `unregister/1` | Registered; bounded functional evidence in registry |
| `whereis/1` | Registered; bounded functional evidence in registry |
| `xor/2` | Registered; bounded functional evidence in registry |
| `yield/0` | Missing implementation/runtime support |

## Remaining work and limits

Real clocks/unit/epoch/calendar/unique/timer-option support; ETF/native function and identity representation/original hashes; process spawn/link/monitor/alias/inspection; native records/code lifecycle; ports/distribution/tracing/VM/host state. Atom-name retention has native default capacity with local system_limit rather than fatal host exhaustion; complete configurable/native loaded-module population and host malformed names remain boundaries. Native storage offsets,full stacks/error_info/resources/exhaustive CT/performance parity and complete compiler remain unfinished.
