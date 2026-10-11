# Erlang API and compatibility scope

Pinned OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Union of explicit erlang.erl exports and bif.tab declarations: **351 functions/arities**. Current **133/351 registered**, **218 missing**, **283 total MFAs**, [2626/full local113](validation/part-113/tests.json). Original2233/same-head2626 CI pending114. [Failed/incomplete112](validation/part-112/remote-checkpoint.json) retained; latest complete [1878/1878 original110](validation/part-110/remote-checkpoint.json). API presence is not semantic completion or a weighted project percentage. The user's active goal remains the complete erlang module; no generic error stubs count as implementations.

[Source/correction/reuse audit](validation/part-113/reference-audit.json), [240 new independent contracts](validation/part-113/contract-matrix.json). Shared immutable term identity optimization, source-directed BIF and parser behavior, C# production, consolidated Apache/zlib notices, no new dependency or production BEAM. Code is not claimed faster than OTP without measurements.

| Export | Current state |
| --- | --- |
| `--/2` | Registered; partial functional scope |
| `-/1` | Registered; partial functional scope |
| `-/2` | Registered; partial functional scope |
| `!/2` | Registered; partial functional scope |
| `*/2` | Registered; partial functional scope |
| `//2` | Registered; partial functional scope |
| `/=/2` | Registered; partial functional scope |
| `+/1` | Registered; partial functional scope |
| `+/2` | Registered; partial functional scope |
| `++/2` | Registered; partial functional scope |
| `</2` | Registered; partial functional scope |
| `=:=/2` | Registered; partial functional scope |
| `=/=/2` | Registered; partial functional scope |
| `=</2` | Registered; partial functional scope |
| `==/2` | Registered; partial functional scope |
| `>/2` | Registered; partial functional scope |
| `>=/2` | Registered; partial functional scope |
| `abs/1` | Registered; partial functional scope |
| `adler32_combine/3` | Registered; partial functional scope |
| `adler32/1` | Registered; partial functional scope |
| `adler32/2` | Registered; partial functional scope |
| `alias/0` | Missing implementation/runtime support |
| `alias/1` | Missing implementation/runtime support |
| `alloc_info/1` | Missing implementation/runtime support |
| `alloc_sizes/1` | Missing implementation/runtime support |
| `and/2` | Registered; partial functional scope |
| `append_element/2` | Registered; partial functional scope |
| `append/2` | Registered; partial functional scope |
| `apply/2` | Registered; partial functional scope |
| `apply/3` | Registered; partial functional scope |
| `atom_to_binary/1` | Missing implementation/runtime support |
| `atom_to_binary/2` | Missing implementation/runtime support |
| `atom_to_list/1` | Missing implementation/runtime support |
| `band/2` | Registered; partial functional scope |
| `binary_part/2` | Registered; partial functional scope |
| `binary_part/3` | Registered; partial functional scope |
| `binary_to_atom/1` | Missing implementation/runtime support |
| `binary_to_atom/2` | Missing implementation/runtime support |
| `binary_to_existing_atom/1` | Missing implementation/runtime support |
| `binary_to_existing_atom/2` | Missing implementation/runtime support |
| `binary_to_float/1` | Registered; partial functional scope |
| `binary_to_integer/1` | Registered; partial functional scope |
| `binary_to_integer/2` | Registered; partial functional scope |
| `binary_to_list/1` | Registered; partial functional scope |
| `binary_to_list/3` | Registered; partial functional scope |
| `binary_to_term/1` | Missing implementation/runtime support |
| `binary_to_term/2` | Missing implementation/runtime support |
| `bit_size/1` | Registered; partial functional scope |
| `bitstring_to_list/1` | Registered; partial functional scope |
| `bnot/1` | Registered; partial functional scope |
| `bor/2` | Registered; partial functional scope |
| `bsl/2` | Registered; partial functional scope |
| `bsr/2` | Registered; partial functional scope |
| `bump_reductions/1` | Missing implementation/runtime support |
| `bxor/2` | Registered; partial functional scope |
| `byte_size/1` | Registered; partial functional scope |
| `call_on_load_function/1` | Missing implementation/runtime support |
| `cancel_timer/1` | Registered; partial functional scope |
| `cancel_timer/2` | Missing implementation/runtime support |
| `ceil/1` | Registered; partial functional scope |
| `check_old_code/1` | Missing implementation/runtime support |
| `check_process_code/2` | Missing implementation/runtime support |
| `check_process_code/3` | Missing implementation/runtime support |
| `convert_time_unit/3` | Missing implementation/runtime support |
| `crc32_combine/3` | Registered; partial functional scope |
| `crc32/1` | Registered; partial functional scope |
| `crc32/2` | Registered; partial functional scope |
| `date/0` | Missing implementation/runtime support |
| `decode_packet/3` | Missing implementation/runtime support |
| `delay_trap/2` | Missing implementation/runtime support |
| `delete_element/2` | Registered; partial functional scope |
| `delete_module/1` | Missing implementation/runtime support |
| `demonitor/1` | Registered; partial functional scope |
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
| `div/2` | Registered; partial functional scope |
| `dmonitor_node/3` | Missing implementation/runtime support |
| `dt_append_vm_tag_data/1` | Missing implementation/runtime support |
| `dt_get_tag_data/0` | Missing implementation/runtime support |
| `dt_get_tag/0` | Missing implementation/runtime support |
| `dt_prepend_vm_tag_data/1` | Missing implementation/runtime support |
| `dt_put_tag/1` | Missing implementation/runtime support |
| `dt_restore_tag/1` | Missing implementation/runtime support |
| `dt_spread_tag/1` | Missing implementation/runtime support |
| `element/2` | Registered; partial functional scope |
| `erase/0` | Registered; partial functional scope |
| `erase/1` | Registered; partial functional scope |
| `error/1` | Registered; partial functional scope |
| `error/2` | Missing implementation/runtime support |
| `error/3` | Missing implementation/runtime support |
| `exit_signal/2` | Missing implementation/runtime support |
| `exit_signal/3` | Missing implementation/runtime support |
| `exit/1` | Registered; partial functional scope |
| `exit/2` | Registered; partial functional scope |
| `exit/3` | Missing implementation/runtime support |
| `external_size/1` | Missing implementation/runtime support |
| `external_size/2` | Missing implementation/runtime support |
| `finish_after_on_load/2` | Missing implementation/runtime support |
| `finish_loading/1` | Missing implementation/runtime support |
| `float_to_binary/1` | Missing implementation/runtime support |
| `float_to_binary/2` | Missing implementation/runtime support |
| `float_to_list/1` | Missing implementation/runtime support |
| `float_to_list/2` | Missing implementation/runtime support |
| `float/1` | Registered; partial functional scope |
| `floor/1` | Registered; partial functional scope |
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
| `get_keys/0` | Registered; partial functional scope |
| `get_keys/1` | Registered; partial functional scope |
| `get_module_info/1` | Missing implementation/runtime support |
| `get_module_info/2` | Missing implementation/runtime support |
| `get/0` | Registered; partial functional scope |
| `get/1` | Registered; partial functional scope |
| `group_leader/0` | Missing implementation/runtime support |
| `group_leader/2` | Missing implementation/runtime support |
| `halt/0` | Missing implementation/runtime support |
| `halt/1` | Missing implementation/runtime support |
| `halt/2` | Missing implementation/runtime support |
| `has_prepared_code_on_load/1` | Missing implementation/runtime support |
| `hd/1` | Registered; partial functional scope |
| `hibernate/0` | Missing implementation/runtime support |
| `hibernate/3` | Missing implementation/runtime support |
| `insert_element/3` | Registered; partial functional scope |
| `integer_to_binary/1` | Registered; partial functional scope |
| `integer_to_binary/2` | Registered; partial functional scope |
| `integer_to_list/1` | Registered; partial functional scope |
| `integer_to_list/2` | Registered; partial functional scope |
| `iolist_size/1` | Registered; partial functional scope |
| `iolist_to_binary/1` | Registered; partial functional scope |
| `iolist_to_iovec/1` | Registered; partial functional scope |
| `is_alive/0` | Missing implementation/runtime support |
| `is_atom/1` | Registered; partial functional scope |
| `is_binary/1` | Registered; partial functional scope |
| `is_bitstring/1` | Registered; partial functional scope |
| `is_boolean/1` | Registered; partial functional scope |
| `is_builtin/3` | Missing implementation/runtime support |
| `is_float/1` | Registered; partial functional scope |
| `is_function/1` | Registered; partial functional scope |
| `is_function/2` | Registered; partial functional scope |
| `is_integer/1` | Registered; partial functional scope |
| `is_integer/3` | Registered; partial functional scope |
| `is_list/1` | Registered; partial functional scope |
| `is_map_key/2` | Registered; partial functional scope |
| `is_map/1` | Registered; partial functional scope |
| `is_number/1` | Registered; partial functional scope |
| `is_pid/1` | Registered; partial functional scope |
| `is_port/1` | Registered; partial functional scope |
| `is_process_alive/1` | Missing implementation/runtime support |
| `is_record/1` | Missing implementation/runtime support |
| `is_record/2` | Missing implementation/runtime support |
| `is_record/3` | Missing implementation/runtime support |
| `is_reference/1` | Registered; partial functional scope |
| `is_tuple/1` | Registered; partial functional scope |
| `length/1` | Registered; partial functional scope |
| `link/1` | Registered; partial functional scope |
| `link/2` | Missing implementation/runtime support |
| `list_to_atom/1` | Missing implementation/runtime support |
| `list_to_binary/1` | Registered; partial functional scope |
| `list_to_bitstring/1` | Registered; partial functional scope |
| `list_to_existing_atom/1` | Missing implementation/runtime support |
| `list_to_float/1` | Registered; partial functional scope |
| `list_to_integer/1` | Registered; partial functional scope |
| `list_to_integer/2` | Registered; partial functional scope |
| `list_to_pid/1` | Missing implementation/runtime support |
| `list_to_port/1` | Missing implementation/runtime support |
| `list_to_ref/1` | Missing implementation/runtime support |
| `list_to_tuple/1` | Registered; partial functional scope |
| `load_module/2` | Missing implementation/runtime support |
| `load_nif/2` | Missing implementation/runtime support |
| `loaded/0` | Missing implementation/runtime support |
| `localtime_to_universaltime/1` | Missing implementation/runtime support |
| `localtime_to_universaltime/2` | Missing implementation/runtime support |
| `localtime/0` | Missing implementation/runtime support |
| `make_fun/3` | Missing implementation/runtime support |
| `make_ref/0` | Registered; partial functional scope |
| `make_tuple/2` | Registered; partial functional scope |
| `make_tuple/3` | Registered; partial functional scope |
| `map_get/2` | Registered; partial functional scope |
| `map_size/1` | Registered; partial functional scope |
| `match_spec_test/3` | Missing implementation/runtime support |
| `max/2` | Registered; partial functional scope |
| `md5_final/1` | Missing implementation/runtime support |
| `md5_init/0` | Missing implementation/runtime support |
| `md5_update/2` | Missing implementation/runtime support |
| `md5/1` | Registered; partial functional scope |
| `memory/0` | Missing implementation/runtime support |
| `memory/1` | Missing implementation/runtime support |
| `min/2` | Registered; partial functional scope |
| `module_loaded/1` | Missing implementation/runtime support |
| `monitor_node/2` | Missing implementation/runtime support |
| `monitor_node/3` | Missing implementation/runtime support |
| `monitor/2` | Registered; partial functional scope |
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
| `not/1` | Registered; partial functional scope |
| `now/0` | Missing implementation/runtime support |
| `open_port/2` | Missing implementation/runtime support |
| `or/2` | Registered; partial functional scope |
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
| `process_flag/2` | Registered; partial functional scope |
| `process_flag/3` | Missing implementation/runtime support |
| `process_info/1` | Missing implementation/runtime support |
| `process_info/2` | Missing implementation/runtime support |
| `processes_iterator/0` | Missing implementation/runtime support |
| `processes_next/1` | Missing implementation/runtime support |
| `processes/0` | Missing implementation/runtime support |
| `purge_module/1` | Missing implementation/runtime support |
| `put/2` | Registered; partial functional scope |
| `raise/3` | Missing implementation/runtime support |
| `read_timer/1` | Registered; partial functional scope |
| `read_timer/2` | Missing implementation/runtime support |
| `ref_to_list/1` | Missing implementation/runtime support |
| `register/2` | Registered; partial functional scope |
| `registered/0` | Missing implementation/runtime support |
| `rem/2` | Registered; partial functional scope |
| `resume_process/1` | Missing implementation/runtime support |
| `round/1` | Registered; partial functional scope |
| `self/0` | Registered; partial functional scope |
| `send_after/3` | Registered; partial functional scope |
| `send_after/4` | Missing implementation/runtime support |
| `send_nosuspend/2` | Missing implementation/runtime support |
| `send_nosuspend/3` | Missing implementation/runtime support |
| `send/2` | Registered; partial functional scope |
| `send/3` | Missing implementation/runtime support |
| `seq_trace_info/1` | Missing implementation/runtime support |
| `seq_trace_print/1` | Missing implementation/runtime support |
| `seq_trace_print/2` | Missing implementation/runtime support |
| `seq_trace/2` | Missing implementation/runtime support |
| `set_cookie/1` | Missing implementation/runtime support |
| `set_cookie/2` | Missing implementation/runtime support |
| `set_cpu_topology/1` | Missing implementation/runtime support |
| `setelement/3` | Registered; partial functional scope |
| `setnode/2` | Missing implementation/runtime support |
| `setnode/3` | Missing implementation/runtime support |
| `size/1` | Registered; partial functional scope |
| `spawn_link/1` | Registered; partial functional scope |
| `spawn_link/2` | Missing implementation/runtime support |
| `spawn_link/3` | Missing implementation/runtime support |
| `spawn_link/4` | Missing implementation/runtime support |
| `spawn_monitor/1` | Registered; partial functional scope |
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
| `spawn/1` | Registered; partial functional scope |
| `spawn/2` | Missing implementation/runtime support |
| `spawn/3` | Missing implementation/runtime support |
| `spawn/4` | Missing implementation/runtime support |
| `split_binary/2` | Registered; partial functional scope |
| `start_timer/3` | Registered; partial functional scope |
| `start_timer/4` | Missing implementation/runtime support |
| `statistics/1` | Missing implementation/runtime support |
| `subtract/2` | Registered; partial functional scope |
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
| `throw/1` | Registered; partial functional scope |
| `time_offset/0` | Missing implementation/runtime support |
| `time_offset/1` | Missing implementation/runtime support |
| `time/0` | Missing implementation/runtime support |
| `timestamp/0` | Missing implementation/runtime support |
| `tl/1` | Registered; partial functional scope |
| `trace_delivered/1` | Missing implementation/runtime support |
| `trace_info/2` | Missing implementation/runtime support |
| `trace_pattern/2` | Missing implementation/runtime support |
| `trace_pattern/3` | Missing implementation/runtime support |
| `trace/3` | Missing implementation/runtime support |
| `trunc/1` | Registered; partial functional scope |
| `tuple_size/1` | Registered; partial functional scope |
| `tuple_to_list/1` | Registered; partial functional scope |
| `unalias/1` | Missing implementation/runtime support |
| `unique_integer/0` | Missing implementation/runtime support |
| `unique_integer/1` | Missing implementation/runtime support |
| `universaltime_to_localtime/1` | Missing implementation/runtime support |
| `universaltime_to_posixtime/1` | Missing implementation/runtime support |
| `universaltime/0` | Missing implementation/runtime support |
| `unlink/1` | Registered; partial functional scope |
| `unregister/1` | Registered; partial functional scope |
| `whereis/1` | Registered; partial functional scope |
| `xor/2` | Registered; partial functional scope |
| `yield/0` | Missing implementation/runtime support |

## Required remaining work

Atom/existing table and module-literal lifetime, float formatting/ETF/native identities/original hashes; clocks/epoch/unit/timer options; spawn/link/monitor/alias/exit and inspection; function/native-record representations/code lifecycle; ports/distribution/external identities; tracing/VM/host control need real C# state and independent original functional/error contracts. Existing local sending and timer APIs do not establish full distributed/option/resource behavior. Native storage-offset iovec partitions, nonidentical arbitrary deep recursion, reductions/resource ceilings/full stacks/error_info/exhaustive Common Test/performance parity and compiler auto-import validation remain explicit. Module status stays Partially compatible.
