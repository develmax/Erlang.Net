-module(reallocation_observer).
-export([metadata/0, run/1]).

metadata() ->
    {reallocation_probe_metadata,
        {otp_release, erlang:system_info(otp_release)},
        {erts_version, erlang:system_info(version)},
        {architecture, erlang:system_info(system_architecture)},
        {wordsize, erlang:system_info(wordsize)},
        {allocator_options, erlang:system_info(allocator)},
        {driver_allocator, erlang:system_info({allocator, driver_alloc})}}.

enabled_driver(Info) when is_list(Info), Info =/= [] ->
    Options = [proplists:get_value(options, Props, missing) || {instance, _, Props} <- Info],
    Options =/= [] andalso length(Options) =:= length(Info) andalso
        lists:all(fun(Value) -> is_list(Value) andalso proplists:get_value(e, Value, false) =:= true end, Options);
enabled_driver(_) -> false.

run(Case) ->
    Driver = erlang:system_info({allocator, driver_alloc}),
    true = enabled_driver(Driver),
    {Selector, Sizes} = case Case of
        null_positive -> {1, [13]};
        null_zero -> {2, [0]};
        resize_sequence -> {3, [13,37,7,0,5]}
    end,
    {module, memory_allocator_nif_fixture} = code:load_file(memory_allocator_nif_fixture),
    ok = memory_allocator_nif_fixture:load(os:getenv("REALLOCATION_FIXTURE_PATH")),
    "native realloc probe" = memory_allocator_nif_fixture:greeting(),
    Mask = memory_allocator_nif_fixture:increment(Selector),
    {reallocation_probe_case, Case,
        {selector, Selector}, {requested_sizes_authored, Sizes},
        {enabled_driver_before_call, Driver}, {returned_mask, Mask}}.
