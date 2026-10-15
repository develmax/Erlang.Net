-module(memory_allocator_probe).
-export([run/1, metadata/0]).
-compile({no_inline, [dynamic_apply/3]}).

run(alloc_info_empty) -> capture(fun() -> erlang:alloc_info([]) end);
run(alloc_sizes_empty) -> capture(fun() -> erlang:alloc_sizes([]) end);
run(alloc_nonlist) -> capture(fun() -> erlang:alloc_info(42) end);
run(alloc_unknown) -> capture(fun() -> erlang:alloc_info(unknown_allocator_audit) end);
run(alloc_duplicate) -> capture(fun() -> erlang:alloc_info([binary_alloc, binary_alloc]) end);
run(alloc_improper) -> capture(fun() -> erlang:alloc_sizes([binary_alloc | tail]) end);
run(alloc_atom) -> capture(fun() -> erlang:alloc_sizes(binary_alloc) end);
run(alloc_order) -> capture(fun() -> erlang:alloc_sizes([binary_alloc, ets_alloc]) end);
run(alloc_raw_ref) -> capture(fun() -> erlang:system_info({allocator, not_a_ref, [binary_alloc]}) end);
run(memory_all) -> capture(fun() -> erlang:memory() end);
run(memory_empty) -> capture(fun() -> erlang:memory([]) end);
run(memory_order_duplicates) -> capture(fun() -> erlang:memory([ets, total, ets]) end);
run(memory_invalid) -> capture(fun() -> erlang:memory(unknown_memory_audit) end);
run(memory_maximum) -> capture(fun() -> erlang:memory(maximum) end);
run(memory_improper) -> capture(fun() -> erlang:memory([total | tail]) end);
run(process_default) -> capture(fun() -> erlang:process_info(self()) end);
run(nif_apply_invalid_path) -> capture(fun() -> dynamic_apply(erlang, load_nif, [42, []]) end);
run(nif_apply_missing_path) -> capture(fun() -> dynamic_apply(erlang, load_nif, ["missing_nif_allocator_audit", []]) end);
run(alloc_info_binary_full) -> capture(fun() -> erlang:alloc_info(binary_alloc) end).

dynamic_apply(Module, Function, Arguments) ->
    erlang:apply(Module, Function, Arguments).

capture(Fun) ->
    try Fun() of
        Value -> {return, Value}
    catch
        Class:Reason:Stack -> {exception, Class, Reason, Stack}
    end.

metadata() ->
    {allocator_memory_metadata,
        [{otp_release, erlang:system_info(otp_release)},
         {erts_version, erlang:system_info(version)},
         {system_version, erlang:system_info(system_version)},
         {system_architecture, erlang:system_info(system_architecture)},
         {wordsize, erlang:system_info(wordsize)},
         {build_type, erlang:system_info(build_type)},
         {emulator_type, erlang:system_info(emu_flavor)}]}.
