namespace Erlang.Differential;

public static class SpawnOptionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ProcessSpawnOptionNames.Function ? "erlang-spawn-option-call-" + arity : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-spawn-option-" + name,
            "-module(spawn_option_contract). -export([run/0,worker/1,empty/0]). run()->" + body + ",done. worker(S)->S!{ready,self()},receive stop->ok end. empty()->ok.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach ((int arity, string call) in new[]
        {
            (2, "spawn_opt(fun()->worker(S) end,[monitor])"),
            (3, "spawn_opt(node(),fun()->worker(S) end,[monitor])"),
            (4, "spawn_opt(spawn_option_contract,worker,[S],[monitor])"),
            (5, "spawn_opt(node(),spawn_option_contract,worker,[S],[monitor])")
        })
            Add(
                "call-" + arity,
                "S=self(),{P,R}=" + call + ",true=is_pid(P),true=is_reference(R),receive {ready,P}->ok after 5000->error(no_child) end,P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end"
            );
        foreach (string options in new[] { "[]", "[link]", "[link,link]", "[{link,[]}]", "[{link,[priority]}]", "[{link,[priority]},link]" })
            Add(
                "link-" + cases.Count,
                "process_flag(trap_exit,true),S=self(),P=spawn_opt(fun()->worker(S) end," + options + "),receive {ready,P}->ok after 5000->error(no_child) end,P!stop"
            );
        Add(
            "tuple-callback",
            "{P,R}=spawn_opt({spawn_option_contract,empty},[monitor]),receive {'DOWN',R,process,P,{{badfun,{spawn_option_contract,empty}},_}}->ok after 5000->error(no_down) end"
        );
        Add(
            "tuple-callback-self-node",
            "{P,R}=spawn_opt(node(),{spawn_option_contract,empty},[monitor]),receive {'DOWN',R,process,P,{{badfun,{spawn_option_contract,empty}},_}}->ok after 5000->error(no_down) end"
        );
        Add(
            "arity-one-child-error",
            "F=fun(_)->ok end,{P,R}=spawn_opt(F,[monitor]),receive {'DOWN',R,process,P,{{badarity,{F,[]}},_}}->ok after 5000->error(no_badarity) end"
        );
        foreach (string options in new[] { "[monitor,monitor]", "[{monitor,[]}]", "[{monitor,[priority]}]", "[{monitor,[{tag,custom}]},monitor]", "[{monitor,[{tag,old}]},{monitor,[]}]" })
            Add(
                "monitor-default-" + cases.Count,
                "{P,R}=spawn_opt(fun()->ok end," + options + "),receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end"
            );
        Add(
            "monitor-custom-tag",
            "{P,R}=spawn_opt(fun()->ok end,[{monitor,[{tag,{custom,42}},priority]}]),receive {{custom,42},R,process,P,normal}->ok after 5000->error(no_custom_down) end"
        );
        Add(
            "link-and-monitor",
            "process_flag(trap_exit,true),S=self(),{P,R}=spawn_opt(fun()->worker(S) end,[link,monitor]),receive {ready,P}->ok after 5000->error(no_child) end,{links,L}=process_info(P,links),true=lists:member(S,L),P!stop,receive {'EXIT',P,normal}->ok after 5000->error(no_exit) end,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end"
        );
        foreach (string alias in new[] { "explicit_unalias", "demonitor", "reply_demonitor" })
            Add(
                "monitor-alias-" + alias,
                "{P,R}=spawn_opt(fun()->receive {reference,A}->A!reply,receive stop->ok end end end,[{monitor,[{alias," + alias + "}]}]),P!{reference,R},receive reply->ok after 5000->error(no_reply) end,true=demonitor(R,[flush]),P!stop"
            );
        foreach (string options in new[] { "wrong", "[link|tail]", "[wrong]", "[{link,[wrong]}]", "[{link,wrong}]", "[{monitor,wrong}]", "[{monitor,[wrong]}]", "[{monitor,[{alias,wrong}]}]", "[{monitor,[{tag,a,b}]}]", "[{reply,no}]", "[{reply_tag,tag}]" })
            Add(
                "invalid-options-" + cases.Count,
                "try spawn_opt(spawn_option_contract,empty,[]," + options + ") of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string call in new[] { "spawn_opt(wrong,[])", "spawn_opt(42,fun()->ok end,[])", "spawn_opt(42,empty,[],[])", "spawn_opt(spawn_option_contract,42,[],[])", "spawn_opt(spawn_option_contract,empty,wrong,[])", "spawn_opt(spawn_option_contract,empty,[a|tail],[])", "spawn_opt(42,spawn_option_contract,empty,[],[])" })
            Add("invalid-types-" + cases.Count, "try " + call + " of _->error(accepted) catch error:badarg->ok end");
        Add(
            "badopt-cause",
            "try spawn_opt(spawn_option_contract,empty,[],[wrong]) of _->error(accepted) catch error:badarg:S->[{erlang,spawn_opt,[_,_,_,_],L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,L) end"
        );

        Add(
            "remote-monitor-fun",
            "{P,R}=spawn_opt('missing@localhost',fun()->error(executed) end,[monitor]),true=is_pid(P),receive {'DOWN',R,process,P,noconnection}->ok after 5000->error(no_disconnect) end"
        );
        Add(
            "remote-link-monitor-mfa",
            "process_flag(trap_exit,true),{P,R}=spawn_opt('missing@localhost',spawn_option_contract,empty,[],[link,monitor]),receive {'EXIT',P,noconnection}->ok after 5000->error(no_exit) end,receive {'DOWN',R,process,P,noconnection}->ok after 5000->error(no_down) end"
        );
        Add(
            "remote-tuple-callback",
            "{P,R}=spawn_opt('missing@localhost',{spawn_option_contract,empty},[monitor]),receive {'DOWN',R,process,P,noconnection}->ok after 5000->error(no_disconnect) end"
        );
        Add(
            "remote-fun-arity-rejected",
            "try spawn_opt('missing@localhost',fun(_)->ok end,[]) of _->error(accepted) catch error:badarg->ok end"
        );

        return cases;
    }
}
