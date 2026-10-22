-module(beam_code_server_observer410).
-export([cases/0, run/0, run_stacks/0, capture/1]).

cases() -> [ready,mode,empty_path,missing,preloaded,loaded,request_reply,finish_empty,owned_tables,wrong_arity].

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
    {Child, Monitor} = erlang:spawn_monitor(beam_code_server_subject410, worker, [self(), Case]),
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


capture(Output) ->
    %% Runs inside the real boot worker, while init serves its ordinary protocol.
    Values=run(), Stacks=run_stacks(),
    ok=prim_file:write_file(Output++"/values.etf",term_to_binary(Values,[{minor_version,2}])),
    ok=prim_file:write_file(Output++"/stacks.etf",term_to_binary(Stacks,[{minor_version,2}])),
    Shutdown=beam_code_server_subject410:shutdown(),
    ok=prim_file:write_file(Output++"/shutdown.etf",term_to_binary(Shutdown,[{minor_version,2}])),
    halt(0).
