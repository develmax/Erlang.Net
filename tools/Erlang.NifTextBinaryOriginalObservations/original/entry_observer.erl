-module(entry_observer).
-export([metadata/0,run/0]).
metadata() ->
    Expected = filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),
    Expected = filename:absname(code:root_dir()),
    {text_binary_callback_metadata,Expected,erlang:system_info(otp_release),
        erlang:system_info(version),erlang:system_info(system_architecture),erlang:system_info(wordsize)}.
run() ->
    ok = text_binary_callback_contract:load(filename:join(os:getenv("ENTRY_NATIVE_ROOT"),"text_binary_callbacks")),
    {text_binary_callback_contract,Rows,Widths} = text_binary_callback_contract:run(),
    true = code:delete(text_binary_callback_contract),
    false = code:purge(text_binary_callback_contract),
    false = erlang:check_old_code(text_binary_callback_contract),
    false = code:is_loaded(text_binary_callback_contract),
    {text_binary_callback_contract,Rows,Widths,{cleanup,deleted_and_purged}}.
