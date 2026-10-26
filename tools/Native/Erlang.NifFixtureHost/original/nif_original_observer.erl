-module(nif_original_observer).
-export([run/1, metadata/0]).

run(Case) ->
    {module, memory_allocator_nif_fixture} = code:load_file(memory_allocator_nif_fixture),
    Before = capture(fun() -> memory_allocator_nif_fixture:increment(0) end),
    LoadInfo = case Case of load_veto -> 17; _ -> 42 end,
    Load = capture(fun() ->
        memory_allocator_nif_fixture:load(os:getenv("NIF_FIXTURE_PATH"), LoadInfo)
    end),
    Call = capture(fun() -> operation(Case) end),
    {nif_original_observation, Case,
        {load_info, LoadInfo},
        {before_load, Before},
        {load_result, Load},
        {call_result, Call}}.

operation(increment_zero) -> memory_allocator_nif_fixture:increment(0);
operation(increment_negative) -> memory_allocator_nif_fixture:increment(-1);
operation(increment_maximum_success) -> memory_allocator_nif_fixture:increment(2147483646);
operation(increment_maximum_badarg) -> memory_allocator_nif_fixture:increment(2147483647);
operation(increment_atom_badarg) -> memory_allocator_nif_fixture:increment(not_an_integer);
operation(greeting) -> memory_allocator_nif_fixture:greeting();
operation(load_veto) -> memory_allocator_nif_fixture:increment(0).

capture(Fun) ->
    try Fun() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

metadata() ->
    {nif_original_metadata,
        {otp_release, erlang:system_info(otp_release)},
        {erts_version, erlang:system_info(version)},
        {system_version, erlang:system_info(system_version)},
        {system_architecture, erlang:system_info(system_architecture)},
        {wordsize, erlang:system_info(wordsize)},
        {emulator_type, erlang:system_info(emu_flavor)},
        {build_type, erlang:system_info(build_type)}}.
