-module(stacktrace_cp_observer).
-export([run/1, same/2, distinct/2]).

run(Kind) when Kind =:= same; Kind =:= distinct ->
    {Pid, Monitor} = spawn_monitor(?MODULE, Kind, [4, self()]),
    try
        receive {ready, Pid} -> ok after 5000 -> error(ready_timeout) end,
        await_waiting(Pid, 5000),
        Info = process_info(Pid, [status, current_function, current_stacktrace, message_queue_len]),
        Pid ! release,
        Exit = receive {'DOWN', Monitor, process, Pid, Reason} -> Reason after 5000 -> error(down_timeout) end,
        {complete, Kind, Info, Exit}
    after
        case is_process_alive(Pid) of true -> exit(Pid, kill); false -> ok end,
        demonitor(Monitor, [flush])
    end.

same(0, Parent) -> Parent ! {ready, self()}, receive release -> ok end;
same(N, Parent) -> Result = same(N-1, Parent), {same, Result}.

%% All clauses intentionally occupy ONE physical source line. Actual return PCs
%% differ because post-call constructors differ; MFA/file/line cannot separate them.
distinct(0, Parent) -> Parent ! {ready, self()}, receive release -> ok end; distinct(N, Parent) when N band 1 =:= 0 -> Result = distinct(N-1, Parent), {even, Result}; distinct(N, Parent) -> Result = distinct(N-1, Parent), {odd, Result}.

await_waiting(_, 0) -> error(waiting_timeout);
await_waiting(Pid, Left) -> case process_info(Pid, status) of {status, waiting} -> ok; undefined -> error(process_died); _ -> receive after 1 -> await_waiting(Pid, Left-1) end end.
