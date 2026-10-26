namespace Erlang.Differential;

public static class JointPlatformCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string label = "erlang:seq_trace(label,joint),true=erlang:seq_trace_print(seed)";
        const string token = "{label,joint}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial)";
        const string pattern = "1=erlang:trace_pattern('receive',[{[node(),P,{'$1',true}],[{is_reference,'$1'}],[{message,{{owner,'$1'}}}]}]),1=erlang:trace(S,true,['receive',{tracer,T}])";
        const string barrier = "1=erlang:trace(S,false,['receive']),B=erlang:trace_delivered(S),receive {trace_delivered,S,B}->ok end";
        const string collect = "erlang:seq_trace(sequential_trace_token,[]),T!{take,S,1},receive [{trace,S,'receive',{Ref,true},{owner,Ref}}]->true=is_reference(Ref) after 5000->error(no_owner_trace) end";
        const string helpers = " hold()->receive stop->ok end. queue(S)->receive {drain,S}->receive []->S!drained end end. alias_queue(S)->A=alias([priority]),S!{alias,self(),A},queue(S). collector(S,P,L,N) when is_integer(N),length(L)==N->S!lists:reverse(L);collector(S,P,L,N)->receive {take,S,Count}->collector(S,P,L,Count);{trace,_,_,_,{owner,_}}=E->collector(S,P,[E|L],N);{trace,_,_,[],nil_seen}=E->collector(S,P,[E|L],N);{monitor,P,long_message_queue,_}=E->collector(S,P,[E|L],N);_->collector(S,P,L,N) end.";

        void Add(string name, string body) => cases.Add(new(
            "erlang-platform-joint-" + name,
            "-module(joint_platform_contract). -export([run/0,hold/0,queue/1,alias_queue/1,collector/4]). run()->" + body + ",done." + helpers,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "display-owner-reply-origin-token",
            "S=self(),P=spawn(joint_platform_contract,hold,[]),T=spawn(fun()->collector(S,P,[],none) end)," + pattern + "," + label + ",true=erlang:process_display(P,backtrace)," + token + "," + barrier + "," + collect + ",P!stop"
        );
        Add(
            "display-self-no-rpc-token",
            "S=self(),P=S,T=spawn(fun()->collector(S,P,[],none) end)," + pattern + "," + label + ",true=erlang:process_display(S,backtrace)," + token + "," + barrier + ",erlang:seq_trace(sequential_trace_token,[]),T!{take,S,0},receive []->ok after 5000->error(unexpected_rpc) end"
        );
        Add(
            "display-dead-no-rpc-token",
            "S=self(),{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->ok end,T=spawn(fun()->collector(S,P,[],none) end)," + pattern + "," + label + ",try erlang:process_display(P,backtrace) of _->error(accepted) catch error:badarg->ok end," + token + "," + barrier + ",erlang:seq_trace(sequential_trace_token,[]),T!{take,S,0},receive []->ok after 5000->error(unexpected_rpc) end"
        );
        Add(
            "display-async-spawn-clock",
            "S=self(),T=spawn(fun()->collector(S,none,[],none) end),erlang:seq_trace(label,joint),R=spawn_request(joint_platform_contract,hold,[]),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial)," + pattern + ",true=erlang:process_display(P,backtrace),{label,joint}=erlang:seq_trace_info(label),{serial,{1,2}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(after_display),{serial,{2,3}}=erlang:seq_trace_info(serial)," + barrier + "," + collect + ",P!stop after 5000->error(no_spawn_reply) end"
        );
        foreach (bool alias in new[] { false, true })
        {
            string start = alias ? "P=spawn(joint_platform_contract,alias_queue,[S]),A=receive {alias,P,Ref}->Ref end" : "P=spawn(joint_platform_contract,queue,[S]),A=P";
            Add(
                alias ? "monitor-priority-alias-nil-rpc" : "monitor-ordinary-nil-rpc",
                "S=self()," + start + ",T=spawn(fun()->collector(S,P,[],none) end),undefined=erlang:system_monitor(T,[{long_message_queue,{0,1}}]),1=erlang:trace_pattern('receive',[{[node(),P,{'$1',true}],[{is_reference,'$1'}],[{message,{{owner,'$1'}}}]},{[node(),S,[]],[],[{message,nil_seen}]}]),1=erlang:trace(S,true,['receive',{tracer,T}]),1=erlang:trace(P,true,['receive',{tracer,T}]),A![]," + label + ",true=erlang:process_display(P,backtrace)," + token + ",P!{drain,S},receive drained->ok after 5000->error(no_drain) end," + barrier + ",erlang:seq_trace(sequential_trace_token,[]),T!{take,S,4},receive Events when is_list(Events)->true=lists:member({monitor,P,long_message_queue,true},Events),true=lists:member({monitor,P,long_message_queue,false},Events),true=lists:member({trace,P,'receive',[],nil_seen},Events),4=length(Events),[{trace,S,'receive',{OwnerRef,true},{owner,OwnerRef}}]=[E||{trace,S,'receive',_,{owner,_}}=E<-Events],true=is_reference(OwnerRef),erlang:system_monitor(undefined) after 5000->error(no_joint_events) end"
            );
        }

        return cases.AsReadOnly();
    }
}
