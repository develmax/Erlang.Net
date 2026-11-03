-module(dirty_flags521).
-export([load_control/3, hold_control_cpu/2, hold_control_io/2, foreign_thread/0, load/1, run/0, normal/1, cpu/1, io/1, hold_cpu/2, hold_io/2, status/0, release/0, reset/0]).
-nifs([hold_control_cpu/2, hold_control_io/2, foreign_thread/0, normal/1, cpu/1, io/1, hold_cpu/2, hold_io/2, status/0, release/0, reset/0]).
load(Path) -> erlang:load_nif(Path, 0).
load_control(Path, Barrier, Marker) -> erlang:load_nif(Path, {Barrier, Marker}).
hold_control_cpu(_, _) -> erlang:nif_error(not_loaded521).
hold_control_io(_, _) -> erlang:nif_error(not_loaded521).
foreign_thread() -> erlang:nif_error(not_loaded521).
normal(_) -> erlang:nif_error(not_loaded521).
cpu(_) -> erlang:nif_error(not_loaded521).
io(_) -> erlang:nif_error(not_loaded521).
hold_cpu(_, _) -> erlang:nif_error(not_loaded521).
hold_io(_, _) -> erlang:nif_error(not_loaded521).
status() -> erlang:nif_error(not_loaded521).
release() -> erlang:nif_error(not_loaded521).
reset() -> erlang:nif_error(not_loaded521).
run() ->
    Payload = {lists:seq(1, 300), <<1,2,3>>, #{native521 => true}},
    Basic = [normal(Payload), cpu(Payload), io(Payload), normal(Payload)],
    %% Each return really resumes a normal Erlang actor before the next call.
    RecursiveOwner = nested521(8, Payload),
    {Basic, RecursiveOwner, foreign_thread(), held521(hold_cpu, Payload), held521(hold_io, Payload),
     cancelled521(hold_cpu, Payload), cancelled521(hold_io, Payload), queued521(Payload)}.
nested521(0, Payload) -> normal(Payload);
nested521(N, Payload) -> {normal(N), cpu(N), io(N), nested521(N-1, Payload)}.
held521(Function, Payload) ->
    ok = reset(), Parent = self(), Ref = make_ref(),
    {Pid, Monitor} = spawn_monitor(fun() -> Parent ! {Ref, self(), apply(?MODULE, Function, [2000, Payload])} end),
    try
        Ready = busy521(5000),
        ReleasedOn = release(),
        Value = receive {Ref, Pid, Result} -> Result after 5000 -> error(held_result_timeout521) end,
        Down = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason after 5000 -> error(held_down_timeout521) end,
        {Ready, ReleasedOn, Value, Down, status(), is_process_alive(Pid)}
    after
        release(), exit(Pid, kill), demonitor(Monitor, [flush])
    end.
cancelled521(Function, Payload) ->
    ok = reset(),
    {Pid, Monitor} = spawn_monitor(fun() -> apply(?MODULE, Function, [2000, Payload]) end),
    try
        Ready = busy521(5000),
        true = exit(Pid, kill),
        ReleasedOn = release(),
        Down = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason after 5000 -> error(cancel_down_timeout521) end,
        Quiet = quiet521(5000),
        {Ready, ReleasedOn, Down, Quiet, is_process_alive(Pid)}
    after
        release(), exit(Pid, kill), demonitor(Monitor, [flush])
    end.
queued521(Payload) ->
    %% Provider MUST use +SDcpu 1 so the second job cannot execute before kill.
    ok = reset(), Parent = self(), Ref = make_ref(),
    {Holder, HM} = spawn_monitor(fun() -> Parent ! {Ref, self(), hold_cpu(2000, Payload)} end),
    try
        Ready = busy521(5000),
        %% Only start the second process after the first native job is physical.
        {Queued, QM} = spawn_monitor(fun() -> Parent ! {Ref, calling521}, cpu(Payload) end),
        try
            receive {Ref, calling521} -> ok after 5000 -> error(queued_ack_timeout521) end,
            true = exit(Queued, kill),
            ReleasedOn = release(),
            QDown = receive {'DOWN', QM, process, Queued, QR} -> QR after 5000 -> error(queued_down_timeout521) end,
            Value = receive {Ref, Holder, HV} -> HV after 5000 -> error(holder_result_timeout521) end,
            HDown = receive {'DOWN', HM, process, Holder, HR} -> HR after 5000 -> error(holder_down_timeout521) end,
            {Ready, ReleasedOn, QDown, Value, HDown, quiet521(5000)}
        after
            exit(Queued, kill), demonitor(QM, [flush])
        end
    after
        release(), exit(Holder, kill), demonitor(HM, [flush])
    end.

busy521(0) -> error(native_start_timeout521);
busy521(N) ->
    case status() of {_, B, _, _, _, _} = Row when B > 0 -> Row;
        _ -> receive after 1 -> busy521(N-1) end end.
quiet521(0) -> error(native_finish_timeout521);
quiet521(N) ->
    case status() of {_, 0, _, _, _, _} = Row -> Row;
        _ -> receive after 1 -> quiet521(N-1) end end.
