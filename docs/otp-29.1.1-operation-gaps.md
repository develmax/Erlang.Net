# Пробелы компилятора по модулям Windows OTP 29.1.1

Снимок основан на [прогоне Part860](validation/part-860/otp-corpus.json). Из 1 366 файлов 93 совпали побайтово. Ниже перечислены все 1 273 файла, которые не удалось пересобрать. Снимок относится к компиляции исходников в BEAM. Исполнение готовых BEAM и C#-реализации библиотек проверяются отдельно.

Каждая строка показывает первый отказ, который вернул компилятор. Если указана только функция или модуль, конкретная неподдерживаемая операция пока не установлена. Не найденный include может указывать на недостающий заголовок или параметры сборки.

Для `inet_epmd_socket:output_handler_wait/6` форма проверена по [дизассемблированию OTP](validation/part-860/next-output-wait.txt).

## Сводка по причинам

| Этап | Причина или уровень диагностики | Файлов | Примеры модулей |
| --- | --- | ---: | --- |
| SourceRejected | Разрешение include | 71 | `ct_snmp`, `cth_log_redirect`, `test_server`, `test_server_ctrl` |
| SourceRejected | Диагностика синтаксиса без установленной причины | 43 | `asn1ct`, `asn1ct_constructed_ber_bin_v2`, `asn1ct_value`, `beam_ssa` |
| SourceRejected | Макросы | 25 | `beam_asm`, `dialyzer_cl_parse`, `dialyzer_cplt`, `dialyzer_iplt` |
| SourceRejected | Parse transforms | 23 | `dialyzer_plt`, `diameter_gen_acct_rfc6733`, `diameter_gen_base_accounting`, `diameter_gen_base_rfc3588` |
| SourceRejected | Литералы и escape-последовательности | 14 | `asn1ct_gen`, `ct_doctest`, `edoc_layout`, `group` |
| SourceRejected | Другие параметры -compile | 8 | `erlang`, `eunit_test`, `error_handler`, `reltool_fgraph` |
| SourceRejected | Формы документации | 6 | `ms_transform`, `peer`, `qlc`, `re` |
| SourceRejected | Атрибут on_load | 5 | `asn1rt_nif`, `crypto`, `pubkey_os_cacerts`, `dyntrace` |
| Unsupported | Генерация BEAM, указан только модуль | 693 | `asn1ct_constructed_per`, `asn1ct_gen_ber_bin_v2`, `asn1ct_gen_check`, `asn1ct_gen_jer` |
| Unsupported | Генерация BEAM, названа функция | 363 | `asn1_db`, `asn1ct_check`, `asn1ct_eval_ext`, `asn1ct_func` |
| Unsupported | Встраивание указанной функции | 21 | `diameter_dict_parser`, `diameter_gen`, `edoc_parser`, `disk_log_server` |
| Unsupported | Выборочный receive с guards и after | 1 | `inet_epmd_socket` |

## Сводка по приложениям

| Приложение | Совпали | Отклонены на входе | Генерация не поддерживается | Всего |
| --- | ---: | ---: | ---: | ---: |
| asn1-5.5.2 | 2 | 5 | 15 | 22 |
| common_test-1.31.2 | 0 | 7 | 42 | 49 |
| compiler-10.0.6 | 2 | 3 | 54 | 59 |
| crypto-5.10 | 0 | 1 | 1 | 2 |
| debugger-7.0 | 0 | 0 | 24 | 24 |
| dialyzer-6.0.3 | 0 | 8 | 21 | 29 |
| diameter-2.7.2 | 5 | 7 | 35 | 47 |
| edoc-1.5 | 0 | 2 | 19 | 21 |
| eldap-1.3.1 | 1 | 1 | 0 | 2 |
| erts-17.1 | 0 | 6 | 16 | 22 |
| et-1.8 | 0 | 0 | 6 | 6 |
| eunit-2.11 | 0 | 11 | 2 | 13 |
| ftp-1.2.6 | 1 | 3 | 2 | 6 |
| inets-9.8 | 4 | 3 | 56 | 63 |
| kernel-11.0.4 | 6 | 11 | 87 | 104 |
| megaco-4.9.2 | 4 | 6 | 60 | 70 |
| mnesia-4.27 | 6 | 2 | 23 | 31 |
| observer-2.19 | 17 | 2 | 25 | 44 |
| odbc-2.17.1 | 2 | 0 | 1 | 3 |
| os_mon-2.12 | 1 | 1 | 6 | 8 |
| parsetools-2.8 | 0 | 2 | 2 | 4 |
| public_key-1.21.7 | 1 | 38 | 3 | 42 |
| reltool-1.1 | 0 | 1 | 8 | 9 |
| runtime_tools-2.5 | 3 | 1 | 9 | 13 |
| sasl-4.4 | 1 | 1 | 16 | 18 |
| snmp-5.20.5 | 4 | 6 | 83 | 93 |
| ssh-6.0.6 | 1 | 2 | 40 | 43 |
| ssl-11.7.7 | 29 | 25 | 25 | 79 |
| stdlib-8.1 | 1 | 23 | 74 | 98 |
| syntax_tools-4.1.1 | 0 | 1 | 8 | 9 |
| tftp-1.3 | 1 | 0 | 7 | 8 |
| tools-4.2.3 | 0 | 3 | 13 | 16 |
| wx-2.7 | 0 | 8 | 266 | 274 |
| xmerl-2.2 | 1 | 5 | 29 | 35 |
| **Всего** | **93** | **195** | **1 078** | **1 366** |

## Все модули с отказами

### asn1-5.5.2

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `asn1_db` | Unsupported | loop/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 67135: Expected a form-terminating dot |
| `asn1ct_check` | Unsupported | check/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_constructed_ber_bin_v2` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 24429: Expected a form-terminating dot |
| `asn1ct_constructed_per` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_eval_ext` | Unsupported | transform_to_EXTERNAL1994_maps/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_func` | Unsupported | call_gen/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_gen` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 30205: Unsupported expression '~' |
| `asn1ct_gen_ber_bin_v2` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_gen_check` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_gen_jer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_gen_per` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_imm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_name` | Unsupported | start/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_parser2` | Unsupported | parse/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_partial_decode` | Unsupported | exclusive_decode_command/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_pretty_format` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `asn1ct_tok` | Unsupported | process/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `asn1ct_value` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 6501: Expected ')', found '(' |
| `asn1rt_nif` | SourceRejected | — | Атрибут on_load | ERL003 at 969: Attribute 'on_load' is not supported yet |

