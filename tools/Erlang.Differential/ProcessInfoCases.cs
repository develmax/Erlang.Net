namespace Erlang.Differential;

public static class ProcessInfoCases
{
    public static string? FixtureName(string function, int arity) => function == ProcessInfoNames.Function && arity == 2 ? "erlang-process-info-links-self" : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-process-info-" + name,
            "-module(process_info_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("links-self", "{links,[]}=process_info(self(),links)");
        Add("empty-options", "[]=process_info(self(),[])");
        Add(
            "trap-self",
            "{trap_exit,false}=process_info(self(),trap_exit),false=process_flag(trap_exit,true),{trap_exit,true}=process_info(self(),trap_exit)"
        );
        Add("group-leader", "G=group_leader(),{group_leader,G}=process_info(self(),group_leader)");
        Add("unnamed-singleton", "[]=process_info(self(),registered_name)");
        Add("unnamed-list", "[{registered_name,[]}]=process_info(self(),[registered_name])");
        Add(
            "name-change",
            "true=register(info_owner,self()),{registered_name,info_owner}=process_info(self(),registered_name),true=unregister(info_owner),[]=process_info(self(),registered_name)"
        );
        Add(
            "ordered-duplicates",
            "[{registered_name,[]},{trap_exit,false},{registered_name,[]},{links,[]}]=process_info(self(),[registered_name,trap_exit,registered_name,links])"
        );
        Add(
            "linked-child",
            "P=spawn_link(fun()->receive stop->ok end end),{links,[P]}=process_info(self(),links),S=self(),{links,[S]}=process_info(P,links),true=unlink(P),{links,[]}=process_info(P,links),P!stop"
        );
        Add(
            "changed-child-group",
            "G=group_leader(),P=spawn(fun()->receive stop->ok end end),{group_leader,G}=process_info(P,group_leader),true=group_leader(self(),P),S=self(),{group_leader,S}=process_info(P,group_leader),P!stop"
        );
        Add(
            "dead-target",
            "{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,_}->ok after 1000->error(timeout) end,undefined=process_info(P,links),undefined=process_info(P,[])"
        );
        Add("invalid-pid", "try process_info(wrong,links) of _->error(unexpected) catch error:badarg->ok end");
        Add("invalid-key", "try process_info(self(),wrong) of _->error(unexpected) catch error:badarg->ok end");
        Add("invalid-list-key", "try process_info(self(),[links,wrong]) of _->error(unexpected) catch error:badarg->ok end");
        Add("invalid-option-value", "try process_info(self(),1) of _->error(unexpected) catch error:badarg->ok end");
        Add("improper-options", "try process_info(self(),[links|wrong]) of _->error(unexpected) catch error:badarg->ok end");
        Add(
            "invalid-key-before-dead",
            "{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,_}->ok after 1000->error(timeout) end,try process_info(P,wrong) of _->error(unexpected) catch error:badarg->ok end"
        );
        Add(
            "trapped-linked-exit-admission",
            "process_flag(trap_exit,true),P=spawn_link(fun()->receive fail->exit(failed) end end),R=monitor(process,P),P!fail,receive {'DOWN',R,process,P,failed}->ok after 1000->error(timeout) end,{links,[]}=process_info(self(),links),receive {'EXIT',P,failed}->ok after 1000->error(timeout) end"
        );

        return cases;
    }
}
