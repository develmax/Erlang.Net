namespace Erlang.Differential;

public static class ProcessExitCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        ("link", 2) => "erlang-exit-link-self",
        ("exit", 3) => "erlang-exit-legacy-trap-self",
        ("exit_signal", 2) => "erlang-exit-signal-trap-self",
        ("exit_signal", 3) => "erlang-exit-signal-priority-alias",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-exit-" + name,
            "-module(exit_signal_contract). -export([run/0]). run()->" + body + ",done. wait_dead(P)->case erlang:is_process_alive(P) of true->erlang:yield(),wait_dead(P);false->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach (string function in new[] { "spawn_monitor", "spawn_link", "spawn" })
        {
            string assignment = function == "spawn_monitor" ? "{P,R}=" : "P=";
            string cleanup = function == "spawn_monitor" ? "receive {'DOWN',R,process,P,normal}->ok after 1000->error(timeout) end" : "wait_dead(P)";
            Add(
                "leader-inheritance-" + function,
                "G=group_leader(),Owner=self(),true=group_leader(Owner,Owner)," + assignment + "erlang:" + function + "(fun()->Owner!{leader,group_leader()} end),true=group_leader(G,Owner),receive {leader,Owner}->ok after 1000->error(timeout) end," + cleanup
            );
        }
        Add(
            "signal-trap-self",
            "false=process_flag(trap_exit,true),P=self(),true=erlang:exit_signal(P,normal),receive {'EXIT',P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "legacy-trap-self",
            "false=process_flag(trap_exit,true),P=self(),true=erlang:exit(P,normal,[]),receive {'EXIT',P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "signal-self-normal-ignored",
            "true=erlang:exit_signal(self(),normal),erlang:yield(),true=erlang:is_process_alive(self())"
        );
        Add(
            "signal-self-normal-options-ignored",
            "true=erlang:exit_signal(self(),normal,[priority]),erlang:yield(),true=erlang:is_process_alive(self())"
        );
        foreach (string function in new[] { "exit", "exit_signal" })
        {
            foreach (string reason in new[] { "normal", "kill", "{shutdown,42}" })
            {
                string expected = reason == "kill" ? "killed" : reason;
                string operation = "erlang:" + function + "(self()," + reason + ",[priority])";
                if (function == "exit_signal" && reason == "normal")
                    operation += ",erlang:yield()";
                else if (function == "exit_signal")
                    operation += ",receive never->ok end";
                Add(
                    function + "-self-" + reason.Replace("{", "").Replace("}", "").Replace(",", "-"),
                    "{P,R}=spawn_monitor(fun()->" + operation + " end),receive {'DOWN',R,process,P," + expected + "}->ok after 1000->error(timeout) end"
                );
            }
            Add(
                function + "-alias-trap",
                "false=process_flag(trap_exit,true),A=erlang:alias(),P=self(),true=erlang:" + function + "(A,{shutdown,42},[]),receive {'EXIT',P,{shutdown,42}}->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
            );
            Add(
                function + "-reply-alias-retained",
                "false=process_flag(trap_exit,true),A=erlang:alias([reply]),P=self(),true=erlang:" + function + "(A,normal,[]),receive {'EXIT',P,normal}->ok after 1000->error(timeout) end,true=erlang:unalias(A)"
            );
            Add(
                function + "-alias-pending-cancel",
                "false=process_flag(trap_exit,true),A=erlang:alias(),true=erlang:" + function + "(A,kill,[]),true=erlang:unalias(A),erlang:yield(),true=erlang:is_process_alive(self())"
            );
            Add(
                function + "-inactive-alias",
                "A=erlang:alias(),true=erlang:unalias(A),true=erlang:" + function + "(A,kill,[]),erlang:yield(),true=erlang:is_process_alive(self())"
            );
            Add(
                function + "-ordinary-reference",
                "true=erlang:" + function + "(make_ref(),kill,[]),erlang:yield(),true=erlang:is_process_alive(self())"
            );
            foreach (string reason in new[] { "kill", "{shutdown,42}" })
                Add(
                    function + "-other-" + (reason == "kill" ? "kill" : "shutdown"),
                    "{P,R}=spawn_monitor(fun()->receive never->ok end end),true=erlang:" + function + "(P," + reason + ",[]),receive {'DOWN',R,process,P," + (reason == "kill" ? "killed" : reason) + "}->ok after 1000->error(timeout) end"
                );
            Add(function + "-dead", "P=spawn(fun()->ok end),wait_dead(P),true=erlang:" + function + "(P,kill,[])");
            Add(
                function + "-foreign",
                "P=erlang:binary_to_term(<<131,88,119,17,\"elsewhere@invalid\",1:32,0:32,0:32>>),true=erlang:" + function + "(P,kill,[priority])"
            );
            int index = 0;
            foreach (string options in new[] { "bad", "[bad]", "[{priority,true}]", "[priority|bad]", "[priority,bad]" })
                Add(
                    function + "-bad-options-" + index++,
                    "ok=try erlang:" + function + "(self(),kill," + options + ") of _->unexpected catch error:badarg->ok end,true=erlang:is_process_alive(self())"
                );
            index = 0;
            foreach (string destination in new[] { "42", "bad", "[]", "{}", "fun()->ok end" })
                Add(
                    function + "-bad-destination-" + index++,
                    "ok=try erlang:" + function + "(" + destination + ",normal,[]) of _->unexpected catch error:badarg->ok end"
                );
        }
        Add(
            "signal-priority-alias",
            "false=process_flag(trap_exit,true),P=self(),A=erlang:alias([priority]),self()!ordinary,true=erlang:exit_signal(A,normal,[priority]),{'EXIT',P,normal}=receive X->X after 1000->error(timeout) end,ordinary=receive Y->Y after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "signal-priority-needs-alias-permission",
            "false=process_flag(trap_exit,true),P=self(),A=erlang:alias(),self()!ordinary,true=erlang:exit_signal(A,normal,[priority]),ordinary=receive X->X after 1000->error(timeout) end,{'EXIT',P,normal}=receive Y->Y after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "signal-priority-needs-request",
            "false=process_flag(trap_exit,true),P=self(),A=erlang:alias([priority]),self()!ordinary,true=erlang:exit_signal(A,normal),ordinary=receive X->X after 1000->error(timeout) end,{'EXIT',P,normal}=receive Y->Y after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "signal-pid-priority-ignored",
            "false=process_flag(trap_exit,true),P=self(),A=erlang:alias([priority]),self()!ordinary,true=erlang:exit_signal(P,normal,[priority]),ordinary=receive X->X after 1000->error(timeout) end,{'EXIT',P,normal}=receive Y->Y after 1000->error(timeout) end,true=erlang:unalias(A)"
        );
        Add(
            "signal-alias-self-normal-ignored",
            "A=erlang:alias(),true=erlang:exit_signal(A,normal),erlang:yield(),true=erlang:is_process_alive(self()),true=erlang:unalias(A)"
        );
        Add(
            "legacy-alias-self-normal-ignored",
            "A=erlang:alias(),true=erlang:exit(A,normal,[]),erlang:yield(),true=erlang:is_process_alive(self()),true=erlang:unalias(A)"
        );
        Add(
            "signal-alias-admitted-retained",
            "false=process_flag(trap_exit,true),P=self(),A=erlang:alias(),true=erlang:exit_signal(A,normal),erlang:yield(),true=erlang:unalias(A),receive {'EXIT',P,normal}->ok after 1000->error(timeout) end"
        );
        Add(
            "signal-kill-cannot-trap",
            "{P,R}=spawn_monitor(fun()->process_flag(trap_exit,true),receive never->ok end end),true=erlang:exit_signal(P,kill),receive {'DOWN',R,process,P,killed}->ok after 1000->error(timeout) end"
        );
        Add("link-self", "true=erlang:link(self(),[priority]),true=erlang:link(self(),[]),true=unlink(self())");
        Add(
            "link-alive",
            "P=spawn(fun()->receive stop->ok end end),true=erlang:link(P,[priority,priority]),true=unlink(P),P!stop,wait_dead(P)"
        );
        Add(
            "link-dead-untrapped-error",
            "P=spawn(fun()->ok end),wait_dead(P),ok=try erlang:link(P,[priority]) of _->unexpected catch error:noproc->ok end,true=erlang:is_process_alive(self())"
        );
        Add(
            "link-dead-priority",
            "false=process_flag(trap_exit,true),P=spawn(fun()->ok end),wait_dead(P),self()!ordinary,true=erlang:link(P,[priority]),{'EXIT',P,noproc}=receive X->X after 1000->error(timeout) end,ordinary=receive Y->Y after 1000->error(timeout) end"
        );
        Add(
            "link-dead-ordinary",
            "false=process_flag(trap_exit,true),P=spawn(fun()->ok end),wait_dead(P),self()!ordinary,true=erlang:link(P,[]),ordinary=receive X->X after 1000->error(timeout) end,{'EXIT',P,noproc}=receive Y->Y after 1000->error(timeout) end"
        );
        Add(
            "link-dead-unlink-cancels",
            "false=process_flag(trap_exit,true),P=spawn(fun()->ok end),wait_dead(P),true=erlang:link(P,[priority]),true=unlink(P),receive {'EXIT',P,_}->error(unexpected) after 0->ok end"
        );
        int badIndex = 0;
        Add(
            "monitor-priority-is-not-alias-exit-permission",
            "false=process_flag(trap_exit,true),P=spawn(fun()->receive stop->ok end end),R=erlang:monitor(process,P,[priority,{alias,explicit_unalias}]),S=self(),self()!ordinary,true=erlang:exit_signal(R,normal,[priority]),ordinary=receive X->X after 1000->error(timeout) end,{'EXIT',S,normal}=receive Y->Y after 1000->error(timeout) end,true=erlang:demonitor(R,[info]),true=erlang:unalias(R),P!stop,wait_dead(P)"
        );
        Add(
            "reply-monitor-alias-exit-retains-monitor",
            "false=process_flag(trap_exit,true),P=spawn(fun()->receive stop->ok end end),R=erlang:monitor(process,P,[{alias,reply_demonitor}]),S=self(),true=erlang:exit_signal(R,normal),receive {'EXIT',S,normal}->ok after 1000->error(timeout) end,true=erlang:demonitor(R,[info]),false=erlang:unalias(R),P!stop,wait_dead(P)"
        );
        Add(
            "link-priority-reset",
            "false=process_flag(trap_exit,true),P=spawn(fun()->ok end),wait_dead(P),self()!ordinary,true=erlang:link(P,[priority]),true=erlang:link(P,[]),ordinary=receive X->X after 1000->error(timeout) end,{'EXIT',P,noproc}=receive Y->Y after 1000->error(timeout) end,receive {'EXIT',P,_}->error(unexpected) after 0->ok end"
        );
        Add(
            "link-priority-upgrade",
            "false=process_flag(trap_exit,true),P=spawn(fun()->ok end),wait_dead(P),self()!ordinary,true=erlang:link(P,[]),true=erlang:link(P,[priority]),{'EXIT',P,noproc}=receive X->X after 1000->error(timeout) end,ordinary=receive Y->Y after 1000->error(timeout) end,receive {'EXIT',P,_}->error(unexpected) after 0->ok end"
        );
        foreach (string options in new[] { "bad", "[bad]", "[{priority,true}]", "[priority|bad]", "[priority,bad]" })
            Add("link-bad-options-" + badIndex++, "ok=try erlang:link(self()," + options + ") of _->unexpected catch error:badarg->ok end");
        badIndex = 0;
        foreach (string destination in new[] { "42", "bad", "[]", "{}", "make_ref()" })
            Add(
                "link-bad-destination-" + badIndex++,
                "ok=try erlang:link(" + destination + ",[priority]) of _->unexpected catch error:badarg->ok end"
            );

        return cases;
    }
}