### common_test-1.31.2

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `ct` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_config` | Unsupported | start/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_config_plain` | Unsupported | read_config_terms/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_config_xml` | Unsupported | list_to_term/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_conn_log_h` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_cover` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_default_gl` | Unsupported | do_start/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_doctest` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 25336: Unsupported expression '~' |
| `ct_event` | Unsupported | handle_event/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_framework` | Unsupported | init_tc/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_ftp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_gen_conn` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_groups` | Unsupported | find_groups/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_hooks` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_hooks_lock` | Unsupported | handle_call/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_logs` | Unsupported | close/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_make` | Unsupported | transform/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_master` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_master_event` | Unsupported | stop/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_master_logs` | Unsupported | make_dirname/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_master_status` | Unsupported | handle_call/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_netconfc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_proper_ext` | Unsupported | existing_atom/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_property_test` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_quickcheck_ext` | Unsupported | safe_tuple/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_release_test` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_repeat` | Unsupported | loop_test/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_rpc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_run` | Unsupported | script_start/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_slave` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_snmp` | SourceRejected | — | Разрешение include | ERL003 at 5948: Could not resolve include 'snmp_types.hrl'. |
| `ct_ssh` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_suite` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_telnet` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_telnet_client` | Unsupported | open/6 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ct_testspec` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ct_util` | Unsupported | start/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `cte_track` | Unsupported | handle_event/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `cth_conn_log` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `cth_log_redirect` | SourceRejected | — | Разрешение include | ERL003 at 1437: Could not resolve include '../../kernel/src/logger_internal.hrl'. |
| `cth_surefire` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl2html2` | Unsupported | parse_preprocessed_file/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `test_server` | SourceRejected | — | Разрешение include | ERL003 at 2398: Could not resolve include 'test_server_internal.hrl'. |
| `test_server_ctrl` | SourceRejected | — | Разрешение include | ERL003 at 3699: Could not resolve include 'test_server_internal.hrl'. |
| `test_server_gl` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `test_server_io` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `test_server_node` | SourceRejected | — | Разрешение include | ERL003 at 1169: Could not resolve include 'test_server_internal.hrl'. |
| `test_server_sup` | SourceRejected | — | Разрешение include | ERL003 at 1535: Could not resolve include 'test_server_internal.hrl'. |
| `unix_telnet` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### compiler-10.0.6

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `beam_a` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_asm` | SourceRejected | — | Макросы | ERL003 at 10150: Macro 'COMPILER_VSN' is undefined or requires an unsupported preprocessing directive. |
| `beam_block` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_bounds` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_call_types` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_clean` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_core_to_ssa` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_dict` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_disasm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_doc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_flatten` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_jump` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_listing` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 5040: Expected atom |
| `beam_ssa_alias` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_bc_size` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_bool` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_bsm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_check` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_codegen` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_dead` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_destructive_update` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_lint` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_opt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_pp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_pre_codegen` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_recv` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_share` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_ss` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_throw` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_ssa_type` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_trim` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_types` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_utils` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_validator` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `beam_z` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `cerl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `cerl_clauses` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `cerl_inline` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `cerl_trees` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `compile` | SourceRejected | — | Разрешение include | ERL003 at 7740: Could not resolve include 'erl_compile.hrl'. |
| `core_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `core_lint` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `core_parse` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `core_pp` | Unsupported | maybe_anno/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `core_scan` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `rec_env` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys_core_alias` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys_core_bsm` | Unsupported | function/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `sys_core_fold` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys_core_fold_lists` | Unsupported | call/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `sys_core_inline` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys_core_prepare` | Unsupported | module/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `sys_coverage` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys_messages` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys_pre_attributes` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `v3_core` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### crypto-5.10

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `crypto` | SourceRejected | — | Атрибут on_load | ERL003 at 27986: Attribute 'on_load' is not supported yet |
| `crypto_ec_curves` | Unsupported | curves/0 | Генерация BEAM, названа функция | Конкретная форма не названа |

### debugger-7.0

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `dbg_debugged` | Unsupported | eval/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_icmd` | Unsupported | break_p/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_idb` | Unsupported | insert/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_ieval` | Unsupported | check_exit_msg/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_iload` | Unsupported | store_module/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_iserver` | Unsupported | handle_call/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_istk` | Unsupported | push/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_break` | Unsupported | init/6 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_break_win` | Unsupported | create_win/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_code` | Unsupported | add_break_to_code/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_filedialog_win` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_interpret` | Unsupported | format_error/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_mon` | Unsupported | init2/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_mon_win` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dbg_wx_settings` | Unsupported | open_win/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_src_view` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dbg_wx_trace` | Unsupported | loop/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_trace_win` | Unsupported | stop/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_view` | Unsupported | start/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_win` | Unsupported | create_menu_item/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg_wx_winman` | Unsupported | update_windows_menu/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `debugger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `i` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `int` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### dialyzer-6.0.3

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `cerl_prettypr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_analysis_callgraph` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 8832: Expected a form-terminating dot |
| `dialyzer_behaviours` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_callgraph` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_cl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_cl_parse` | SourceRejected | — | Макросы | ERL003 at 16627: Macro 'VSN' is undefined or requires an unsupported preprocessing directive. |
| `dialyzer_clean_core` | Unsupported | clean/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dialyzer_codeserver` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_contracts` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_coordinator` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_cplt` | SourceRejected | — | Макросы | ERL003 at 6118: Macro 'VSN' is undefined or requires an unsupported preprocessing directive. |
| `dialyzer_dataflow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_dep` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_dot` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_incremental` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_iplt` | SourceRejected | — | Макросы | ERL003 at 7534: Macro 'VSN' is undefined or requires an unsupported preprocessing directive. |
| `dialyzer_options` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_plt` | SourceRejected | — | Parse transforms | ERL003 at 11988: Unsupported -compile option: {parse_transform,ms_transform} |
| `dialyzer_succ_typings` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_timing` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_typegraph` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_typesig` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dialyzer_utils` | SourceRejected | — | Разрешение include | ERL003 at 1974: Could not resolve include '../../compiler/src/core_parse.hrl'. |
| `dialyzer_worker` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_bif_types` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_types` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 151328: Expected a form-terminating dot |
| `typer` | SourceRejected | — | Макросы | ERL003 at 7217: Macro 'VSN' is undefined or requires an unsupported preprocessing directive. |
| `typer_core` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### diameter-2.7.2

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `diameter` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_app` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_callback` | Unsupported | cb/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_capx` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_codec` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_codegen` | Unsupported | do_write/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_config` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_dbg` | Unsupported | table/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_dict_parser` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `diameter_dict_scanner` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 1486: Expected atom |
| `diameter_dict_util` | Unsupported | fmt/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_dist` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_etcp` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_exprecs` | Unsupported | parse_transform/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_gen` | Unsupported | incr/8 | Встраивание указанной функции | Targeted inline lowering is not supported for incr/8. |
| `diameter_gen_acct_rfc6733` | SourceRejected | — | Parse transforms | ERL003 at 238: Unsupported -compile option: {parse_transform,diameter_exprecs} |
| `diameter_gen_base_accounting` | SourceRejected | — | Parse transforms | ERL003 at 241: Unsupported -compile option: {parse_transform,diameter_exprecs} |
| `diameter_gen_base_rfc3588` | SourceRejected | — | Parse transforms | ERL003 at 238: Unsupported -compile option: {parse_transform,diameter_exprecs} |
| `diameter_gen_base_rfc6733` | SourceRejected | — | Parse transforms | ERL003 at 238: Unsupported -compile option: {parse_transform,diameter_exprecs} |
| `diameter_gen_doic_rfc7683` | SourceRejected | — | Parse transforms | ERL003 at 238: Unsupported -compile option: {parse_transform,diameter_exprecs} |
| `diameter_gen_relay` | SourceRejected | — | Parse transforms | ERL003 at 231: Unsupported -compile option: {parse_transform,diameter_exprecs} |
| `diameter_info` | Unsupported | format/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_make` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_misc_sup` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_peer` | Unsupported | match/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_peer_fsm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_reg` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_sctp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_sctp_sup` | Unsupported | start_child/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_service` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_session` | Unsupported | sequence/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_stats` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_sup` | Unsupported | ets_new/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_sync` | Unsupported | info/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_tcp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_tcp_sup` | Unsupported | start_child/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_traffic` | Unsupported | make_recvdata/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_transport` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `diameter_transport_sup` | Unsupported | start_child/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_types` | Unsupported | OctetString/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `diameter_watchdog` | Unsupported | start/2 | Генерация BEAM, названа функция | Конкретная форма не названа |

### edoc-1.5

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `edoc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_cli` | Unsupported | main/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `edoc_data` | Unsupported | function_filter/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `edoc_doclet` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_doclet_chunks` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_doclet_markdown` | Unsupported | overview/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `edoc_extract` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_html_to_markdown` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 1688: Expected ')', found ',' |
| `edoc_layout` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 22341: Hex, control and octal escapes are not implemented yet |
| `edoc_layout_chunks` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_macros` | Unsupported | check_defs/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `edoc_parser` | Unsupported | yeccpars2_6_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_6_/1. |
| `edoc_refs` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_report` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_run` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_scanner` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_specs` | Unsupported | type/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `edoc_tags` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edoc_types` | Unsupported | to_xml/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `edoc_wiki` | Unsupported | expand/3 | Генерация BEAM, названа функция | Конкретная форма не названа |

