-module(acquire_handoff_corrected529).
-export([run/6]).

run(Partition, FirstLibrary, NextBeam, SecondLibrary, FirstMarker, SecondMarker) ->
    ok = native_resource_handoff528:load(FirstLibrary, FirstMarker),
    Ref = make_ref(),
    {Pid, Monitor} = spawn_monitor(resource_handoff_worker528, hold, [self(), Ref]),
    try
        Initial = receive {Ref, ready, Value1} -> Value1 after 10000 -> error(no_ready528) end,
        {1, 0, 1, 1, 0, ChildBytes} = Initial,
        64 = length(ChildBytes),
        true = lists:all(fun(Byte) -> Byte =:= 90 end, ChildBytes),
        Before = markers529(FirstMarker),
        {{error, enoent}, {error, enoent}, {error, enoent}} = Before,
        PreCode = case Partition of
            fresh_load_after_purge ->
                D = code:delete(native_resource_handoff528),
                P = code:purge(native_resource_handoff528),
                false = erlang:check_old_code(native_resource_handoff528),
                {D, P, false};
            genuine_upgrade -> old_code_not_purged
        end,
        {module, native_resource_handoff528} = code:load_binary(native_resource_handoff528, "handoff529-v2", NextBeam),
        LoadResult = native_resource_handoff528:load(SecondLibrary, SecondMarker),
        ok = LoadResult,
        Pid ! {Ref, inspect},
        Inspected = receive {Ref, inspected, Value2} -> Value2 after 10000 -> error(no_inspected528) end,
        {PostCode, HeldMarkers} = case Partition of
            fresh_load_after_purge ->
                {2, 0, 1, 0, 1, []} = Inspected,
                %% Actual CREATE, distinct type, live old resource still pins old DLL.
                Held = markers529(FirstMarker),
                {{error, enoent}, {error, enoent}, {error, enoent}} = Held,
                {not_applicable_no_old_beam, Held};
            genuine_upgrade ->
                {2, 1, 2, 1, 0, ChildBytes} = Inspected,
                %% Actual UPGRADE callback and TAKEOVER preserve the same type identity.
                Purged = code:purge(native_resource_handoff528),
                false = erlang:check_old_code(native_resource_handoff528),
                Unloaded = await_file529(FirstMarker, 2000),
                Finalized = await_file529(FirstMarker ++ ".final", 2000),
                {error, enoent} = file:read_file(FirstMarker ++ ".dtor"),
                {{Purged, false}, {Unloaded, Finalized, no_old_destructor}}
        end,
        Pid ! {Ref, release},
        FinalProbe = receive {Ref, final_probe, Value3} -> Value3 after 10000 -> error(no_final_probe529) end,
        Down = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason after 10000 -> error(no_down528) end,
        normal = Down,
        DestructorPath = case Partition of
            fresh_load_after_purge -> FirstMarker ++ ".dtor";
            genuine_upgrade -> SecondMarker ++ ".dtor"
        end,
        DestructorBytes = await_file529(DestructorPath, 2000),
        ExpectedVersion = case Partition of fresh_load_after_purge -> 49; genuine_upgrade -> 50 end,
        <<ExpectedVersion, ActualChild:64/binary>> = DestructorBytes,
        ChildBytes = binary_to_list(ActualChild),
        OldUnload = await_file529(FirstMarker, 2000),
        OldFinal = await_file529(FirstMarker ++ ".final", 2000),
        {Partition, Initial, PreCode, LoadResult, Inspected, PostCode, Before,
            HeldMarkers, FinalProbe, Down, DestructorBytes, OldUnload, OldFinal,
            native_resource_handoff528:counts528(), is_process_alive(Pid)}
    catch Class:Failure:Stack ->
        exit(Pid, kill),
        receive {'DOWN', Monitor, process, Pid, _} -> ok after 10000 -> ok end,
        demonitor(Monitor, [flush]),
        erlang:raise(Class, Failure, Stack)
    end.

markers529(Path) ->
    {file:read_file(Path), file:read_file(Path ++ ".final"), file:read_file(Path ++ ".dtor")}.

await_file529(Path, 0) -> error({missing_marker529, Path, file:read_file(Path)});
await_file529(Path, Left) ->
    case file:read_file(Path) of
        {ok, Bytes} -> Bytes;
        {error, enoent} -> timer:sleep(5), await_file529(Path, Left - 1);
        Other -> error({marker_read529, Path, Other})
    end.
