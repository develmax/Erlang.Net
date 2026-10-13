namespace Erlang.Differential;

public static class PortLifecycleCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string open = "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[binary])";
        const string foreign = "P=binary_to_term(<<131,89,118,0,11,\"remote@host\",1:32,0:32>>)";

        void Add(string name, string body) => cases.Add(new(
            "erlang-port-observer-" + name,
            "-module(port_observer_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "close-down",
            open + ",R=monitor(port,P),true=erlang:port_close(P),receive {'DOWN',R,port,P,normal}->ok after 5000->error(no_down) end,false=erlang:demonitor(R,[info])"
        );
        Add(
            "named",
            open + ",true=register(port_observer_name,P),R=monitor(port,port_observer_name),true=erlang:port_close(P),N=node(),receive {'DOWN',R,port,{port_observer_name,N},normal}->ok after 5000->error(no_down) end"
        );
        Add(
            "qualified-name",
            open + ",true=register(port_observer_name,P),N=node(),R=monitor(port,{port_observer_name,N}),true=erlang:port_close(P),receive {'DOWN',R,port,{port_observer_name,N},normal}->ok after 5000->error(no_down) end"
        );
        Add(
            "missing-name",
            "R=monitor(port,port_observer_missing),N=node(),receive {'DOWN',R,port,{port_observer_missing,N},noproc}->ok after 5000->error(no_down) end"
        );
        Add(
            "wrong-name-kind",
            "true=register(port_observer_pid,self()),R=monitor(port,port_observer_pid),N=node(),receive {'DOWN',R,port,{port_observer_pid,N},noproc}->ok after 5000->error(no_down) end,true=unregister(port_observer_pid)"
        );
        Add(
            "closed",
            open + ",true=erlang:port_close(P),R=monitor(port,P),receive {'DOWN',R,port,P,noproc}->ok after 5000->error(no_down) end"
        );
        Add(
            "multiple",
            open + ",A=monitor(port,P),B=monitor(port,P),true=erlang:port_close(P),receive {'DOWN',A,port,P,normal}->ok after 5000->error(no_a) end,receive {'DOWN',B,port,P,normal}->ok after 5000->error(no_b) end"
        );
        Add(
            "demonitor-active",
            open + ",R=monitor(port,P),true=erlang:demonitor(R,[info,flush]),false=erlang:demonitor(R,[info]),true=erlang:port_close(P),receive {'DOWN',R,_,_,_}->error(still_active) after 0->ok end"
        );
        Add(
            "custom-tag",
            open + ",R=erlang:monitor(port,P,[{tag,{custom,42}}]),true=erlang:port_close(P),receive {{custom,42},R,port,P,normal}->ok after 5000->error(no_tag) end"
        );
        Add(
            "monitored-by",
            open + ",{monitored_by,[]}=erlang:port_info(P,monitored_by),{monitors,[]}=erlang:port_info(P,monitors),R=monitor(port,P),{monitored_by,[S]}=erlang:port_info(P,monitored_by),S=self(),true=demonitor(R),{monitored_by,[]}=erlang:port_info(P,monitored_by),true=erlang:port_close(P)"
        );
        foreach (string mode in new[] { "explicit_unalias", "demonitor", "reply_demonitor" })
            Add(
                "alias-" + mode,
                open + ",R=erlang:monitor(port,P,[{alias," + mode + "}]),R!reply,receive reply->ok after 5000->error(no_reply) end,true=erlang:port_close(P)," + (mode == "reply_demonitor" ? "receive {'DOWN',R,_,_,_}->error(unwanted_down) after 0->ok end,false=unalias(R)" : "receive {'DOWN',R,port,P,normal}->ok after 5000->error(no_down) end," + (mode == "explicit_unalias" ? "true=unalias(R)" : "false=unalias(R)"))
            );
        foreach (string function in new[] { "exit", "exit_signal" })
            foreach (string reason in new[] { "normal", "shutdown", "kill", "{custom,42}" })
                foreach (bool options in new[] { false, true })
                    Add(
                        function + "-" + reason.Replace('{', '_').Replace('}', '_').Replace(',', '_') + (options ? "-three" : "-two"),
                        "process_flag(trap_exit,true)," + open + ",R=monitor(port,P),true=erlang:" + function + "(P," + reason + (options ? ",[priority]" : string.Empty) + "),receive {'DOWN',R,port,P," + (reason == "kill" ? "killed" : reason) + "}->ok after 5000->error(no_exit_down) end,receive {'EXIT',P," + (reason == "kill" ? "killed" : reason) + "}->ok after 5000->error(no_link_exit) end"
                    );
        Add(
            "priority-monitor",
            open + ",R=erlang:monitor(port,P,[priority]),self()!ordinary,true=erlang:port_close(P),receive M->{'DOWN',R,port,P,normal}=M end,receive ordinary->ok end"
        );
        Add(
            "priority-link",
            "process_flag(trap_exit,true)," + open + ",true=erlang:link(P,[priority]),self()!ordinary,true=erlang:port_close(P),receive M->{'EXIT',P,normal}=M end,receive ordinary->ok end"
        );
        Add(
            "priority-link-reset",
            "process_flag(trap_exit,true)," + open + ",true=erlang:link(P,[priority]),true=link(P),self()!ordinary,true=erlang:port_close(P),receive ordinary->ok end,receive {'EXIT',P,normal}->ok end"
        );
        Add(
            "dead-link-trapped",
            "process_flag(trap_exit,true)," + open + ",true=erlang:port_close(P),receive {'EXIT',P,normal}->ok end,true=link(P),receive {'EXIT',P,noproc}->ok after 5000->error(no_noproc) end"
        );
        Add("dead-link-untrapped", open + ",true=erlang:port_close(P),try link(P) of _->error(accepted) catch error:noproc->ok end");
        foreach (string target in new[] { "self()", "42", "[]", "make_ref()", "{missing,'remote@host'}", "{missing,42}", "{missing,node(),extra}" })
            Add("bad-monitor-" + cases.Count, "try monitor(port," + target + ") of _->error(accepted) catch error:badarg->ok end");
        Add(
            "foreign-rejections",
            foreign + ",try monitor(port,P) of _->error(accepted) catch error:badarg->ok end,try exit(P,kill) of _->error(accepted) catch error:badarg->ok end,try link(P) of _->error(accepted) catch error:badarg->ok end,try unlink(P) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "dead-exit",
            open + ",true=erlang:port_close(P),true=exit(P,kill),true=erlang:exit_signal(P,normal),undefined=erlang:port_info(P)"
        );

        Add(
            "owner-death",
            "Root=self(),C=spawn(fun()->" + open + ",Root!{owned,P},receive stop->ok end end),P=receive {owned,Q}->Q after 5000->error(no_port) end,R=monitor(port,P),C!stop,receive {'DOWN',R,port,P,normal}->ok after 5000->error(no_owner_down) end"
        );
        Add(
            "observer-death",
            open + ",Root=self(),C=spawn(fun()->R=monitor(port,P),Root!{observing,self(),R},receive stop->ok end end),receive {observing,C,_}->ok after 5000->error(no_observer) end,{monitored_by,[C]}=erlang:port_info(P,monitored_by),M=monitor(process,C),true=exit(C,kill),receive {'DOWN',M,process,C,killed}->ok after 5000->error(no_death) end,{monitored_by,[]}=erlang:port_info(P,monitored_by),true=erlang:port_close(P)"
        );
        Add(
            "other-normal-exit",
            "process_flag(trap_exit,true)," + open + ",R=monitor(port,P),Root=self(),spawn(fun()->true=exit(P,normal),Root!sent end),receive sent->ok after 5000->error(not_sent) end,receive {'DOWN',R,port,P,normal}->ok after 5000->error(no_normal_exit) end"
        );
        Add(
            "invalid-exit-options-preserve",
            open + ",try erlang:exit(P,kill,[bad]) of _->error(accepted) catch error:badarg->ok end,try erlang:link(P,[bad]) of _->error(accepted) catch error:badarg->ok end,{connected,S}=erlang:port_info(P,connected),S=self(),true=erlang:port_close(P)"
        );

        return cases;
    }
}
