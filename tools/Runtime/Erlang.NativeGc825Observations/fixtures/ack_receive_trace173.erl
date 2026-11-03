-module(ack_receive_trace173).
-export([observe/0,worker/1,held/1,sink/1,capture/1]).

observe() ->
    {Pid,Ref}=spawn_monitor(?MODULE,worker,[self()]),
    receive
        {Pid,Value} -> receive {'DOWN',Ref,process,Pid,normal} -> Value end;
        {'DOWN',Ref,process,Pid,Reason} -> erlang:error({ack_trace_worker_failed,Reason})
    after 10000 -> exit(Pid,kill),erlang:error(ack_trace_worker_timeout)
    end.

worker(Parent) ->
    {Tracer,TracerRef}=spawn_monitor(?MODULE,sink,[self()]),
    {Target,TargetRef}=spawn_monitor(?MODULE,held,[self()]),
    receive {ready173,Target} -> ok after 5000 -> erlang:error(target_ready_timeout) end,
    try
        {Dead,DeadRef}=spawn_monitor(fun() -> ok end),
        receive {'DOWN',DeadRef,process,Dead,normal} -> ok after 5000 -> erlang:error(dead_target_timeout) end,
        _=erlang:seq_trace(sequential_trace_token,[]),
        _=erlang:seq_trace(label,ack_token173),
        Before={erlang:seq_trace_info(label),erlang:seq_trace_info(serial)},
        MissingTag={ack173,missing,<<1,3,5,7>>},
        MissingAck={MissingTag,badarg},
        1=erlang:trace_pattern('receive',[{[system,undefined,'_'],[],[{message,'$_'}]}]),
        1=erlang:trace(self(),true,['receive',{tracer,Tracer}]),
        MissingResult=erlang:suspend_process(Dead,[{asynchronous,MissingTag}]),
        receive MissingAck -> ok after 5000 -> erlang:error(missing_ack_timeout) end,
        AfterMissing={erlang:seq_trace_info(label),erlang:seq_trace_info(serial)},
        MissingTokenPreserved=AfterMissing=:=Before,
        MissingActual=receive {trace_record173,T1} -> T1 after 5000 -> erlang:error(missing_trace_timeout) end,
        MissingExpected={trace,self(),'receive',MissingAck,[system,undefined,MissingAck]},
        true=MissingActual=:=MissingExpected,
        _=erlang:seq_trace(sequential_trace_token,[]),
        _=erlang:seq_trace(label,ack_token173),
        BeforeLive={erlang:seq_trace_info(label),erlang:seq_trace_info(serial)},
        ResetMatches=BeforeLive=:=Before,
        LiveTag={ack173,live,<<2,4,6,8>>},
        LiveAck={LiveTag,suspended},
        1=erlang:trace_pattern('receive',[{[node(),Target,'_'],[],[{message,'$_'}]}]),
        LiveResult=erlang:suspend_process(Target,[{asynchronous,LiveTag}]),
        receive LiveAck -> ok after 5000 -> erlang:error(live_ack_timeout) end,
        AfterLive={erlang:seq_trace_info(label),erlang:seq_trace_info(serial)},
        LiveTokenPreserved=AfterLive=:=BeforeLive,
        LiveActual=receive {trace_record173,T2} -> T2 after 5000 -> erlang:error(live_trace_timeout) end,
        LiveExpected={trace,self(),'receive',LiveAck,[node(),Target,LiveAck]},
        true=LiveActual=:=LiveExpected,

        Resumed=erlang:resume_process(Target),
        Values={{missing,MissingResult,MissingAck,true,Before,AfterMissing,MissingTokenPreserved},
                {live,LiveResult,LiveAck,true,Resumed,BeforeLive,AfterLive,LiveTokenPreserved,ResetMatches},[missing,live]},
        Parent!{self(),{Values,[{MissingActual,MissingExpected},{LiveActual,LiveExpected}]}}
    after
        _=erlang:trace(self(),false,['receive']),
        _=erlang:trace_pattern('receive',true),
        _=erlang:seq_trace(sequential_trace_token,[]),
        exit(Target,kill),
        receive {'DOWN',TargetRef,process,Target,_} -> ok after 5000 -> erlang:error(target_cleanup_timeout) end,
        Tracer!stop173,
        receive {'DOWN',TracerRef,process,Tracer,normal} -> ok after 5000 -> erlang:error(tracer_cleanup_timeout) end
    end.

held(Owner) -> Owner!{ready173,self()},receive stop173 -> ok end.

sink(Owner) ->
    receive
        {trace,_, 'receive',_,_}=Trace -> Owner!{trace_record173,Trace},sink(Owner);
        stop173 -> ok
    end.

capture([Out]) ->
    Whole={Values,Pairs}=observe(),
    ok=file:write_file(Out++"/values.etf",term_to_binary(Values,[{minor_version,2}])),
    ok=file:write_file(Out++"/trace-pairs.etf",term_to_binary(Pairs,[{minor_version,2}])),
    ok=file:write_file(Out++"/whole.etf",term_to_binary(Whole,[{minor_version,2}])),
    halt().