### eldap-1.3.1

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `eldap` | SourceRejected | — | Разрешение include | ERL003 at 4768: Could not resolve include 'ELDAPv3.hrl'. |

### erts-17.1

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `atomics` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `counters` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_init` | Unsupported | if_loaded/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_prim_loader` | SourceRejected | — | Разрешение include | ERL003 at 2910: Could not resolve include 'inet_boot.hrl'. |
| `erl_tracer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erlang` | SourceRejected | — | Другие параметры -compile | ERL003 at 4085: Unsupported -compile option: no_auto_import_types |
| `erts_code_purger` | Unsupported | handle_request/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erts_dirty_process_signal_handler` | Unsupported | handle_request/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erts_internal` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 4600: Expected '(', found ':' |
| `erts_literal_area_collector` | Unsupported | msg_loop/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erts_trace_cleaner` | Unsupported | loop/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `init` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `persistent_term` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `prim_buffer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `prim_eval` | Unsupported | receive/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `prim_file` | SourceRejected | — | Разрешение include | ERL003 at 1668: Could not resolve include 'file_int.hrl'. |
| `prim_inet` | SourceRejected | — | Разрешение include | ERL003 at 1909: Could not resolve include 'inet_sctp.hrl'. |
| `prim_net` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `prim_socket` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `prim_zip` | SourceRejected | — | Разрешение include | ERL003 at 1095: Could not resolve include 'zip_internal.hrl'. |
| `socket_registry` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `zlib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### et-1.8

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `et` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `et_collector` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `et_selector` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `et_viewer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `et_wx_contents_viewer` | Unsupported | parse_opt/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `et_wx_viewer` | Unsupported | start_link/1 | Генерация BEAM, названа функция | Конкретная форма не названа |

### eunit-2.11

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `eunit` | SourceRejected | — | Parse transforms | ERL003 at 11876: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_autoexport` | Unsupported | parse_transform/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `eunit_data` | SourceRejected | — | Parse transforms | ERL003 at 35803: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_lib` | SourceRejected | — | Parse transforms | ERL003 at 23945: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_listener` | SourceRejected | — | Parse transforms | ERL003 at 10139: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_proc` | SourceRejected | — | Parse transforms | ERL003 at 30314: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_serial` | SourceRejected | — | Parse transforms | ERL003 at 11532: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_server` | SourceRejected | — | Parse transforms | ERL003 at 14253: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_striptests` | Unsupported | form/6 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `eunit_surefire` | SourceRejected | — | Parse transforms | ERL003 at 24973: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_test` | SourceRejected | — | Другие параметры -compile | ERL003 at 1575: Unsupported -compile option: nowarn_unexported_function |
| `eunit_tests` | SourceRejected | — | Parse transforms | ERL003 at 5865: Unsupported -compile option: {parse_transform,eunit_autoexport} |
| `eunit_tty` | SourceRejected | — | Parse transforms | ERL003 at 12108: Unsupported -compile option: {parse_transform,eunit_autoexport} |

