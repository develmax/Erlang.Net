-module(entry_observer).
-export([metadata/0,run/0]).
metadata() ->
    Expected = filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),
    Expected = filename:absname(code:root_dir()),
    {map_size_callback_metadata,Expected,erlang:system_info(otp_release),
        erlang:system_info(version),erlang:system_info(system_architecture),erlang:system_info(wordsize)}.
run() ->
    ok = map_size_callback_contract:load(filename:join(os:getenv("ENTRY_NATIVE_ROOT"),"map_size_callbacks")),
    {map_size_callback_contract,Rows,Widths} = map_size_callback_contract:run(),
    true = code:delete(map_size_callback_contract),
    false = code:purge(map_size_callback_contract),
    false = erlang:check_old_code(map_size_callback_contract),
    false = code:is_loaded(map_size_callback_contract),
    {map_size_callback_contract,Rows,Widths,{cleanup,deleted_and_purged}}.
