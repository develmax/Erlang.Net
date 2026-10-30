-module(env_observer709).
-export([capture/5,worker/3]).

capture(BeamA, BeamB, Native, Log, Mode) ->
    Parent=self(), Ref=make_ref(),
    {Pid,Monitor}=spawn_monitor(?MODULE,worker,[Parent,Ref,{BeamA,BeamB,Native,Log,Mode}]),
    try
        Result=receive {Ref,Pid,Value}->Value
            after 30000->erlang:error(env709_worker_timeout) end,
        receive
            {'DOWN',Monitor,process,Pid,normal}->{Result,normal};
            {'DOWN',Monitor,process,Pid,Reason}->erlang:error({env709_worker_failed,Reason})
        after 30000->erlang:error(env709_down_timeout)
        end
    after
        erlang:demonitor(Monitor,[flush]),
        case is_process_alive(Pid) of true->exit(Pid,kill);false->ok end
    end.

worker(Parent, Ref, Arguments) ->
    Result=try observe(Arguments) of Value->{returned,Value}
        catch Class:Reason:Stack->{raised,Class,Reason,Stack} end,
    Parent ! {Ref,self(),Result},
    ok.

observe({BeamA,BeamB,Native,Log,Mode}) ->
    {module,nif_env709}=erlang:load_module(nif_env709,BeamA),
    First=outcome(fun()->nif_env709:load(Native,{Log,0}) end),
    {OldRound,OldCounts,OldStable,OldThread}=nif_env709:capture(),
    Before={outcome(fun()->OldRound({before,709}) end),outcome(OldCounts),outcome(OldStable)},
    {module,nif_env709}=erlang:load_module(nif_env709,BeamB),
    Upgrade=outcome(fun()->nif_env709:load(Native,{Log,Mode}) end),
    {CurrentRound,CurrentCounts,CurrentStable,CurrentThread}=nif_env709:capture(),
    erlang:garbage_collect(),
    OldAfter=outcome(fun()->OldRound({old_after,709}) end),
    CurrentAfter=outcome(fun()->CurrentRound({current_after,709}) end),
    Counts={outcome(OldCounts),outcome(CurrentCounts)},
    Stable={outcome(OldStable),outcome(CurrentStable)},
    Threads={outcome(OldThread),outcome(CurrentThread)},
    {First,Before,Upgrade,OldAfter,CurrentAfter,Counts,Stable,Threads}.

outcome(Fun) ->
    try Fun() of Value->{value,Value}
    catch Class:Reason:Stack->{raised,Class,Reason,Stack} end.
