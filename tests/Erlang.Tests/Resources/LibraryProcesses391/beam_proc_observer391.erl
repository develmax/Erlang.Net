-module(beam_proc_observer391).
-export([cases/0, cases/1, run/0, run_stacks/0, run_group/1, run_group_stacks/1]).

cases() ->
    [spawn1,
     spawn2,
     spawn3,
     spawn4,
     spawn_link1,
     spawn_link2,
     spawn_link3,
     spawn_link4,
     spawn_opt2,
     spawn_opt3,
     spawn_opt4,
     spawn_opt5,
     start3,
     start4,
     start5,
     start_link3,
     start_link4,
     start_link5,
     start_monitor3,
     start_monitor4,
     start_monitor5,
     init_ack1,
     init_ack2,
     init_fail2,
     init_fail3,
     init_p3,
     init_p5,
     wake_up3,
     hibernate3,
     initial_call1,
     translate_initial_call1,
     set_label1,
     get_label1,
     format1,
     format2,
     format3,
     report_cb2,
     stop1,
     stop3,
     spawn_badfun,
     spawn_mfa_badmodule,
     spawn_opt_badopts,
     start_monitor_option,
     start_monitor_tuple_option,
     start_timeout,
     initial_call_unknown,
     translate_unknown,
     initial_call_bad_shape,
     init_ack_missing_ancestors,
     init_fail_raw_invalidclass,
     wake_up_badmodule,
     hibernate_badmodule].

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
    {Child, Monitor} = erlang:spawn_monitor(beam_proc_subject391, worker, [self(), Case]),
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

cases(error_full_stack) -> [spawn_badfun,spawn_mfa_badmodule,spawn_opt_badopts,start_monitor_option,start_monitor_tuple_option,start_timeout,initial_call_unknown,translate_unknown,initial_call_bad_shape,init_ack_missing_ancestors,init_fail_raw_invalidclass,wake_up_badmodule,hibernate_badmodule];
cases(formatter_dependencies) -> [format1,format2,format3,report_cb2];
cases(local_lifecycle) -> [spawn1,spawn2,spawn3,spawn4,spawn_link1,spawn_link2,spawn_link3,spawn_link4,spawn_opt2,spawn_opt3,spawn_opt4,spawn_opt5,start3,start4,start5,start_link3,start_link4,start_link5,start_monitor3,start_monitor4,start_monitor5,init_ack1,init_ack2,init_fail2,init_fail3,init_p3,init_p5,wake_up3,hibernate3,initial_call1,translate_initial_call1,set_label1,get_label1];
cases(sys_dependencies) -> [stop1,stop3].

run_group(Group) -> rows(cases(Group)).
run_group_stacks(Group) ->
    Old=erlang:system_flag(backtrace_depth,8), erlang:system_flag(backtrace_depth,Old),
    try
        Default={default,Old,run_group(Group)},
        Zero=group_depth(Group,0), One=group_depth(Group,1),
        Two=group_depth(Group,2), Eight=group_depth(Group,8),
        [Default,Zero,One,Two,Eight]
    after erlang:system_flag(backtrace_depth,Old) end.
group_depth(Group,Depth) ->
    erlang:system_flag(backtrace_depth,Depth),
    Effective=erlang:system_flag(backtrace_depth,Depth),
    {Depth,Effective,run_group(Group)}.
