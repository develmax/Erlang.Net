namespace Erlang.Differential;

public static class RuntimeNodeControlCases
{
    public static string? FixtureName(string function, int arity) => function switch
    {
        RuntimeNodeControlNames.Monitor when arity == 2 => "erlang-node-control-self-two",
        RuntimeNodeControlNames.Monitor when arity == 3 => "erlang-node-control-self-three",
        RuntimeNodeControlNames.MonitorTrap when arity == 3 => "erlang-node-control-trap-direct",
        RuntimeNodeControlNames.Disconnect when arity == 1 => "erlang-node-control-disconnect-absent",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-node-control-" + name,
            "-module(node_control_contract). -export([run/0,init/1,handle_call/3,handle_cast/2]). run()->" + body + ",done. " +
            "init(S)->{ok,S}. " +
            "handle_call({disconnect,_},_,shutdown)->{stop,shutdown,shutdown}; " +
            "handle_call({disconnect,_},_,killed)->{stop,killed,killed}; " +
            "handle_call({disconnect,_},_,noproc)->{stop,noproc,noproc}; " +
            "handle_call({disconnect,_},_,normal)->{stop,normal,normal}; " +
            "handle_call({disconnect,N},_,S)->{reply,{disconnected,N},S}; " +
            "handle_call({passive_cnct,_},_,S)->{reply,S,S}; handle_call(stop,_,S)->{stop,normal,stopped,S}. handle_cast(_,S)->{noreply,S}.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string noDown = ",receive {nodedown,_}->error(unexpected_down) after 0->ok end";
        Add("self-two", "true=erlang:monitor_node(node(),true),true=erlang:monitor_node(node(),false)" + noDown);
        Add("self-three", "true=erlang:monitor_node(node(),true,[]),true=erlang:monitor_node(node(),false,[])" + noDown);
        Add(
            "self-passive",
            "true=erlang:monitor_node(node(),true,[allow_passive_connect]),true=erlang:monitor_node(node(),false,[allow_passive_connect,allow_passive_connect])" + noDown
        );
        foreach (string flag in new[] { "true", "false", "wrong", "42", "[]" })
            foreach (string options in new[] { "", ",[]", ",[allow_passive_connect]" })
                Add(
                    "notalive-" + cases.Count,
                    "try erlang:monitor_node('peer@invalid'," + flag + options + ") of _->error(accepted) catch error:notalive->ok end"
                );
        foreach (string node in new[] { "42", "[]", "[node]", "{node}", "self()", "make_ref()", "<<1>>" })
            foreach (string options in new[] { "", ",[]" })
                Add(
                    "bad-node-" + cases.Count,
                    "try erlang:monitor_node(" + node + ",true" + options + ") of _->error(accepted) catch error:badarg->ok end"
                );
        foreach (string flag in new[] { "wrong", "42", "[]", "self()", "make_ref()" })
            Add(
                "bad-self-flag-" + cases.Count,
                "try erlang:monitor_node(node()," + flag + ") of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string options in new[] { "wrong", "[wrong]", "[allow_passive_connect|tail]", "[{allow_passive_connect}]", "[allow_passive_connect,wrong]" })
            Add(
                "bad-options-first-" + cases.Count,
                "try erlang:monitor_node('peer@invalid',wrong," + options + ") of _->error(accepted) catch error:badarg:S->[{erlang,monitor_node,_,L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,L) end"
            );
        Add(
            "trap-direct",
            "N='peer@invalid',true=erlang:dmonitor_node(N,wrong,[]),receive {nodedown,N}->ok after 1000->error(no_down) end"
        );
        foreach (string node in new[] { "node()", "42", "[]", "{nested,#{key=>value}}" })
            Add(
                "trap-term-" + cases.Count,
                "N=" + node + ",true=erlang:dmonitor_node(N,false,[]),receive {nodedown,N}->ok after 1000->error(no_down) end"
            );
        Add(
            "trap-ignores-nonpassive-option",
            "N='peer@invalid',true=erlang:dmonitor_node(N,wrong,[ignored_option]),receive {nodedown,N}->ok after 1000->error(no_down) end"
        );
        Add(
            "trap-passive-absent",
            "N='peer@invalid',true=erlang:dmonitor_node(N,wrong,[allow_passive_connect]),receive {nodedown,N}->ok after 1000->error(no_down) end"
        );
        Add(
            "trap-member-improper-match",
            "N='peer@invalid',true=erlang:dmonitor_node(N,wrong,[allow_passive_connect|tail]),receive {nodedown,N}->ok after 1000->error(no_down) end"
        );
        foreach (string options in new[] { "wrong", "[other|tail]", "42" })
            Add(
                "trap-bad-member-" + cases.Count,
                "try erlang:dmonitor_node(peer,wrong," + options + ") of _->error(accepted) catch error:badarg->ok end"
            );
        Add("disconnect-absent", "ignored=erlang:disconnect_node('peer@invalid')");
        foreach (string node in new[] { "node()", "42", "[]", "[peer]", "{peer}", "self()", "make_ref()", "<<1>>", "fun()->ok end" })
            Add("disconnect-absence-no-type-guard-" + cases.Count, "ignored=erlang:disconnect_node(" + node + ")");
        const string start = "undefined=whereis(net_kernel),{ok,P}=gen_server:start_link({local,net_kernel},node_control_contract,";
        const string ready = ",[]),Ref=monitor(process,P),";
        const string stop = ",stopped=gen_server:call(P,stop),receive {'DOWN',Ref,process,P,normal}->ok after 1000->error(kernel_not_stopped) end,ignored=erlang:disconnect_node(peer)";
        Add(
            "disconnect-kernel-request",
            start + "false" + ready + "{disconnected,'peer@invalid'}=erlang:disconnect_node('peer@invalid')" + stop
        );
        Add(
            "disconnect-kernel-arbitrary-request",
            start + "false" + ready + "{disconnected,{nested,42}}=erlang:disconnect_node({nested,42})" + stop
        );
        Add(
            "trap-passive-kernel-false",
            start + "false" + ready + "N='peer@invalid',true=erlang:dmonitor_node(N,wrong,[allow_passive_connect]),receive {nodedown,N}->ok after 1000->error(no_down) end" + stop
        );
        Add(
            "trap-passive-kernel-true-rechecks-notalive",
            start + "true" + ready + "try erlang:dmonitor_node('peer@invalid',wrong,[allow_passive_connect]) of _->error(accepted) catch error:notalive->ok end" + stop
        );
        Add(
            "trap-passive-kernel-unexpected",
            start + "42" + ready + "try erlang:dmonitor_node('peer@invalid',true,[allow_passive_connect]) of _->error(accepted) catch error:{case_clause,42}->ok end" + stop
        );

        foreach (string reason in new[] { "noproc", "shutdown", "killed" })
            Add(
                "disconnect-retry-" + reason,
                "undefined=whereis(net_kernel),{ok,P}=gen_server:start({local,net_kernel},node_control_contract," + reason + ",[]),Ref=monitor(process,P),ignored=erlang:disconnect_node(peer),receive {'DOWN',Ref,process,P," + reason + "}->ok after 1000->error(no_kernel_down) end,undefined=whereis(net_kernel)"
            );
        Add(
            "disconnect-normal-not-retried",
            "undefined=whereis(net_kernel),{ok,P}=gen_server:start({local,net_kernel},node_control_contract,normal,[]),Ref=monitor(process,P),try erlang:disconnect_node(peer) of _->error(accepted) catch exit:{normal,{gen_server,call,_}}->ok end,receive {'DOWN',Ref,process,P,normal}->ok after 1000->error(no_kernel_down) end"
        );

        return cases;
    }
}
