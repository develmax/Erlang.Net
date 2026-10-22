-module(beam_image_observer388).
-export([run/3, run_stacks/3]).

run(One, Two, Three) ->
    [{held_error, observe(held_error, One, Two, Three)},
     {held_throw, observe(held_throw, One, Two, Three)}].

run_stacks(One, Two, Three) ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run(One, Two, Three)},
        Zero = at_depth(0, One, Two, Three),
        Single = at_depth(1, One, Two, Three),
        Pair = at_depth(2, One, Two, Three),
        Eight = at_depth(8, One, Two, Three),
        [Default, Zero, Single, Pair, Eight]
    after
        erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth, One, Two, Three) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run(One, Two, Three)}.

observe(Case, One, Two, Three) ->
    true = image_control388:soft_purge(),
    {module, beam_image388} = image_control388:load(One),
    true = image_control388:soft_purge(),
    {Child, Monitor} = erlang:spawn_monitor(beam_image388, capture, [self(), Case]),
    HeldVersion = receive
        {held_ready, Child, Version} -> Version
    after 5000 -> erlang:error({held_not_ready, Case})
    end,
    {module, beam_image388} = image_control388:load(Two),
    CurrentVersion = beam_image388:version(),
    HeldPurge = image_control388:soft_purge(),
    ThirdBlocked = image_control388:load(Three),
    Child ! {release, Case},
    Outcome = receive
        {image_report, Child, Case, Result} -> Result
    after 5000 -> erlang:error({report_missing, Case})
    end,
    receive
        {'DOWN', Monitor, process, Child, normal} -> ok;
        {'DOWN', Monitor, process, Child, Reason} -> erlang:error({child_abnormal, Case, Reason})
    after 5000 -> erlang:error({child_unsettled, Case})
    end,
    SettledPurge = image_control388:soft_purge(),
    ThirdLoaded = image_control388:load(Three),
    ThirdVersion = beam_image388:version(),
    FinalPurge = image_control388:soft_purge(),
    {HeldVersion, CurrentVersion, HeldPurge, ThirdBlocked, Outcome,
     SettledPurge, ThirdLoaded, ThirdVersion, FinalPurge}.
