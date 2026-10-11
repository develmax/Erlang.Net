namespace Erlang.Differential;

public static class ProcessSpawnCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("group_leader", 0) => "erlang-spawn-group-get-leader",
        ("group_leader", 2) => "erlang-spawn-group-set-self",
        ("spawn", 3) => "erlang-spawn-group-mfa-spawn",
        ("spawn_link", 3) => "erlang-spawn-group-mfa-linked",
        ("spawn_monitor", 3) => "erlang-spawn-group-mfa-monitored",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-spawn-group-" + name,
            "-module(spawn_group_contract). -export([run/0,child/3,callback/0]). run()->" + body + ",done. child(Owner,Tag,Value)->Owner!{Tag,self(),Value,group_leader()},receive stop->ok end. callback()->case whereis(spawn_tuple_owner) of undefined->ok;Owner->Owner!tuple_was_applied end. wait_dead(P)->case erlang:is_process_alive(P) of true->erlang:yield(),wait_dead(P);false->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("get-leader", "true=is_pid(erlang:group_leader())");
        Add("set-self", "G=group_leader(),P=self(),true=group_leader(P,P),P=group_leader(),true=group_leader(G,P),G=group_leader()");
        Add(
            "dead-leader-allowed",
            "G=group_leader(),P=spawn(fun()->ok end),wait_dead(P),true=group_leader(P,self()),P=group_leader(),true=group_leader(G,self())"
        );
        Add(
            "dead-target",
            "P=spawn(fun()->ok end),wait_dead(P),ok=try group_leader(self(),P) of _->unexpected catch error:badarg->ok end"
        );
        Add(
            "child-inherits",
            "G=group_leader(),Owner=self(),true=group_leader(Owner,Owner),P=spawn(fun()->Owner!{leader,group_leader()} end),true=group_leader(G,Owner),receive {leader,Owner}->ok after 1000->error(timeout) end,wait_dead(P)"
        );
        Add(
            "child-change",
            "Owner=self(),P=spawn(fun()->receive query->Owner!{leader,group_leader()} end end),true=group_leader(Owner,P),P!query,receive {leader,Owner}->ok after 1000->error(timeout) end,wait_dead(P)"
        );
        Add(
            "foreign-leader",
            "G=group_leader(),P=erlang:binary_to_term(<<131,88,119,17,\"elsewhere@invalid\",1:32,0:32,0:32>>),true=group_leader(P,self()),P=group_leader(),true=group_leader(G,self())"
        );
        Add(
            "foreign-target-disconnected",
            "G=group_leader(),P=erlang:binary_to_term(<<131,88,119,17,\"elsewhere@invalid\",1:32,0:32,0:32>>),true=group_leader(self(),P),G=group_leader()"
        );
        Add(
            "old-local-target",
            "N=erlang:atom_to_binary(node()),S=byte_size(N),P=erlang:binary_to_term(<<131,88,119,S,N/binary,1:32,0:32,7:32>>),ok=try group_leader(self(),P) of _->unexpected catch error:badarg->ok end"
        );
        int index = 0;
        foreach (string value in new[] { "bad", "42", "[]", "{}", "make_ref()" })
        {
            Add("bad-leader-" + index, "ok=try group_leader(" + value + ",self()) of _->unexpected catch error:badarg->ok end");
            Add("bad-target-" + index++, "ok=try group_leader(self()," + value + ") of _->unexpected catch error:badarg->ok end");
        }
        Add(
            "mfa-spawn",
            "Owner=self(),G=group_leader(),P=erlang:spawn(spawn_group_contract,child,[Owner,plain,#{key=>42}]),receive {plain,P,#{key:=42},G}->ok after 1000->error(timeout) end,P!stop,wait_dead(P)"
        );
        Add(
            "mfa-linked",
            "false=process_flag(trap_exit,true),Owner=self(),G=group_leader(),P=erlang:spawn_link(spawn_group_contract,child,[Owner,linked,42]),receive {linked,P,42,G}->ok after 1000->error(timeout) end,P!stop,receive {'EXIT',P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "mfa-monitored",
            "Owner=self(),G=group_leader(),{P,R}=erlang:spawn_monitor(spawn_group_contract,child,[Owner,monitored,42]),receive {monitored,P,42,G}->ok after 1000->error(timeout) end,P!stop,receive {'DOWN',R,process,P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "mfa-missing-module-child-error",
            "{P,R}=erlang:spawn_monitor(spawn_missing,run,[]),receive {'DOWN',R,process,P,{undef,_}}->ok after 1000->error(timeout) end"
        );
        Add(
            "mfa-missing-export-child-error",
            "{P,R}=erlang:spawn_monitor(spawn_group_contract,missing,[]),receive {'DOWN',R,process,P,{undef,_}}->ok after 1000->error(timeout) end"
        );
        Add(
            "mfa-link-child-error",
            "false=process_flag(trap_exit,true),P=erlang:spawn_link(spawn_missing,run,[]),receive {'EXIT',P,{undef,_}}->ok after 1000->error(timeout) end"
        );
        Add(
            "mfa-immediate-monitor",
            "{P,R}=erlang:spawn_monitor(spawn_group_contract,callback,[]),receive {'DOWN',R,process,P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "callback-tuple",
            "true=register(spawn_tuple_owner,self()),P=erlang:spawn({spawn_group_contract,callback}),R=erlang:monitor(process,P),receive {'DOWN',R,process,P,{{badfun,_},_}}->ok;{'DOWN',R,process,P,noproc}->ok after 1000->error(timeout) end,receive tuple_was_applied->error(unexpected) after 0->ok end"
        );
        Add(
            "callback-wrong-arity-child-error",
            "P=erlang:spawn(fun(_)->ok end),R=erlang:monitor(process,P),receive {'DOWN',R,process,P,{{badarity,_},_}}->ok;{'DOWN',R,process,P,noproc}->ok after 1000->error(timeout) end"
        );
        index = 0;
        foreach (string arguments in new[] { "42,run,[]", "spawn_group_contract,42,[]", "spawn_group_contract,callback,bad", "spawn_group_contract,callback,[a|bad]" })
            foreach (string function in new[] { "spawn", "spawn_link", "spawn_monitor" })
                Add("bad-mfa-" + index++, "ok=try erlang:" + function + "(" + arguments + ") of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string value in new[] { "bad", "42", "{}", "{spawn_group_contract,42}", "{spawn_group_contract,callback,extra}" })
            Add("bad-callback-" + index++, "ok=try erlang:spawn(" + value + ") of _->unexpected catch error:badarg->ok end");

        return cases;
    }
}
