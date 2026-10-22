-module(beam_standard_observer411).
-export([cases/0, run/0, run_stacks/0, capture/1, capture_after_started/1]).

cases() -> [status,script_id,running_apps,loaded_apps,services,app_supervisors,code_path,app_dirs,modules,default_env,wrong_supervisor_argument,wrong_application_timeout,application_lifecycle].

run() -> rows(cases()).

rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, Old, run()},
        Zero = at_depth(0),
        One = at_depth(1),
        Two = at_depth(2),
        Eight = at_depth(8),
        [Default, Zero, One, Two, Eight]
    after
        erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    Effective = erlang:system_flag(backtrace_depth, Depth),
    {Depth, Effective, run()}.

observe(Case) ->
    {Child, Monitor} = erlang:spawn_monitor(beam_standard_subject411, worker, [self(), Case]),
    receive
        {Case, Outcome} -> wait_down(Child, Monitor, Case, Outcome);
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_without_report, Case, Reason})
    after 30000 ->
        abort(Child, Monitor, Case)
    end.

wait_down(Child, Monitor, Case, Outcome) ->
    receive
        {'DOWN', Monitor, process, Child, normal} -> Outcome;
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({reported_but_abnormal_child, Case, Reason})
    after 5000 ->
        abort(Child, Monitor, Case)
    end.

abort(Child, Monitor, Case) ->
    erlang:exit(Child, kill),
    receive
        {'DOWN', Monitor, process, Child, _} -> erlang:error({child_timeout, Case})
    after 5000 ->
        erlang:error({unsettled_child, Case})
    end.


capture([Output0]) ->
    Output=case Output0 of A when is_atom(A)->atom_to_list(A); B when is_binary(B)->binary_to_list(B); L when is_list(L)->L end,
    spawn(?MODULE,capture_after_started,[Output]),ok.

capture_after_started(Output)->
    %% Return capture/1 to the real boot worker so init can enter actual started state.
    ok=await_started(5000),
    Values=run(),Stacks=run_stacks(),
    ok=prim_file:write_file(Output++"/values.etf",term_to_binary(Values,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(Stacks,[{minor_version,2}])),
    Proof={init:get_status(),init:script_id(),is_pid(whereis(init)),
        is_pid(whereis(application_controller)),is_pid(whereis(kernel_sup))},
    ok=prim_file:write_file(Output++"/bootstrap-proof.etf",term_to_binary(Proof,[{minor_version,2}])),
    halt(0).

await_started(0)->erlang:error(real_init_never_started);
await_started(N)->
    case init:get_status() of
        {started,started}->ok;
        _->receive after 1->await_started(N-1) end
    end.