### ftp-1.2.6

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `ftp` | SourceRejected | — | Разрешение include | ERL003 at 3968: Could not resolve include 'ftp_internal.hrl'. |
| `ftp_internal` | SourceRejected | — | Разрешение include | ERL003 at 1535: Could not resolve include 'ftp_internal.hrl'. |
| `ftp_progress` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ftp_response` | SourceRejected | — | Разрешение include | ERL003 at 898: Could not resolve include 'ftp_internal.hrl'. |
| `ftp_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### inets-9.8

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `http_chunk` | Unsupported | encode/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `http_request` | Unsupported | key_value/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `http_response` | Unsupported | fill_headers/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `http_transport` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `http_uri` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `http_util` | Unsupported | timestamp/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpc_cookie` | Unsupported | open_db/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc_handler` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc_handler_sup` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc_manager` | Unsupported | which_sessions_order/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc_profile_sup` | Unsupported | start_child/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc_request` | Unsupported | send/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpc_response` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpc_sup` | Unsupported | child_specs/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpd_acceptor` | Unsupported | start_link/7 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_cgi` | Unsupported | parse_headers/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_conf` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpd_custom` | Unsupported | customize_headers/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_custom_api` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpd_esi` | Unsupported | parse_headers/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_example` | Unsupported | do_get/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_file` | Unsupported | handle_error/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_instance_sup` | Unsupported | start_link/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_log` | Unsupported | access_entry/8 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_logger` | Unsupported | error_report/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_manager` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_misc_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpd_request` | Unsupported | parse_version/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_request_handler` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_response` | Unsupported | send_header/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_script_env` | Unsupported | which_connect_addr/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_socket` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `httpd_sup` | Unsupported | child_specs/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `httpd_util` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inets` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inets_service` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inets_sup` | Unsupported | default_profile/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inets_trace` | Unsupported | enable2/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_actions` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mod_alias` | SourceRejected | — | Разрешение include | ERL003 at 1027: Could not resolve include 'inets_internal.hrl'. |
| `mod_auth` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mod_auth_dets` | Unsupported | get_user/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_auth_mnesia` | Unsupported | add_user/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_auth_plain` | Unsupported | get_user/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_auth_server` | Unsupported | stop/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_cgi` | SourceRejected | — | Разрешение include | ERL003 at 942: Could not resolve include 'http_internal.hrl'. |
| `mod_dir` | Unsupported | format/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_disk_log` | Unsupported | store/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_esi` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mod_get` | SourceRejected | — | Разрешение include | ERL003 at 820: Could not resolve include 'inets_internal.hrl'. |
| `mod_head` | Unsupported | do_head/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_log` | Unsupported | create_log/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_range` | Unsupported | send_multipart_start/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_responsecontrol` | Unsupported | send_return_value/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_security` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mod_security_server` | Unsupported | handle_call/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mod_trace` | Unsupported | generate_trace_response_body/2 | Генерация BEAM, названа функция | Конкретная форма не названа |

### kernel-11.0.4

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `application` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 4421: Expected atom |
| `application_controller` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `application_master` | Unsupported | handle_msg/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `application_starter` | Unsupported | start_apps/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `auth` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `code` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `code_server` | SourceRejected | — | Parse transforms | ERL003 at 48048: Unsupported -compile option: {parse_transform,ms_transform} |
| `data_publisher` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `disk_log` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `disk_log_1` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `disk_log_server` | Unsupported | do_get_log_pid/1 | Встраивание указанной функции | Targeted inline lowering is not supported for do_get_log_pid/1. |
| `dist_ac` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dist_util` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_boot_server` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_compile_server` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_ddll` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_debugger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_distribution` | Unsupported | stop/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_epmd` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_erts_errors` | Unsupported | format_bs_fail/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_kernel_errors` | Unsupported | format_code_error/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erpc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `error_handler` | SourceRejected | — | Другие параметры -compile | ERL003 at 1464: Unsupported -compile option: no_module_opt |
| `error_logger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erts_debug` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `file` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `file_io_server` | Unsupported | format_error/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `file_server` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen_sctp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen_tcp` | SourceRejected | — | Макросы | ERL003 at 25759: Function context macros require a function head without macro expansion. |
| `gen_tcp_socket` | SourceRejected | — | Разрешение include | ERL003 at 1482: Could not resolve include 'socket_int.hrl'. |
| `gen_udp` | SourceRejected | — | Макросы | ERL003 at 11958: Function context macros require a function head without macro expansion. |
| `gen_udp_socket` | SourceRejected | — | Разрешение include | ERL003 at 1413: Could not resolve include 'socket_int.hrl'. |
| `global` | SourceRejected | — | Parse transforms | ERL003 at 134116: Unsupported -compile option: {parse_transform,ms_transform} |
| `global_group` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `global_search` | Unsupported | start/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `group` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 38326: Hex, control and octal escapes are not implemented yet |
| `group_history` | Unsupported | add/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `heart` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet` | SourceRejected | — | Макросы | ERL003 at 21907: Function context macros require a function head without macro expansion. |
| `inet_config` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_db` | Unsupported | dns_rr_match_tm_ttl_cnt/3 | Встраивание указанной функции | Targeted inline lowering is not supported for dns_rr_match_tm_ttl_cnt/3. |
| `inet_dns` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_dns_tsig` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_epmd_dist` | Unsupported | connect/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet_epmd_socket` | Unsupported | output_handler_wait/6 | Выборочный receive с guards и after | Следующий участок генерации. OTP выбирает ветку по сообщениям dist_data, dist_tick и $socket, проверяет guards, вызывает продолжение хвостовым вызовом или отправляет tick по таймауту. |
| `inet_gethost_native` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_hosts` | Unsupported | make_hostent/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet_parse` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_res` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_sctp` | Unsupported | connect_get_assoc/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet_tcp` | Unsupported | connect/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet_tcp_dist` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `inet_udp` | Unsupported | send/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet6_sctp` | Unsupported | open/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet6_tcp` | Unsupported | connect/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `inet6_udp` | Unsupported | send/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `kernel` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `kernel_config` | Unsupported | sync_nodes/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `kernel_refc` | Unsupported | handle_call/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `local_tcp` | Unsupported | do_connect/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `local_udp` | Unsupported | open/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_backend` | Unsupported | call_handlers/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger_config` | Unsupported | get_primary_level/0 | Встраивание указанной функции | Targeted inline lowering is not supported for get_primary_level/0. |
| `logger_disk_log_h` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_filters` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_formatter` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_h_common` | Unsupported | adding_handler/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger_handler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_handler_watcher` | Unsupported | register_handler/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger_olp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_proxy` | Unsupported | handle_info/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger_server` | Unsupported | set_module_level/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger_simple_h` | Unsupported | adding_handler/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `logger_std_h` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `logger_sup` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `net` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `net_adm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `net_kernel` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `os` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 20664: Hex, control and octal escapes are not implemented yet |
| `pg` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `prim_tty` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ram_file` | Unsupported | open/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `raw_file_io` | Unsupported | open_1/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `raw_file_io_compressed` | Unsupported | open_layer/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `raw_file_io_deflate` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `raw_file_io_delayed` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `raw_file_io_inflate` | Unsupported | opening/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `raw_file_io_list` | Unsupported | make_public_fd/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `rpc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `seq_trace` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `socket` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `standard_error` | Unsupported | io_request/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `trace` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `user_drv` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `user_sup` | Unsupported | wait_for_user_p/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `wrap_log_reader` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### megaco-4.9.2

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `megaco` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_ber_media_gateway_control_v1` | Unsupported | decode_inc_disp/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_ber_media_gateway_control_v2` | Unsupported | decode_inc_disp/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_ber_media_gateway_control_v3` | Unsupported | decode_inc_disp/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_encoder_lib` | Unsupported | version_of/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_name_resolver_v1` | Unsupported | decode/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_name_resolver_v2` | Unsupported | decode/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_name_resolver_v3` | Unsupported | decode/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_term_id` | Unsupported | encode/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_term_id_gen` | Unsupported | encode_without_wildcards/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_transformer_v1` | Unsupported | verify_constraint/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_transformer_v2` | Unsupported | verify_constraint/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_binary_transformer_v3` | Unsupported | verify_constraint/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_codec_meas` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_codec_mstone_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_codec_mstone1` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_codec_mstone2` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_codec_transform` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_compact_text_encoder` | Unsupported | l2i/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_compact_text_encoder_v1` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 92040: Expected ']', found '(' |
| `megaco_compact_text_encoder_v2` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 95366: Expected ']', found '(' |
| `megaco_compact_text_encoder_v3` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 142405: Expected ']', found '(' |
| `megaco_config` | Unsupported | user_info/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_digit_map` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_edist_compress` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_encoder` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_erl_dist_encoder` | Unsupported | encode_message/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_erl_dist_encoder_mc` | Unsupported | e/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_filter` | Unsupported | raw_filter/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_flex_scanner` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_flex_scanner_handler` | Unsupported | handle_info/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_messenger` | Unsupported | cleanup/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_messenger_misc` | Unsupported | encode_trans_request/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_misc_sup` | Unsupported | start_permanent_worker/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_monitor` | Unsupported | apply_at_exit/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_per_media_gateway_control_v1` | Unsupported | enc_Value/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_per_media_gateway_control_v2` | Unsupported | enc_Value/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_per_media_gateway_control_v3` | Unsupported | enc_Value/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_pretty_text_encoder` | Unsupported | l2i/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_pretty_text_encoder_v1` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 92177: Expected ']', found '(' |
| `megaco_pretty_text_encoder_v2` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 96025: Expected ']', found '(' |
| `megaco_pretty_text_encoder_v3` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 142959: Expected ']', found '(' |
| `megaco_sdp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_stats` | Unsupported | get_stats/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_sup` | Unsupported | start/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_tcp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_tcp_accept` | Unsupported | net_accept/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_tcp_accept_sup` | Unsupported | start_child/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_tcp_connection` | Unsupported | handle_info/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_tcp_connection_sup` | Unsupported | start_child/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_tcp_sup` | Unsupported | start_accept_child/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_text_mini_decoder` | Unsupported | decode_message/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_text_mini_parser` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `megaco_text_parser_v1` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `megaco_text_parser_v2` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `megaco_text_parser_v3` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `megaco_text_scanner` | Unsupported | scan/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_timer` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_trans_sender` | Unsupported | stop/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_trans_sup` | Unsupported | start/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_transport` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_udp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_udp_server` | Unsupported | activate/1 | Встраивание указанной функции | Targeted inline lowering is not supported for activate/1. |
| `megaco_udp_sup` | Unsupported | start_child/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `megaco_user` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `megaco_user_default` | Unsupported | handle_trans_request/3 | Генерация BEAM, названа функция | Конкретная форма не названа |

### mnesia-4.27

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `mnesia` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_backup` | Unsupported | open_read/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_bup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_checkpoint` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_controller` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_dumper` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_event` | Unsupported | report_fatal/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_frag` | Unsupported | search_first/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_frag_hash` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_index` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_kernel_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_late_loader` | Unsupported | start/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_lib` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 16737: Expected a form-terminating dot |
| `mnesia_loader` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 26026: Expected 'end', found '(' |
| `mnesia_locker` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_log` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_monitor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_recover` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_rpc` | Unsupported | start/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_schema` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_snmp_hook` | Unsupported | build_table/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_subscr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_sup` | Unsupported | init/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `mnesia_text` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `mnesia_tm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### observer-2.19

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `cdv_info_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `cdv_multi_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `cdv_table_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `cdv_virtual_list_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `cdv_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `crashdump_viewer` | SourceRejected | — | Parse transforms | ERL003 at 116353: Unsupported -compile option: {parse_transform,ms_transform} |
| `etop` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `etop_tr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `etop_txt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `observer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `observer_alloc_wx` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `observer_app_wx` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `observer_html_lib` | Unsupported | expandable_term_body/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_lib` | Unsupported | interval_dialog/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_perf_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_port_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_pro_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_procinfo` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_sock_wx` | Unsupported | do_init/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_sys_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_trace_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_traceoptions_wx` | Unsupported | process_trace/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_tv_table` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `observer_tv_wx` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `observer_wx` | Unsupported | setup/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ttb` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ttb_et` | SourceRejected | — | Разрешение include | ERL003 at 777: Could not resolve include 'et.hrl'. |

### odbc-2.17.1

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `odbc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### os_mon-2.12

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `cpu_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `disksup` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 13592: Hex, control and octal escapes are not implemented yet |
| `memsup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `nteventlog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `os_mon` | Unsupported | call/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `os_mon_sysinfo` | Unsupported | get_disk_info/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `os_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### parsetools-2.8

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `leex` | SourceRejected | — | Разрешение include | ERL003 at 7763: Could not resolve include 'erl_compile.hrl'. |
| `yecc` | SourceRejected | — | Разрешение include | ERL003 at 17339: Could not resolve include 'erl_compile.hrl'. |
| `yeccparser` | Unsupported | yeccpars2_6_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_6_/1. |
| `yeccscan` | Unsupported | scan/3 | Генерация BEAM, названа функция | Конкретная форма не названа |

