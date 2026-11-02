-module(init_observer582).
-export([capture/6]).

%% Controlled variant: real online schedulers and two distinct pinned actors.
%% Preserve the original complete nine-field observation, not a replacement verdict.
capture(AckBeam,Beam1,Beam2,AckNative,Native1,Native2)->
    2=erlang:system_info(schedulers),
    2=erlang:system_info(schedulers_online),
    Parent=self(),Ref=make_ref(),
    {Worker,Monitor}=spawn_opt(fun()->
        1=erlang:system_info(scheduler_id),
        Result=outcome(fun()->capture_run(AckBeam,Beam1,Beam2,AckNative,Native1,Native2) end),
        Parent!{Ref,self(),get(init582_profile),Result}
    end,[monitor,{scheduler,1}]),
    try
        {Profile,Result}=receive
            {Ref,Worker,P,R}->{P,R}
        after 30000->erlang:error(loader_result_timeout)
        end,
        receive {'DOWN',Monitor,process,Worker,normal}->ok;
            {'DOWN',Monitor,process,Worker,Reason}->erlang:error({loader_down,Reason})
        after 10000->erlang:error(loader_down_timeout)
        end,
        put(init582_profile,Profile),
        case Result of {value,Value}->Value;
            {raised,Class,Reason0,Stack}->erlang:raise(Class,Reason0,Stack)
        end
    after
        erlang:demonitor(Monitor,[flush]),
        case is_process_alive(Worker) of true->exit(Worker,kill);false->ok end
    end.

capture_run(AckBeam,Beam1,Beam2,AckNative,Native1,Native2)->
    {module,init_ack582}=erlang:load_module(init_ack582,AckBeam),
    AckLoad=outcome(fun()->init_ack582:load(AckNative) end),
    {module,init_subject582}=erlang:load_module(init_subject582,Beam1),
    Load=outcome(fun()->init_subject582:load(Native1) end),
    OldStatus=init_subject582:make(),
    Before=outcome(OldStatus),
    {module,init_subject582}=erlang:load_module(init_subject582,Beam2),
    Parent=self(),Ref=make_ref(),
    {Pid,Monitor}=spawn_opt(fun()->
        receive after 50->ok end,
        Warm=init_ack582:ack(),
        Scheduler=erlang:system_info(scheduler_id),
        Parent!{Ref,self(),armed,Warm,Scheduler},
        receive {Ref,go}->
            Parent!{Ref,self(),outcome(fun()->ack(500) end)}
        after 10000->erlang:error(ack_go_timeout)
        end
    end,[monitor,{scheduler,2}]),
    try
        {WarmAck,WarmScheduler}=receive {Ref,Pid,armed,Warm,Scheduler}->{Warm,Scheduler}
        after 10000->erlang:error(ack_arm_timeout)
        end,
        not_ready=WarmAck,
        2=WarmScheduler,
        Profile={init582_control,erlang:system_info(schedulers),
            erlang:system_info(schedulers_online),
            erlang:system_info(scheduler_id),WarmScheduler,WarmAck},
        put(init582_profile,Profile),
        Pid!{Ref,go},
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
