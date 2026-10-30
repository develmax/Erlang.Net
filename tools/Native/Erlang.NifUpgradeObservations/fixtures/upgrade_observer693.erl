-module(upgrade_observer693).
-export([capture/7,worker/3]).

capture(Beam1, Beam2, NativeA, NativeB, LogPath, Mode, ReloadOnly) ->
    Parent=self(), Ref=make_ref(),
    {Pid,Monitor}=spawn_monitor(?MODULE,worker,[Parent,Ref,
        {Beam1,Beam2,NativeA,NativeB,LogPath,Mode,ReloadOnly}]),
    try
        Result=receive {Ref,Pid,Value}->Value
            after 30000->erlang:error(upgrade693_worker_timeout) end,
        receive
            {'DOWN',Monitor,process,Pid,normal}->{Result,normal};
            {'DOWN',Monitor,process,Pid,Reason}->erlang:error({upgrade693_worker_failed,Reason})
        after 30000->erlang:error(upgrade693_down_timeout)
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

observe({Beam1,Beam2,NativeA,NativeB,LogPath,Mode,ReloadOnly}) ->
    {module,nif_upgrade693}=erlang:load_module(nif_upgrade693,Beam1),
    FirstLoad=outcome(fun()->nif_upgrade693:load(NativeA,{LogPath,0}) end),
    {OldIdentity,OldState}=nif_upgrade693:make(),
    Before={outcome(OldIdentity),outcome(OldState)},
    MarkerBefore=nif_upgrade693:marker(),
    case ReloadOnly of
        true->ok;
        false->{module,nif_upgrade693}=erlang:load_module(nif_upgrade693,Beam2)
    end,
    Upgrade=outcome(fun()->nif_upgrade693:load(NativeB,{LogPath,Mode}) end),
    {NewIdentity,NewState}=nif_upgrade693:make(),
    After={outcome(OldIdentity),outcome(NewIdentity),outcome(OldState),outcome(NewState)},
    Roles={OldIdentity=:=NewIdentity,OldState=:=NewState,Beam1=:=Beam2,
        MarkerBefore,nif_upgrade693:marker()},
    Reload=outcome(fun()->nif_upgrade693:load(NativeB,{LogPath,Mode}) end),
    {FirstLoad,Before,Upgrade,After,Reload,Roles}.

outcome(Fun) ->
    try Fun() of Value->{value,Value}
    catch Class:Reason:Stack->{raised,Class,Reason,Stack} end.
