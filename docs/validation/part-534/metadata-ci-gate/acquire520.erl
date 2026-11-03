-module(acquire520).
-export([run/0]).

run() ->
    {module, dynamic_apply_caller520} = code:load_abs("dynamic_apply_caller520"),
    false = code:is_loaded(dynamic_apply_target520),
    {module, dynamic_apply_target520} = code:load_abs("dynamic_apply_target520"),
    {ok, LoaderImage} = file:read_file("odbc.beam"),
    false = code:is_loaded(odbc),
    Parent = self(),
    {Pid, Monitor} = spawn_monitor(dynamic_apply_caller520, worker, [{Parent,odbc,LoaderImage}]),
    try
        Observations = receive
            {dynamic_apply520, Pid, Values} -> Values;
            {'DOWN', Monitor, process, Pid, EarlyReason} ->
                ok = file:write_file("worker-failure.etf", term_to_binary(EarlyReason)),
                error({worker_ended_before_output, EarlyReason})
        after 15000 -> error(worker_result_timeout)
        end,
        Down = receive
            {'DOWN', Monitor, process, Pid, Reason} -> Reason
        after 5000 -> error(worker_down_timeout)
        end,
        normal = Down,
        false = is_process_alive(Pid),
        ok = file:write_file("observed.etf", term_to_binary(Observations, [{minor_version,2}])),
        ok = file:write_file("observed-deterministic.etf", term_to_binary(Observations, [deterministic,{minor_version,2}])),
        Witness = #{caller => Parent, worker => Pid, monitor => Monitor,
                    spawn_mfa => {dynamic_apply_caller520,worker,1}, down => Down,
                    alive_after_down => is_process_alive(Pid)},
        ok = file:write_file("worker-witness.etf", term_to_binary(Witness)),
        Metadata = [{M, code:which(M), M:module_info(compile), M:module_info(attributes),
                     M:module_info(md5), beam_lib:chunks(code:which(M),[imports])} ||
                    M <- [dynamic_apply_caller520,dynamic_apply_target520]],
        ok = file:write_file("owners.etf", term_to_binary(Metadata)),
        ok
    after
        case is_process_alive(Pid) of
            true ->
                exit(Pid, kill),
                receive {'DOWN', Monitor, process, Pid, _} -> ok
                after 5000 -> error(worker_cleanup_timeout)
                end;
            false -> ok
        end,
        demonitor(Monitor, [flush])
    end.