### public_key-1.21.7

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `AlgorithmInformation-2009` | SourceRejected | — | Разрешение include | ERL003 at 277: Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `AttributeCertificateVersion1-2009` | SourceRejected | — | Разрешение include | ERL003 at 293: Could not resolve include 'AttributeCertificateVersion1-2009.hrl'. |
| `CMSAesRsaesOaep-2009` | SourceRejected | — | Разрешение include | ERL003 at 267: Could not resolve include 'CMSAesRsaesOaep-2009.hrl'. |
| `CryptographicMessageSyntax-2009` | SourceRejected | — | Разрешение include | ERL003 at 289: Could not resolve include 'CryptographicMessageSyntax-2009.hrl'. |
| `CryptographicMessageSyntaxAlgorithms-2009` | SourceRejected | — | Разрешение include | ERL003 at 309: Could not resolve include 'CryptographicMessageSyntaxAlgorithms-2009.hrl'. |
| `DSS` | SourceRejected | — | Разрешение include | ERL003 at 233: Could not resolve include 'DSS.hrl'. |
| `ECPrivateKey` | SourceRejected | — | Разрешение include | ERL003 at 251: Could not resolve include 'ECPrivateKey.hrl'. |
| `EnrollmentMessageSyntax-2009` | SourceRejected | — | Разрешение include | ERL003 at 283: Could not resolve include 'EnrollmentMessageSyntax-2009.hrl'. |
| `OCSP-2024-08` | SourceRejected | — | Разрешение include | ERL003 at 251: Could not resolve include 'OCSP-2024-08.hrl'. |
| `OTP-PKIX` | SourceRejected | — | Разрешение include | ERL003 at 243: Could not resolve include 'OTP-PKIX.hrl'. |
| `OTP-PKIX-Relaxed` | SourceRejected | — | Разрешение include | ERL003 at 259: Could not resolve include 'OTP-PKIX-Relaxed.hrl'. |
| `PKCS-1` | SourceRejected | — | Разрешение include | ERL003 at 239: Could not resolve include 'PKCS-1.hrl'. |
| `PKCS-10` | SourceRejected | — | Разрешение include | ERL003 at 241: Could not resolve include 'PKCS-10.hrl'. |
| `PKCS-3` | SourceRejected | — | Разрешение include | ERL003 at 239: Could not resolve include 'PKCS-3.hrl'. |
| `PKCS-FRAME` | Unsupported | enc_PBMAC1-params_keyDerivationFunc/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `PKIX-CommonTypes-2009` | SourceRejected | — | Разрешение include | ERL003 at 269: Could not resolve include 'PKIX-CommonTypes-2009.hrl'. |
| `PKIX-X400Address-2009` | SourceRejected | — | Разрешение include | ERL003 at 269: Could not resolve include 'PKIX-X400Address-2009.hrl'. |
| `PKIX1-PSS-OAEP-Algorithms-2009` | SourceRejected | — | Разрешение include | ERL003 at 287: Could not resolve include 'PKIX1-PSS-OAEP-Algorithms-2009.hrl'. |
| `PKIX1Explicit-2009` | SourceRejected | — | Разрешение include | ERL003 at 263: Could not resolve include 'PKIX1Explicit-2009.hrl'. |
| `PKIX1Implicit-2009` | SourceRejected | — | Разрешение include | ERL003 at 263: Could not resolve include 'PKIX1Implicit-2009.hrl'. |
| `PKIXAlgs-2009` | SourceRejected | — | Разрешение include | ERL003 at 253: Could not resolve include 'PKIXAlgs-2009.hrl'. |
| `PKIXAttributeCertificate-2009` | SourceRejected | — | Разрешение include | ERL003 at 285: Could not resolve include 'PKIXAttributeCertificate-2009.hrl'. |
| `PKIXCMP-2023` | SourceRejected | — | Разрешение include | ERL003 at 251: Could not resolve include 'PKIXCMP-2023.hrl'. |
| `PKIXCRMF-2009` | SourceRejected | — | Разрешение include | ERL003 at 253: Could not resolve include 'PKIXCRMF-2009.hrl'. |
| `pubkey_cert` | SourceRejected | — | Разрешение include | ERL003 at 1550: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `pubkey_cert_records` | SourceRejected | — | Разрешение include | ERL003 at 761: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `pubkey_crl` | SourceRejected | — | Разрешение include | ERL003 at 748: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `pubkey_ocsp` | SourceRejected | — | Разрешение include | ERL003 at 749: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `pubkey_os_cacerts` | SourceRejected | — | Атрибут on_load | ERL003 at 940: Attribute 'on_load' is not supported yet |
| `pubkey_pbe` | SourceRejected | — | Разрешение include | ERL003 at 846: Could not resolve include 'PKCS-1.hrl'. |
| `pubkey_pem` | Unsupported | strip_tail_whitespace/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `pubkey_policy_tree` | SourceRejected | — | Разрешение include | ERL003 at 755: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `pubkey_ssh` | Unsupported | dh_gex_group/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `pubkey_translation` | SourceRejected | — | Разрешение include | ERL003 at 787: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `public_key` | SourceRejected | — | Разрешение include | ERL003 at 1308: In include 'public_key_internal.hrl': Could not resolve include 'AlgorithmInformation-2009.hrl'. |
| `RFC5639` | SourceRejected | — | Разрешение include | ERL003 at 241: Could not resolve include 'RFC5639.hrl'. |
| `Safecurves-pkix-18` | SourceRejected | — | Разрешение include | ERL003 at 263: Could not resolve include 'Safecurves-pkix-18.hrl'. |
| `SecureMimeMessageV3dot1-2009` | SourceRejected | — | Разрешение include | ERL003 at 283: Could not resolve include 'SecureMimeMessageV3dot1-2009.hrl'. |
| `SLH-DSA-Module-2024` | SourceRejected | — | Разрешение include | ERL003 at 265: Could not resolve include 'SLH-DSA-Module-2024.hrl'. |
| `X509-ML-DSA-2025` | SourceRejected | — | Разрешение include | ERL003 at 259: Could not resolve include 'X509-ML-DSA-2025.hrl'. |
| `X509-ML-KEM-2025` | SourceRejected | — | Разрешение include | ERL003 at 259: Could not resolve include 'X509-ML-KEM-2025.hrl'. |

### reltool-1.1

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `reltool` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `reltool_app_win` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `reltool_fgraph` | SourceRejected | — | Другие параметры -compile | ERL003 at 917: Unsupported -compile option: inline |
| `reltool_fgraph_win` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `reltool_mod_win` | Unsupported | do_init/6 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `reltool_server` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `reltool_sys_win` | Unsupported | do_init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `reltool_target` | Unsupported | do_gen_config/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `reltool_utils` | Unsupported | split_app_name/1 | Генерация BEAM, названа функция | Конкретная форма не названа |

