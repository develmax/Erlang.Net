-module(dirty_flags521_driver).
-export([observe/2, observe_control/4]).
observe(Path, Mode) ->
    try dirty_flags521:load(Path) of
        ok ->
            Value = case Mode of
                whole -> dirty_flags521:run();
                probe ->
                    Payload = {lists:seq(1, 300), <<1,2,3>>, #{native521 => true}},
                    [dirty_flags521:normal(Payload), dirty_flags521:cpu(Payload), dirty_flags521:io(Payload)]
            end,
            {loaded, Value};
        Error -> {not_loaded, Error}
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.

%% Each mode runs in a fresh VM/image with new ASCII artifact paths.
observe_control(Path, Barrier, Marker, {Kind, Mode}) ->
    try
        fresh521([Barrier, Marker, Barrier ++ ".entered", Barrier ++ ".done",
            Marker ++ ".tmp", Barrier ++ ".entered.tmp", Barrier ++ ".done.tmp"]),
        ok = dirty_flags521:load_control(Path, Barrier, Marker),
        controlled521(Barrier, Marker, Kind, Mode)
    catch Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.
controlled521(Barrier, Marker, Kind, Mode) ->
    Parent = self(), Ref = make_ref(),
    Function = case Kind of cpu -> hold_control_cpu; io -> hold_control_io end,
    Payload = {lists:seq(1, 300), <<1,2,3>>, {control521, Kind, Mode}},
    {Holder, HM} = spawn_monitor(fun() -> Parent ! {Ref, holder, apply(dirty_flags521, Function, [2000, Payload])} end),
    try
        Entered = await_file521(Barrier ++ ".entered", 5000),
        case {Kind, Entered} of
            {cpu, <<"entered:2\n">>} -> ok;
            {io, <<"entered:3\n">>} -> ok;
            OtherEntered -> error({wrong_native_scheduler521, OtherEntered})
        end,
        Deleted = code:delete(dirty_flags521),
        true = Deleted,
        Kill = case Mode of kill -> exit(Holder, kill); purge -> not_requested end,
        {Purger, PM} = spawn_monitor(fun() ->
            Parent ! {Ref, purge_requested},
            Result = try code:purge(dirty_flags521) of Value -> {returned, Value}
                catch PC:PR:PS -> {raised, PC, PR, PS} end,
            Parent ! {Ref, purge_result, Result}
        end),
        try
            receive {Ref, purge_requested} -> ok after 5000 -> error(purge_start_timeout521) end,
            PurgeBefore = receive {Ref, purge_result, PB} -> {seen, PB} after 0 -> pending end,
            DownBefore = receive {'DOWN', HM, process, Holder, DB} -> {seen, DB} after 0 -> pending end,
            DoneBefore = file:read_file(Barrier ++ ".done"),
            MarkerBefore = file:read_file(Marker),
            case {DoneBefore, MarkerBefore} of
                {{error, enoent}, {error, enoent}} -> ok;
                Other -> error({control_not_physically_held521, Other})
            end,
            %% Release uses file API after delete/purge; no call into closed module.
            ok = file:write_file(Barrier, <<"1">>),
            HolderDown = down521(HM, Holder, DownBefore),
            PurgeResult = case PurgeBefore of {seen, PB1} -> PB1;
                pending -> receive {Ref, purge_result, PB2} -> PB2 after 5000 -> error(purge_result_timeout521) end end,
            PurgerDown = receive {'DOWN', PM, process, Purger, PD} -> PD after 5000 -> error(purger_down_timeout521) end,
            DoneAfter = await_file521(Barrier ++ ".done", 5000),
            case DoneAfter of <<"released\n">> -> ok; _ -> error({control_expired521, DoneAfter}) end,
            Unloaded = await_file521(Marker, 5000),
            case Unloaded of <<"unloaded:0:1\n">> -> ok; _ -> error({unload_before_physical_return521, Unloaded}) end,
            {control521, Kind, Mode, Entered, Deleted, Kill, PurgeBefore, DownBefore,
             DoneBefore, MarkerBefore, HolderDown, PurgeResult, PurgerDown,
             DoneAfter, Unloaded, is_process_alive(Holder), is_process_alive(Purger)}
        after
            file:write_file(Barrier, <<"1">>), stop521(Purger, PM)
        end
    after
        file:write_file(Barrier, <<"1">>), stop521(Holder, HM)
    end.
down521(_Monitor, _Pid, {seen, Reason}) -> Reason;
down521(Monitor, Pid, pending) ->
    receive {'DOWN', Monitor, process, Pid, Reason} -> Reason
    after 5000 -> error(holder_down_timeout521) end.
await_file521(_Path, 0) -> error(control_file_timeout521);
await_file521(Path, N) ->
    case file:read_file(Path) of
        {ok, Bytes} -> Bytes;
        {error, enoent} -> receive after 1 -> await_file521(Path, N-1) end;
        Other -> error({control_file_error521, Other})
    end.
fresh521([]) -> ok;
fresh521([Path | Paths]) ->
    true = is_list(Path),
    true = lists:all(fun(C) -> is_integer(C) andalso C > 0 andalso C < 128 end, Path),
    case file:read_file(Path) of
        {error, enoent} -> fresh521(Paths);
        Other -> error({stale_control_file521, Other})
    end.

stop521(Pid, OriginalMonitor) ->
    %% A new cleanup monitor also works when the original DOWN was consumed.
    CleanupMonitor = monitor(process, Pid),
    try
        exit(Pid, kill),
        receive {'DOWN', CleanupMonitor, process, Pid, _} -> ok
        after 5000 -> error(control_cleanup_timeout521) end
    after
        demonitor(CleanupMonitor, [flush]), demonitor(OriginalMonitor, [flush])
    end.
