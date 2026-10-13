namespace Erlang.Differential;

public static class ProcessInfoObservableCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-process-info-observable-" + name,
            "-module(info_observable). -export([run/0]). run()->" + body + ",done. " +
            "worker(Parent)->put(kept,{value,[1,2]}),Parent!{ready,self()},receive finish->ok end. " +
            "stack_probe()->S=process_info(self(),current_stacktrace),put(stack_marker,seen),S. " +
            "wait_status(P,State,0)->error({no_state,P,State});wait_status(P,State,N)->case process_info(P,status) of {status,State}->ok;_->receive after 1->ok end,wait_status(P,State,N-1) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("dictionary-empty", "{dictionary,[]}=process_info(self(),dictionary)");
        Add(
            "dictionary-real",
            "put(atom,{value,[1,2]}),put({key,1},other),{dictionary,D}=process_info(self(),dictionary),[{atom,{value,[1,2]}},{{key,1},other}]=lists:sort(D)"
        );
        Add(
            "dictionary-lookup",
            "put({key,[1|tail]},{kept,[1,2]}),{{dictionary,{key,[1|tail]}},{kept,[1,2]}}=process_info(self(),{dictionary,{key,[1|tail]}})"
        );
        Add("dictionary-absent", "{{dictionary,absent},undefined}=process_info(self(),{dictionary,absent})");
        Add(
            "dictionary-exact",
            "put(1,integer),put(1.0,float),{{dictionary,1},integer}=process_info(self(),{dictionary,1}),{{dictionary,1.0},float}=process_info(self(),{dictionary,1.0})"
        );
        Add(
            "dictionary-lookup-list",
            "put(key,value),[{{dictionary,key},value},{dictionary,[{key,value}]},{{dictionary,key},value}]=process_info(self(),[{dictionary,key},dictionary,{dictionary,key}])"
        );
        Add(
            "dictionary-update",
            "put(kept,first),{dictionary,[{kept,first}]}=process_info(self(),dictionary),put(kept,last),{dictionary,[{kept,last}]}=process_info(self(),dictionary),erase(kept),{dictionary,[]}=process_info(self(),dictionary)"
        );
        Add(
            "messages-empty",
            "{messages,[]}=process_info(self(),messages),{message_queue_len,0}=process_info(self(),message_queue_len)"
        );
        Add(
            "messages-real",
            "self()!first,self()!{second,[1|tail]},{messages,[first,{second,[1|tail]}]}=process_info(self(),messages),{message_queue_len,2}=process_info(self(),message_queue_len),receive first->ok end,{messages,[{second,[1|tail]}]}=process_info(self(),messages)"
        );
        Add(
            "messages-selective",
            "self()!kept,self()!chosen,self()!last,receive chosen->ok end,[{messages,[kept,last]},{message_queue_len,2}]=process_info(self(),[messages,message_queue_len])"
        );
        Add(
            "messages-duplicates",
            "self()!same,self()!same,[{messages,[same,same]},{message_queue_len,2},{messages,[same,same]}]=process_info(self(),[messages,message_queue_len,messages])"
        );
        Add(
            "messages-nil",
            "self()![],self()!kept,[{message_queue_len,2},{messages,[[],kept]}]=process_info(self(),[message_queue_len,messages])"
        );
        Add("status-self", "{status,running}=process_info(self(),status)");
        Add("current-self", "{current_function,{info_observable,run,0}}=process_info(self(),current_function)");
        Add("stack-self", "{current_stacktrace,[{info_observable,stack_probe,0,_}|_]}=stack_probe()");
        Add("stack-default-depth", "{current_stacktrace,Stack}=stack_probe(),true=length(Stack)>0,true=length(Stack)=<8");
        Add(
            "monitors-empty",
            "S=self(),P=spawn(fun()->Outcome=try {monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(self(),monitored_by),done catch Class:Reason->{Class,Reason} end,S!{probe,self(),Outcome},receive finish->ok end end),receive {probe,P,done}->ok;{probe,P,Failure}->error({probe_failed,Failure}) after 5000->error(no_probe) end,M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok after 5000->error(no_down) end"
        );
        Add(
            "self-monitor",
            "S=self(),P=spawn(fun()->Outcome=try R=monitor(process,self()),{monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(self(),monitored_by),true=demonitor(R),done catch Class:Reason->{Class,Reason} end,S!{probe,self(),Outcome},receive finish->ok end end),receive {probe,P,done}->ok;{probe,P,Failure}->error({probe_failed,Failure}) after 5000->error(no_probe) end,M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok after 5000->error(no_down) end"
        );
        Add(
            "clock-monitor",
            "R=monitor(time_offset,clock_service),{monitors,[{time_offset,clock_service}]}=process_info(self(),monitors),true=demonitor(R),{monitors,[]}=process_info(self(),monitors)"
        );
        const string start = "Parent=self(),P=spawn(fun()->worker(Parent) end),receive {ready,P}->ok end,";
        const string finish = ",FinishRef=monitor(process,P),P!finish,receive {'DOWN',FinishRef,process,P,normal}->ok end";
        Add("dictionary-other", start + "{dictionary,[{kept,{value,[1,2]}}]}=process_info(P,dictionary)" + finish);
        Add("lookup-other", start + "{{dictionary,kept},{value,[1,2]}}=process_info(P,{dictionary,kept})" + finish);
        Add(
            "messages-other",
            start + "P!first,P![],[{messages,[first,[]]},{message_queue_len,2}]=process_info(P,[messages,message_queue_len])" + finish
        );
        Add(
            "messages-queue-len-first",
            start + "P!first,P!last,[{message_queue_len,2},{messages,[first,last]},{message_queue_len,2}]=process_info(P,[message_queue_len,messages,message_queue_len])" + finish
        );
        Add("status-waiting", start + "ok=wait_status(P,waiting,1000),{status,waiting}=process_info(P,status)" + finish);
        Add(
            "status-snapshot-waiting",
            start + "ok=wait_status(P,waiting,1000),[{status,waiting},{dictionary,[{kept,{value,[1,2]}}]},{message_queue_len,0}]=process_info(P,[status,dictionary,message_queue_len])" + finish
        );
        Add("current-other", start + "{current_function,{info_observable,worker,1}}=process_info(P,current_function)" + finish);
        Add("stack-other", start + "{current_stacktrace,[{info_observable,worker,1,_}|_]}=process_info(P,current_stacktrace)" + finish);
        Add(
            "suspended-snapshot",
            start + "true=erlang:suspend_process(P),P!kept,[{status,suspended},{messages,[kept]},{message_queue_len,1},{dictionary,[{kept,{value,[1,2]}}]}]=process_info(P,[status,messages,message_queue_len,dictionary]),true=erlang:resume_process(P)" + finish
        );
        Add(
            "monitors-other",
            start + "R=monitor(process,P),{monitors,[{process,P}]}=process_info(self(),monitors),{monitored_by,[Parent]}=process_info(P,monitored_by),true=demonitor(R),{monitors,[]}=process_info(self(),monitors),{monitored_by,[]}=process_info(P,monitored_by)" + finish
        );
        Add(
            "duplicate-monitors",
            start + "A=monitor(process,P),B=monitor(process,P),{monitors,[{process,P},{process,P}]}=process_info(self(),monitors),{monitored_by,[Parent,Parent]}=process_info(P,monitored_by),true=demonitor(A),{monitored_by,[Parent]}=process_info(P,monitored_by),true=demonitor(B)" + finish
        );
        Add(
            "named-monitor",
            start + "true=register(info_named,P),R=monitor(process,info_named),N=node(),{monitors,[{process,{info_named,N}}]}=process_info(self(),monitors),{monitored_by,[Parent]}=process_info(P,monitored_by),true=demonitor(R),true=unregister(info_named)" + finish
        );
        Add(
            "monitor-death-admission",
            "{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->ok end,{monitors,[]}=process_info(self(),monitors)"
        );
        Add(
            "signal-exit-before-query",
            start + "R=monitor(process,P),exit(P,kill),undefined=process_info(P,dictionary),receive {'DOWN',R,process,P,killed}->ok end"
        );
        Add(
            "trapped-exit-messages",
            "process_flag(trap_exit,true),Parent=self(),P=spawn_link(fun()->receive finish->exit(reason) end end),R=monitor(process,P),P!finish,receive {'DOWN',R,process,P,reason}->ok end,[{messages,[{'EXIT',P,reason}]},{message_queue_len,1}]=process_info(self(),[messages,message_queue_len])"
        );
        const string dead = "{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->ok end,";
        foreach (string key in new[] { "dictionary", "{dictionary,any}", "messages", "message_queue_len", "monitors", "monitored_by", "status", "current_function", "current_stacktrace", "[dictionary,messages,status]" })
            Add("dead-" + cases.Count, dead + "undefined=process_info(P," + key + ")");
        foreach (string key in new[] { "{dictionary}", "{dictionary,key,extra}", "{wrong,key}", "[messages|tail]", "[dictionary,{wrong,key}]" })
            Add("invalid-" + cases.Count, "try process_info(self()," + key + ") of _->error(accepted) catch error:badarg->ok end");
        Add(
            "foreign",
            "P=binary_to_term(<<131,88,119,12,102,111,114,101,105,103,110,64,104,111,115,116,0,0,0,1,0,0,0,0,0,0,0,0>>),try process_info(P,dictionary) of _->error(accepted) catch error:badarg->ok end"
        );

        Add(
            "signal-exit-before-status",
            start + "R=monitor(process,P),exit(P,kill),undefined=process_info(P,status),receive {'DOWN',R,process,P,killed}->ok end"
        );
        Add(
            "trapped-exit-before-status",
            "Parent=self(),P=spawn(fun()->process_flag(trap_exit,true),Parent!{ready,self()},receive finish->ok end end),receive {ready,P}->ok end,ok=wait_status(P,waiting,1000),exit(P,reason),{status,State}=process_info(P,status),true=lists:member(State,[running,runnable,waiting]),{messages,[{'EXIT',Parent,reason}]}=process_info(P,messages)" + finish
        );

        return cases;
    }
}
