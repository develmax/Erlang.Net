-module(init_observer582).
-export([capture/6]).
capture(AckBeam,Beam1,Beam2,AckNative,Native1,Native2)->
    {module,init_ack582}=erlang:load_module(init_ack582,AckBeam),
    AckLoad=outcome(fun()->init_ack582:load(AckNative) end),
    {module,init_subject582}=erlang:load_module(init_subject582,Beam1),
    Load=outcome(fun()->init_subject582:load(Native1) end),
    OldStatus=init_subject582:make(),
    Before=outcome(OldStatus),
    {module,init_subject582}=erlang:load_module(init_subject582,Beam2),
    Parent=self(),Ref=make_ref(),
    {Pid,Monitor}=spawn_monitor(fun()->
        receive after 50->ok end,
        Parent!{Ref,self(),outcome(fun()->ack(500) end)}
    end),
    try
        Upgrade=outcome(fun()->init_subject582:load(Native2) end),
        After=outcome(fun init_subject582:status/0),
        OldAfter=outcome(OldStatus),
        Ack=receive {Ref,Pid,Value}->Value after 10000->erlang:error(ack_result_timeout) end,
        Down=receive {'DOWN',Monitor,process,Pid,Reason}->Reason
            after 10000->erlang:error(ack_down_timeout) end,
        {AckLoad,Load,Before,Upgrade,After,OldAfter,Ack,Down,init_subject582:marker()}
    after
        erlang:demonitor(Monitor,[flush]),
        case is_process_alive(Pid) of true->exit(Pid,kill);false->ok end
    end.
ack(0)->erlang:error(ack_ready_timeout);
ack(N)->case init_ack582:ack() of
    not_ready->receive after 5->ack(N-1) end;
    Value->Value
end.
outcome(Fun)->try Fun() of Value->{value,Value}
    catch Class:Reason:Stack->{raised,Class,Reason,Stack} end.
