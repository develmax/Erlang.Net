namespace Erlang.Differential;

public static class ProcessInfoMonitorContextCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string start = "S=self(),P=spawn(fun()->S!{ready,self()},receive finish->ok end end),receive {ready,P}->ok after 5000->error(no_ready) end,";
        const string finish = ",Cleanup=monitor(process,P),P!finish,receive {'DOWN',Cleanup,process,P,normal}->ok after 5000->error(no_down) end";

        void Add(string name, string body) => cases.Add(new(
            "erlang-process-info-monitor-context-" + name,
            "-module(info_monitor_context). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add(
            "parent-observer-install-remove",
            start + "[{monitors,[]},{monitored_by,[]}]=process_info(P,[monitors,monitored_by]),R=monitor(process,P),[{monitors,[]},{monitored_by,[S]}]=process_info(P,[monitors,monitored_by]),true=demonitor(R),[{monitors,[]},{monitored_by,[]}]=process_info(P,[monitors,monitored_by])" + finish
        );
        Add(
            "self-monitor-retains-real-observer",
            "S=self(),P=spawn(fun()->S!{ready,self()},receive inspect->Outcome=try {monitored_by,[S]}=process_info(self(),monitored_by),R=monitor(process,self()),[{monitors,[]},{monitored_by,[S]}]=process_info(self(),[monitors,monitored_by]),false=demonitor(R,[info]),[{monitors,[]},{monitored_by,[S]}]=process_info(self(),[monitors,monitored_by]),done catch Class:Reason->{Class,Reason} end,S!{inspected,self(),Outcome},receive finish->ok end end end),receive {ready,P}->ok after 5000->error(no_ready) end,M=monitor(process,P),P!inspect,receive {inspected,P,done}->ok;{inspected,P,Failure}->error({probe_failed,Failure}) after 5000->error(no_inspection) end,true=demonitor(M),{monitored_by,[]}=process_info(P,monitored_by)" + finish
        );
        Add(
            "duplicate-observers-remove-individually",
            start + "R1=monitor(process,P),R2=monitor(process,P),{monitored_by,[S,S]}=process_info(P,monitored_by),true=demonitor(R1),{monitored_by,[S]}=process_info(P,monitored_by),true=demonitor(R2),{monitored_by,[]}=process_info(P,monitored_by)" + finish
        );
        Add(
            "reciprocal-origin-target-shapes",
            "S=self(),P=spawn(fun()->R=monitor(process,S),S!{ready,self()},receive remove_origin->true=demonitor(R),S!{removed,self()},receive finish->ok end end end),receive {ready,P}->ok after 5000->error(no_ready) end,M=monitor(process,P),[{monitors,[{process,S}]},{monitored_by,[S]}]=process_info(P,[monitors,monitored_by]),P!remove_origin,receive {removed,P}->ok after 5000->error(no_removed) end,[{monitors,[]},{monitored_by,[S]}]=process_info(P,[monitors,monitored_by]),true=demonitor(M),[{monitors,[]},{monitored_by,[]}]=process_info(P,[monitors,monitored_by])" + finish
        );

        return cases;
    }
}
