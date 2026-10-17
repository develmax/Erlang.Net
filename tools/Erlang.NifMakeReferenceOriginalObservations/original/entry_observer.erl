-module(entry_observer).
-export([metadata/0,run/0]).
metadata() ->
    Expected = filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),
    Expected = filename:absname(code:root_dir()),
    {make_reference_callback_metadata,Expected,erlang:system_info(otp_release),
        erlang:system_info(version),erlang:system_info(system_architecture),erlang:system_info(wordsize)}.
run() ->
    ok = make_reference_callback_contract:load(filename:join(os:getenv("ENTRY_NATIVE_ROOT"),"make_reference_callbacks")),
    {make_reference_callback_contract,Rows,Widths} = make_reference_callback_contract:run(),
    true = code:delete(make_reference_callback_contract),
    false = code:purge(make_reference_callback_contract),
    false = erlang:check_old_code(make_reference_callback_contract),
    false = code:is_loaded(make_reference_callback_contract),
    {make_reference_callback_contract,Rows,Widths,{cleanup,deleted_and_purged}}.
