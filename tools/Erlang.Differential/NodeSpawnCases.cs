namespace Erlang.Differential;

public static class NodeSpawnCases
{
    public static string? FixtureName(string function, int arity) => function is ErlangModuleNames.Spawn or ErlangModuleNames.SpawnLink or ErlangModuleNames.SpawnMonitor && arity is 2 or 4 ? "erlang-node-spawn-local-" + function + "-" + arity : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-node-spawn-" + name,
            "-module(node_spawn_contract). -export([run/0,worker/1,tuple_child/0]). run()->" + body + ",done. worker(Parent)->Parent!{ready,self(),group_leader()},receive stop->ok end. tuple_child()->whereis(node_spawn_owner)!{tuple_ready,self()}.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach (string function in new[] { ErlangModuleNames.Spawn, ErlangModuleNames.SpawnLink, ErlangModuleNames.SpawnMonitor })
            foreach (int arity in new[] { 2, 4 })
            {
                string args = arity == 2 ? "fun()->worker(Self) end" : "node_spawn_contract,worker,[Self]";
                string binding = function == ErlangModuleNames.SpawnMonitor ? "{P,Ref}" : "P";
                string stop = function == ErlangModuleNames.SpawnMonitor ? ",P!stop,receive {'DOWN',Ref,process,P,normal}->ok after 1000->error(no_down) end" : ",P!stop";
                Add(
                    "local-" + function + "-" + arity,
                    "Self=self(),Leader=group_leader()," + binding + "=erlang:" + function + "(node()," + args + "),receive {ready,P,Leader}->ok after 1000->error(no_child) end" + stop
                );
                if (function == ErlangModuleNames.SpawnLink)
                    Add(
                        "remote-" + function + "-" + arity,
                        "_=process_flag(trap_exit,true),Self=self(),P=erlang:" + function + "('missing@invalid'," + args + "),true=is_pid(P),N=node(P),N=node(),receive {'EXIT',P,noconnection}->ok after 1000->error(no_exit) end,receive {ready,_,_}->error(remote_called) after 0->ok end"
                    );
                else if (function == ErlangModuleNames.SpawnMonitor)
                    Add(
                        "remote-" + function + "-" + arity,
                        "Self=self(),{P,Ref}=erlang:" + function + "('missing@invalid'," + args + "),true=is_pid(P),N=node(P),N=node(),receive {'DOWN',Ref,process,P,noconnection}->ok after 1000->error(no_down) end,receive {ready,_,_}->error(remote_called) after 0->ok end"
                    );
                else
                    Add(
                        "remote-" + function + "-" + arity,
                        "Self=self(),P=erlang:" + function + "('missing@invalid'," + args + "),true=is_pid(P),N=node(P),N=node(),receive {ready,_,_}->error(remote_called) after 0->ok end"
                    );
                Add(
                    "bad-node-" + function + "-" + arity,
                    "Self=self(),try erlang:" + function + "(42," + args + ") of _->error(accepted) catch error:badarg->ok end"
                );
            }
        Add(
            "tuple-callback",
            "Self=self(),true=register(node_spawn_owner,Self),P=erlang:spawn(node(),{node_spawn_contract,tuple_child}),true=is_pid(P),Ref=monitor(process,P),receive {'DOWN',Ref,process,P,{{badfun,_},_}}->ok;{'DOWN',Ref,process,P,noproc}->ok after 1000->error(no_down) end,receive {tuple_ready,_}->error(unexpected_tuple_apply) after 0->ok end"
        );
        foreach (string function in new[] { ErlangModuleNames.Spawn, ErlangModuleNames.SpawnLink, ErlangModuleNames.SpawnMonitor })
        {
            foreach (string callback in new[] { "atom", "42", "[]", "{}", "{wrong}", "{42,wrong}" })
                Add(
                    "bad-callback-" + function + "-" + cases.Count,
                    "try erlang:" + function + "(node()," + callback + ") of _->error(accepted) catch error:badarg->ok end"
                );
            Add(
                "bad-mfa-list-" + function,
                "try erlang:" + function + "(node(),node_spawn_contract,worker,[self()|tail]) of _->error(accepted) catch error:badarg->ok end"
            );
            Add(
                "bad-remote-list-" + function,
                "try erlang:" + function + "('missing@invalid',node_spawn_contract,worker,[self()|tail]) of _->error(accepted) catch error:badarg->ok end"
            );
            Add(
                "bad-mfa-name-" + function,
                "try erlang:" + function + "(node(),42,worker,[]) of _->error(accepted) catch error:badarg->ok end"
            );
            if (function != ErlangModuleNames.Spawn)
                Add(
                    "bad-callback-arity-" + function,
                    "try erlang:" + function + "(node(),fun(_)->ok end) of _->error(accepted) catch error:badarg->ok end"
                );
        }
        Add(
            "error-frame",
            "try erlang:spawn_monitor(42,fun()->ok end) of _->error(accepted) catch error:badarg:S->[{erlang,spawn_monitor,[42,_],L}|_]=S,{error_info,#{module:=erl_erts_errors}}=lists:keyfind(error_info,1,L) end"
        );

        return cases;
    }
}
