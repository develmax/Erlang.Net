-module(acquire_handoff528).
-export([run/5]).

run(FirstLibrary, NextBeam, SecondLibrary, FirstMarker, SecondMarker) ->
    ok = native_resource_handoff528:load(FirstLibrary, FirstMarker),
    Ref = make_ref(),
    {Pid, Monitor} = spawn_monitor(resource_handoff_worker528, hold, [self(), Ref]),
    try
        Initial = receive {Ref, ready, Value1} -> Value1 after 10000 -> error(no_ready528) end,
        Deleted = code:delete(native_resource_handoff528),
        Purged = code:purge(native_resource_handoff528),
        OldCode = erlang:check_old_code(native_resource_handoff528),
        false = OldCode,
        BeforeTakeover = file:read_file(FirstMarker),
        BeforeFinal = file:read_file(FirstMarker ++ ".final"),
        {error, enoent} = BeforeFinal,
        {error, enoent} = BeforeTakeover,
        {module, native_resource_handoff528} = code:load_binary(native_resource_handoff528, "handoff528-v2", NextBeam),
        ok = native_resource_handoff528:load(SecondLibrary, SecondMarker),
        OldUnload = await_file528(FirstMarker, 2000),
        OldFinal = await_file528(FirstMarker ++ ".final", 2000),
        Pid ! {Ref, inspect},
        Inspected = receive {Ref, inspected, Value2} -> Value2 after 10000 -> error(no_inspected528) end,
        Pid ! {Ref, release},
        Down = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason after 10000 -> error(no_down528) end,
        Count = await_count528(2000),
        {Initial, Deleted, Purged, OldCode, BeforeTakeover, BeforeFinal, OldUnload, OldFinal, Inspected, Down, Count, is_process_alive(Pid)}
    catch Class:Failure:Stack ->
        exit(Pid, kill),
        receive {'DOWN', Monitor, process, Pid, _} -> ok after 10000 -> ok end,
        demonitor(Monitor, [flush]),
        erlang:raise(Class, Failure, Stack)
    end.

await_file528(Path, 0) -> error({no_unload528, file:read_file(Path)});
await_file528(Path, Left) ->
    case file:read_file(Path) of
        {ok, Bytes} -> Bytes;
        {error, enoent} -> timer:sleep(5), await_file528(Path, Left - 1);
        Other -> error({unload_read528, Other})
    end.

await_count528(0) -> error({no_destructor528, native_resource_handoff528:counts528()});
await_count528(Left) ->
    case native_resource_handoff528:counts528() of
        {2, 1, _} = Value -> Value;
        {2, 0, _} -> timer:sleep(5), await_count528(Left - 1);
        Other -> error({destructor_count528, Other})
    end.