### runtime_tools-2.5

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `appmon_info` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `dbg` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dyntrace` | SourceRejected | — | Атрибут on_load | ERL003 at 5331: Attribute 'on_load' is not supported yet |
| `instrument` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `msacc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `observer_backend` | Unsupported | get_table2/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `scheduler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `system_information` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ttb_autostart` | Unsupported | read_config/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `versions` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### sasl-4.4

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `alarm_handler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erlsrv` | Unsupported | run_erlsrv/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `format_lib_supp` | Unsupported | print_data/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `misc_supp` | Unsupported | multi_map/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `rb` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `rb_format_supp` | Unsupported | print/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `release_handler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `release_handler_1` | Unsupported | check_script/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `sasl` | Unsupported | start/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `sasl_report` | Unsupported | supervisor_format/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `sasl_report_file_h` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `systools` | SourceRejected | — | Разрешение include | ERL003 at 1795: Could not resolve include 'erl_compile.hrl'. |
| `systools_lib` | Unsupported | get_dirs/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `systools_make` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `systools_rc` | Unsupported | translate_scripts/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `systools_relup` | Unsupported | done_mk_relup/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `target_system` | Unsupported | create/2 | Генерация BEAM, названа функция | Конкретная форма не названа |

### snmp-5.20.5

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `ex1` | Unsupported | friends_table/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_app` | Unsupported | start_agent/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp_app_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_community_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_conf` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_config` | SourceRejected | — | Разрешение include | ERL003 at 846: Could not resolve include 'snmp_internal.hrl'. |
| `snmp_ex2_manager` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_ex2_simple_standard_test` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_framework_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_generic` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_generic_mnesia` | Unsupported | table_func/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp_index` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_log` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_mini_mib` | Unsupported | make_mini_mib_elem/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp_misc` | SourceRejected | — | Разрешение include | ERL003 at 887: Could not resolve include 'snmpc_misc.hrl'. |
| `snmp_note_store` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp_notification_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_pdus` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_shadow_table` | Unsupported | delete_time_stamp_table/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp_standard_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_target_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_user_based_sm_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_usm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmp_verbosity` | Unsupported | print1/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmp_view_based_acm_mib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_acm` | Unsupported | add_res/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_agent` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_agent_sup` | Unsupported | start_link/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_app` | Unsupported | convert_config/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_authentication_service` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_conf` | SourceRejected | — | Разрешение include | ERL003 at 949: Could not resolve include 'snmp_internal.hrl'. |
| `snmpa_discovery_handler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_error` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_error_io` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_error_logger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_error_report` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_get` | Unsupported | empty_pdu/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_get_lib` | Unsupported | split_vbs_gb/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_get_mechanism` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_local_db` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_mib` | SourceRejected | — | Макросы | ERL003 at 25372: Macro 'default_verbosity' is undefined or requires an unsupported preprocessing directive. |
| `snmpa_mib_data` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_mib_data_tttn` | Unsupported | load_mib/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_mib_lib` | Unsupported | print_variables/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_mib_storage` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_mib_storage_dets` | Unsupported | write/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_mib_storage_ets` | Unsupported | write/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_mib_storage_mnesia` | Unsupported | read/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_misc_sup` | Unsupported | start_link/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_mpd` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_net_if` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_net_if_filter` | Unsupported | accept_send_pdu/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_network_interface` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_network_interface_filter` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_notification_delivery_info_receiver` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_notification_filter` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_set` | Unsupported | set_phase_one_subagents/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_set_lib` | Unsupported | is_varbinds_ok/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_set_mechanism` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_supervisor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_svbl` | Unsupported | partition/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_symbolic_store` | SourceRejected | — | Макросы | ERL003 at 22404: Macro 'default_verbosity' is undefined or requires an unsupported preprocessing directive. |
| `snmpa_target_cache` | Unsupported | targets/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_trap` | Unsupported | localise_type/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpa_usm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpa_vacm` | Unsupported | unique_table_name/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpc_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpc_mib_gram` | Unsupported | yeccpars2_3_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_3_/1. |
| `snmpc_mib_to_hrl` | Unsupported | insert_oids2/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpc_misc` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 3350: Expected ')', found '(' |
| `snmpc_tok` | Unsupported | getLine/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_conf` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_config` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_misc_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_mpd` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_net_if` | Unsupported | send_pdu/7 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpm_net_if_mt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_network_interface` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_network_interface_filter` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_server` | Unsupported | do_init/0 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpm_server_sup` | Unsupported | start_link/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpm_supervisor` | Unsupported | stop/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpm_user` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `snmpm_user_default` | Unsupported | handle_error/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `snmpm_usm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### ssh-6.0.6

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `ssh` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_acceptor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_acceptor_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_agent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_app` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssh_auth` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_bits` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_channel` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_channel_sup` | Unsupported | start_child/7 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssh_cli` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_client_channel` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_client_key_api` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_connection` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_connection_handler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_connection_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_daemon_channel` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_dbg` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_file` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_fsm_kexinit` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_fsm_userauth_client` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_fsm_userauth_server` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_info` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_io` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_lsocket` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_lsocket_sup` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssh_message` | SourceRejected | — | Макросы | ERL003 at 35404: Function context macros require a function head without macro expansion. |
| `ssh_options` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_server_channel` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_server_key_api` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_sftp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_sftpd` | SourceRejected | — | Макросы | ERL003 at 10747: Function context macros require a function head without macro expansion. |
| `ssh_sftpd_file` | Unsupported | close/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssh_sftpd_file_api` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_shell` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_system_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_tcpip_forward_acceptor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_tcpip_forward_acceptor_sup` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_tcpip_forward_client` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssh_tcpip_forward_srv` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssh_transport` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssh_xfer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### ssl-11.7.7

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `dtls_client_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 30123: Expected 'end', found '(' |
| `dtls_gen_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 22117: Expected 'end', found '(' |
| `dtls_handshake` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 15826: Expected 'end', found '(' |
| `dtls_packet_demux` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dtls_record` | SourceRejected | — | Макросы | ERL003 at 19006: Function context macros require a function head without macro expansion. |
| `dtls_server_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 30204: Expected 'end', found '(' |
| `inet_tls_dist` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 23742: Expected 'end', found '#' |
| `ssl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_alert` | SourceRejected | — | Макросы | ERL003 at 6033: Function context macros require a function head without macro expansion. |
| `ssl_certificate` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_cipher` | SourceRejected | — | Макросы | ERL003 at 10668: Function context macros require a function head without macro expansion. |
| `ssl_cipher_format` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_config` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 18299: Expected atom |
| `ssl_crl` | Unsupported | trusted_cert_and_path/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssl_crl_cache` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_crl_cache_api` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_crl_hash_dir` | Unsupported | lookup/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssl_dh_groups` | Unsupported | dh_params/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssl_gen_statem` | SourceRejected | — | Макросы | ERL003 at 29467: Function context macros require a function head without macro expansion. |
| `ssl_handshake` | SourceRejected | — | Макросы | ERL003 at 45485: Function context macros require a function head without macro expansion. |
| `ssl_logger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_manager` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_pem_cache` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_pkix_db` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_record` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 18948: Expected 'end', found '(' |
| `ssl_server_session_cache` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_server_session_cache_db` | Unsupported | lookup/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ssl_session` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_session_cache_api` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ssl_trace` | Unsupported | start/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `tls_bloom_filter` | Unsupported | add_elem/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `tls_client_connection` | SourceRejected | — | Макросы | ERL003 at 17894: Function context macros require a function head without macro expansion. |
| `tls_client_connection_1_3` | SourceRejected | — | Макросы | ERL003 at 27562: Function context macros require a function head without macro expansion. |
| `tls_client_ticket_store` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tls_dtls_client_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 9049: Expected 'end', found '(' |
| `tls_dtls_gen_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 18713: Expected 'end', found '(' |
| `tls_dtls_server_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 1098: Expected ')', found ',' |
| `tls_gen_connection` | SourceRejected | — | Макросы | ERL003 at 20884: Function context macros require a function head without macro expansion. |
| `tls_gen_connection_1_3` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tls_handshake` | SourceRejected | — | Макросы | ERL003 at 21286: Function context macros require a function head without macro expansion. |
| `tls_handshake_1_3` | SourceRejected | — | Макросы | ERL003 at 14917: Function context macros require a function head without macro expansion. |
| `tls_record` | SourceRejected | — | Макросы | ERL003 at 11465: Function context macros require a function head without macro expansion. |
| `tls_record_1_3` | SourceRejected | — | Макросы | ERL003 at 10304: Function context macros require a function head without macro expansion. |
| `tls_sender` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 23483: Expected a form-terminating dot |
| `tls_server_connection` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 25563: Expected 'end', found '(' |
| `tls_server_connection_1_3` | SourceRejected | — | Макросы | ERL003 at 13324: Function context macros require a function head without macro expansion. |
| `tls_server_session_ticket` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tls_socket` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tls_socket_tcp` | Unsupported | setopts/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `tls_v1` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### stdlib-8.1

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `argparse` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `array` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `base64` | Unsupported | missing_padding_error/0 | Встраивание указанной функции | Targeted inline lowering is not supported for missing_padding_error/0. |
| `beam_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `binary` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `c` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `calendar` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dets` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `dets_server` | Unsupported | pid2name_1/1 | Встраивание указанной функции | Targeted inline lowering is not supported for pid2name_1/1. |
| `dets_utils` | Unsupported | sz2pos/1 | Встраивание указанной функции | Targeted inline lowering is not supported for sz2pos/1. |
| `dets_v9` | Unsupported | max_objsize/1 | Встраивание указанной функции | Targeted inline lowering is not supported for max_objsize/1. |
| `dict` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `digraph` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `digraph_utils` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edlin` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edlin_context` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `edlin_expand` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 50959: Hex, control and octal escapes are not implemented yet |
| `edlin_key` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 6477: Hex, control and octal escapes are not implemented yet |
| `edlin_type_suggestion` | Unsupported | type_traverser/5 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `epp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_abstract_code` | Unsupported | debug_info/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_anno` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_bits` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_compile` | Unsupported | compile_cmdline1/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_error` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_eval` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 5319: Expected atom |
| `erl_expand_records` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 2716: Expected atom |
| `erl_features` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_internal` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_lint` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 34934: Expected atom |
| `erl_parse` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 18959: Expected atom |
| `erl_posix_msg` | Unsupported | message_1/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_pp` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 2756: Expected atom |
| `erl_scan` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_stdlib_errors` | Unsupported | format_binary_error/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `erl_tar` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 4689: In include 'erl_tar.hrl': Hex, control and octal escapes are not implemented yet |
| `error_logger_file_h` | Unsupported | handle_event/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `error_logger_tty_h` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `escript` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 35970: Expected ')', found '(' |
| `ets` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `eval_bits` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `file_sorter` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 9379: Expected atom |
| `filelib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `filename` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gb_sets` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gb_trees` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen_event` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen_fsm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen_server` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `gen_statem` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `graph` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `io` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `io_ansi` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `io_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `io_lib_format` | Unsupported | args/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `io_lib_fread` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `io_lib_pretty` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `json` | SourceRejected | — | Другие параметры -compile | ERL003 at 2098: Unsupported -compile option: warn_missing_spec |
| `lists` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `log_mf_h` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `man_docs` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL001 at 14967: Adjacent quoted strings require a separator. |
| `maps` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `math` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ms_transform` | SourceRejected | — | Формы документации | ERL003 at 741: Expected a documentation string, binary, metadata map, true, false, or hidden. |
| `orddict` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `ordsets` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `otp_internal` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `peer` | SourceRejected | — | Формы документации | ERL003 at 2216: Expected a documentation string, binary, metadata map, true, false, or hidden. |
| `pool` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `proc_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `proplists` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `qlc` | SourceRejected | — | Формы документации | ERL003 at 732: Expected a documentation string, binary, metadata map, true, false, or hidden. |
| `qlc_pt` | SourceRejected | — | Parse transforms | ERL003 at 119144: Unsupported -compile option: {parse_transform,ms_transform} |
| `queue` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `rand` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 26211: Based floating-point literals are not implemented yet |
| `random` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `re` | SourceRejected | — | Формы документации | ERL003 at 731: Expected a documentation string, binary, metadata map, true, false, or hidden. |
| `records` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sets` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `shell` | SourceRejected | — | Формы документации | ERL003 at 734: Expected a documentation string, binary, metadata map, true, false, or hidden. |
| `shell_default` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `shell_docs` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `shell_docs_markdown` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 2995: Unsupported expression '~' |
| `slave` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sofs` | SourceRejected | — | Формы документации | ERL003 at 733: Expected a documentation string, binary, metadata map, true, false, or hidden. |
| `string` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `supervisor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `supervisor_bridge` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `sys` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `timer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `unicode` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `unicode_util` | Unsupported | class/1 | Встраивание указанной функции | Targeted inline lowering is not supported for class/1. |
| `uri_string` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `win32reg` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `zip` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 10299: Unsupported expression '~' |
| `zstd` | SourceRejected | — | Атрибут on_load | ERL003 at 7519: Attribute 'on_load' is not supported yet |

