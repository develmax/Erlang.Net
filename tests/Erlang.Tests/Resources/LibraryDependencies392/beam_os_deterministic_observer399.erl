-module(beam_os_deterministic_observer399).
-export([cases/0, run/0, run_stacks/0]).

cases() -> [getenv_seed,getenv_missing,put_get_unset,put_replace,unset_missing,empty_value,binary_key_value,nested_key_value,unicode_value,case_keys,getenv_atom,getenv_nul,getenv_empty,getenv_equals,putenv_atom_value,putenv_nul_value,unsetenv_atom,system_time_unit_zero,system_time_unit_bad,signal_bad_option,signal_nonatom,signal_unknown,signal_ignore,signal_handle,utf16_pair,utf16_lone,empty_key_roundtrip,tree_ascending,tree_descending,tree_delete_successor,tree_case_replace].

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
    {Child, Monitor} = erlang:spawn_monitor(beam_os_subject399, worker, [self(), Case]),
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