### syntax_tools-4.1.1

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `epp_dodger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_comment_scan` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_prettypr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_recomment` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_syntax` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `erl_syntax_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `merl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `merl_transform` | SourceRejected | — | Parse transforms | ERL003 at 13023: Unsupported -compile option: {parse_transform,merl_transform} |
| `prettypr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### tftp-1.3

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `tftp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tftp_binary` | Unsupported | prepare/6 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `tftp_engine` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tftp_file` | Unsupported | prepare/6 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `tftp_lib` | Unsupported | do_parse_config/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `tftp_logger` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tftp_sup` | Unsupported | stop_child/1 | Генерация BEAM, названа функция | Конкретная форма не названа |

### tools-4.2.3

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `cover` | SourceRejected | — | Parse transforms | ERL003 at 104355: Unsupported -compile option: {parse_transform,ms_transform} |
| `cprof` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `crashdump` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `eprof` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `fprof` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `lcnt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `make` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `tags` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 11921: Hex, control and octal escapes are not implemented yet |
| `tprof` | SourceRejected | — | Другие параметры -compile | ERL003 at 14758: Unsupported -compile option: warn_missing_spec |
| `xref` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xref_base` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xref_compiler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xref_parser` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xref_reader` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xref_scanner` | Unsupported | scan/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xref_utils` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### wx-2.7

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `demo` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `demo_html_tagger` | Unsupported | erl2htmlfile/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_aui` | SourceRejected | — | Разрешение include | ERL003 at 917: Could not resolve include '../../include/wx.hrl'. |
| `ex_button` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_canvas` | Unsupported | do_init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_canvas_paint` | Unsupported | handle_event/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_choices` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_cursor` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_dialogs` | Unsupported | handle_event/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_frame_utils` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_gauge` | Unsupported | do_init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_gl` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 3910: Expected ']', found '(' |
| `ex_graphicsContext` | Unsupported | draw/4 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_grid` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_htmlWindow` | SourceRejected | — | Разрешение include | ERL003 at 924: Could not resolve include '../../include/wx.hrl'. |
| `ex_listCtrl` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_notebook` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_notificationMessage` | Unsupported | do_init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_pickers` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_popupMenu` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_radioBox` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_sashWindow` | Unsupported | handle_event/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_sizers` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_slider` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_splitterWindow` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_static` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_textCtrl` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_treeCtrl` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `ex_webview` | Unsupported | button/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `gl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `glu` | SourceRejected | — | Другие параметры -compile | ERL003 at 1073: Unsupported -compile option: inline |
| `sudoku` | SourceRejected | — | Разрешение include | ERL003 at 1070: In include 'sudoku.hrl': Could not resolve include '../../include/wx.hrl'. |
| `sudoku_board` | SourceRejected | — | Разрешение include | ERL003 at 1325: In include 'sudoku.hrl': Could not resolve include '../../include/wx.hrl'. |
| `sudoku_game` | SourceRejected | — | Разрешение include | ERL003 at 791: In include 'sudoku.hrl': Could not resolve include '../../include/wx.hrl'. |
| `sudoku_gui` | SourceRejected | — | Разрешение include | ERL003 at 1262: In include 'sudoku.hrl': Could not resolve include '../../include/wx.hrl'. |
| `wx` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wx_misc` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wx_object` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAcceleratorEntry` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAcceleratorTable` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxActivateEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxArtProvider` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiDockArt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiManager` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiManagerEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiNotebook` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiNotebookEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiPaneInfo` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiSimpleTabArt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxAuiTabArt` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBitmap` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBitmapButton` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBitmapDataObject` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBookCtrlBase` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBookCtrlEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBoxSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBrush` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBufferedDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxBufferedPaintDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxButton` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCalendarCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCalendarDateAttr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCalendarEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCaret` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCheckBox` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCheckListBox` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxChildFocusEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxChoice` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxChoicebook` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxClientDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxClipboard` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxClipboardTextEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCloseEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxColourData` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxColourDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxColourPickerCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxColourPickerEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxComboBox` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCommandEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxContextMenuEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxControl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxControlWithItems` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxCursor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDataObject` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDateEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDatePickerCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDCOverlay` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDirDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDirPickerCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDisplay` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDisplayChangedEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxDropFilesEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxe_master` | Unsupported | init/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `wxe_server` | Unsupported | start/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `wxe_util` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxEraseEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxEvtHandler` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFileDataObject` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFileDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFileDirPickerEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFilePickerCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFindReplaceData` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFindReplaceDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFlexGridSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFocusEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFont` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFontData` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFontDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFontPickerCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFontPickerEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxFrame` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGauge` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGBSizerItem` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGCDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGenericDirCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGLCanvas` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGLContext` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsBrush` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsContext` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsFont` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsGradientStops` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsMatrix` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsObject` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsPath` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsPen` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGraphicsRenderer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGrid` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridBagSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellAttr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellBoolEditor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellBoolRenderer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellChoiceEditor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellEditor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellFloatEditor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellFloatRenderer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellNumberEditor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellNumberRenderer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellRenderer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellStringRenderer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridCellTextEditor` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxGridSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxHelpEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxHtmlEasyPrinting` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxHtmlLinkEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxHtmlWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxIcon` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxIconBundle` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxIconizeEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxIdleEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxImage` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxImageList` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxInitDialogEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxJoystickEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxKeyEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxLayoutAlgorithm` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListbook` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListBox` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListItem` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListItemAttr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxListView` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxLocale` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxLogNull` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMask` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMaximizeEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMDIChildFrame` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMDIClientWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMDIParentFrame` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMemoryDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMenu` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMenuBar` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMenuEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMenuItem` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMessageDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMiniFrame` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMirrorDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMouseCaptureChangedEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMouseCaptureLostEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMouseEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMoveEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxMultiChoiceDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxNavigationKeyEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxNotebook` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxNotificationMessage` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxNotifyEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxOverlay` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPageSetupDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPageSetupDialogData` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPaintDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPaintEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPalette` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPaletteChangedEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPanel` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPasswordEntryDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPen` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPickerBase` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPopupTransientWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPopupWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPostScriptDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPreviewCanvas` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPreviewControlBar` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPreviewFrame` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPrintData` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPrintDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPrintDialogData` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPrinter` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPrintout` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxPrintPreview` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxProgressDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxQueryNewPaletteEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxRadioBox` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxRadioButton` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxRegion` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSashEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSashLayoutWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSashWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxScreenDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxScrollBar` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxScrolledWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxScrollEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxScrollWinEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSetCursorEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxShowEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSingleChoiceDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSizeEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSizerFlags` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSizerItem` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSlider` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSpinButton` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSpinCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSpinEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSplashScreen` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSplitterEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSplitterWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStaticBitmap` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStaticBox` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStaticBoxSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStaticLine` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStaticText` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStatusBar` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStdDialogButtonSizer` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStyledTextCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxStyledTextEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSysColourChangedEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSystemOptions` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxSystemSettings` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTaskBarIcon` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTaskBarIconEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTextAttr` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTextCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTextDataObject` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTextEntryDialog` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxToggleButton` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxToolBar` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxToolbook` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxToolTip` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTopLevelWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTreebook` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTreeCtrl` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxTreeEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxUpdateUIEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxWebView` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxWebViewEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxWindow` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxWindowCreateEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxWindowDC` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxWindowDestroyEvent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `wxXmlResource` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |

### xmerl-2.2

| Модуль | Этап | Функция | Операция или уровень диагностики | Сообщение |
| --- | --- | --- | --- | --- |
| `xmerl` | SourceRejected | — | Литералы и escape-последовательности | ERL003 at 2214: Unsupported expression '~' |
| `xmerl_b64Bin` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `xmerl_b64Bin_scan` | Unsupported | scan_token/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_eventp` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_html` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_lib` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_otpsgml` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_sax_old_dom` | Unsupported | build_dom/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_sax_parser` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_sax_parser_latin1` | Unsupported | handle_end_document/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_sax_parser_list` | Unsupported | handle_end_document/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_sax_parser_utf16be` | Unsupported | handle_end_document/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_sax_parser_utf16le` | Unsupported | handle_end_document/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_sax_parser_utf8` | Unsupported | handle_end_document/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_sax_simple_dom` | Unsupported | build_dom/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_scan` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 20142: Expected a form-terminating dot |
| `xmerl_sgml` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_simple` | Unsupported | keyreplace/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_ucs` | SourceRejected | — | Другие параметры -compile | ERL003 at 756: Unsupported -compile option: verbose |
| `xmerl_uri` | Unsupported | parse_scheme2/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_validate` | Unsupported | element_content/1 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_xlate` | Unsupported | file/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_xml` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_xml_indent` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_xpath` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_xpath_lib` | Unsupported | primary_expr/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_xpath_parse` | Unsupported | yeccpars2_4_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_4_/1. |
| `xmerl_xpath_pred` | Unsupported | eval/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_xpath_scan` | Unsupported | scan_token/2 | Генерация BEAM, названа функция | Конкретная форма не названа |
| `xmerl_xs` | Unsupported | — | Генерация BEAM, указан только модуль | Конкретная форма не названа |
| `xmerl_xsd` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 157493: Expected 'end', found '(' |
| `xmerl_xsd_re` | SourceRejected | — | Диагностика синтаксиса без установленной причины | ERL002 at 1766: Expected atom |
| `xmerl_xsd_re_parse` | Unsupported | yeccpars2_0_/1 | Встраивание указанной функции | Targeted inline lowering is not supported for yeccpars2_0_/1. |
| `xmerl_xsd_type` | Unsupported | check_simpleType/3 | Генерация BEAM, названа функция | Конкретная форма не названа |
