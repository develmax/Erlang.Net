namespace Erlang.Differential;

public static class SpawnRequestCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ProcessSpawnRequestNames.Function ? "erlang-spawn-request-call-" + arity : function == ProcessSpawnRequestNames.AbandonFunction ? "erlang-spawn-request-abandon-unknown" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-spawn-request-" + name,
            "-module(spawn_request_contract). -export([run/0,worker/1,empty/0]). run()->" + body + ",done. worker(S)->S!{ready,self()},receive stop->ok end. empty()->ok.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach ((int arity, string call) in new[]
        {
            (1, "spawn_request(fun()->worker(S) end)"),
            (2, "spawn_request(fun()->worker(S) end,[])"),
            (3, "spawn_request(spawn_request_contract,worker,[S])"),
            (4, "spawn_request(spawn_request_contract,worker,[S],[])"),
            (5, "spawn_request(node(),spawn_request_contract,worker,[S],[])")
        })
            Add(
                "call-" + arity,
                "S=self(),R=" + call + ",true=is_reference(R),receive {spawn_reply,R,ok,P}->true=is_pid(P),receive {ready,P}->ok after 5000->error(no_child) end,P!stop after 5000->error(no_reply) end,false=spawn_request_abandon(R)"
            );
        Add(
            "node-fun-two",
            "R=spawn_request(node(),fun()->ok end),receive {spawn_reply,R,ok,P}->true=is_pid(P) after 5000->error(no_reply) end"
        );
        Add(
            "node-fun-three",
            "R=spawn_request(node(),fun()->ok end,[]),receive {spawn_reply,R,ok,P}->true=is_pid(P) after 5000->error(no_reply) end"
        );
        Add(
            "node-mfa-four",
            "R=spawn_request(node(),spawn_request_contract,empty,[]),receive {spawn_reply,R,ok,P}->true=is_pid(P) after 5000->error(no_reply) end"
        );
        foreach (string options in new[] { "[monitor]", "[monitor,monitor]", "[{monitor,[]}]", "[{monitor,[priority]}]", "[{monitor,[{tag,old}]},monitor]", "[{monitor,[{tag,old}]},{monitor,[]}]" })
            Add(
                "monitor-order-" + cases.Count,
                "R=spawn_request(fun()->ok end," + options + "),receive {spawn_reply,R,ok,P}->receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end;Other->error({reply_order,Other}) after 5000->error(no_reply) end"
            );
        Add(
            "monitor-custom-tag",
            "R=spawn_request(fun()->ok end,[{monitor,[{tag,{custom,42}},priority]}]),receive {spawn_reply,R,ok,P}->receive {{custom,42},R,process,P,normal}->ok after 5000->error(no_down) end after 5000->error(no_reply) end"
        );
        Add(
            "child-error-after-success",
            "R=spawn_request(missing_spawn_request_module,missing,[],[monitor]),receive {spawn_reply,R,ok,P}->receive {'DOWN',R,process,P,{undef,_}}->ok after 5000->error(no_down) end after 5000->error(no_reply) end"
        );
        Add(
            "reply-tag",
            "R=spawn_request(fun()->ok end,[{reply_tag,{custom,42}}]),receive {{custom,42},R,ok,P}->true=is_pid(P) after 5000->error(no_reply) end"
        );
        Add(
            "reply-overwrite",
            "R=spawn_request(fun()->ok end,[{reply,no},{reply,yes},{reply_tag,old},{reply_tag,new}]),receive {new,R,ok,P}->true=is_pid(P) after 5000->error(no_reply) end"
        );
        foreach (string mode in new[] { "yes", "success_only" })
            Add(
                "success-" + mode,
                "R=spawn_request(fun()->ok end,[{reply," + mode + "}]),receive {spawn_reply,R,ok,P}->true=is_pid(P) after 5000->error(no_reply) end"
            );
        foreach (string mode in new[] { "no", "error_only" })
            Add(
                "success-suppressed-" + mode,
                "R=spawn_request(fun()->ok end,[monitor,{reply," + mode + "}]),receive {'DOWN',R,process,P,normal}->true=is_pid(P);Other->error({unexpected,Other}) after 5000->error(no_down) end,false=spawn_request_abandon(R)"
            );
        foreach (string options in new[] { "[wrong]", "[{link,[wrong]}]", "[{link,wrong}]", "[{monitor,wrong}]", "[{monitor,[wrong]}]", "[{reply,wrong}]", "[{reply_tag,a,b}]" })
            Add(
                "bad-options-" + cases.Count,
                "R=spawn_request(fun()->error(executed) end," + options + "),true=is_reference(R),receive {spawn_reply,R,error,badopt}->ok after 5000->error(no_badopt) end,false=spawn_request_abandon(R)"
            );
        Add(
            "bad-options-custom-tag",
            "R=spawn_request(fun()->error(executed) end,[wrong,{reply_tag,{custom,42}}]),receive {{custom,42},R,error,badopt}->ok after 5000->error(no_badopt) end"
        );
        foreach (string mode in new[] { "no", "success_only" })
            Add(
                "bad-options-suppressed-" + mode,
                "R=spawn_request(fun()->error(executed) end,[wrong,{reply," + mode + "}]),true=is_reference(R),receive {spawn_reply,R,_,_}->error(unexpected_reply) after 0->ok end,false=spawn_request_abandon(R)"
            );
        Add(
            "bad-options-error-only",
            "R=spawn_request(fun()->error(executed) end,[{reply,error_only},wrong]),receive {spawn_reply,R,error,badopt}->ok after 5000->error(no_badopt) end"
        );
        foreach (string options in new[] { "wrong", "[link|tail]", "[wrong|tail]" })
            Add(
                "improper-options-" + cases.Count,
                "try spawn_request(spawn_request_contract,empty,[]," + options + ") of _->error(accepted) catch error:badarg:S->[{erlang,spawn_request,[_,_,_,_],L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,L) end"
            );
        foreach (string call in new[] { "spawn_request(wrong)", "spawn_request(fun(_)->ok end)", "spawn_request({spawn_request_contract,empty})", "spawn_request(42,fun()->ok end)", "spawn_request(wrong,[])", "spawn_request(42,empty,[])", "spawn_request(spawn_request_contract,42,[])", "spawn_request(spawn_request_contract,empty,wrong)", "spawn_request(spawn_request_contract,empty,[a|tail])", "spawn_request(42,spawn_request_contract,empty,[],[])" })
            Add("invalid-types-" + cases.Count, "try " + call + " of _->error(accepted) catch error:badarg->ok end");
        Add(
            "link-monitor",
            "process_flag(trap_exit,true),S=self(),R=spawn_request(fun()->worker(S) end,[link,monitor]),receive {spawn_reply,R,ok,P}->{links,L}=process_info(P,links),true=lists:member(S,L),P!stop,receive {'EXIT',P,normal}->ok after 5000->error(no_exit) end,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end after 5000->error(no_reply) end"
        );
        foreach (string mode in new[] { "explicit_unalias", "demonitor", "reply_demonitor" })
            Add(
                "monitor-alias-" + mode,
                "R=spawn_request(fun()->receive {reference,A}->A!reply,receive stop->ok end end end,[{monitor,[{alias," + mode + "}]}]),receive {spawn_reply,R,ok,P}->P!{reference,R},receive reply->ok after 5000->error(no_alias_reply) end,true=demonitor(R,[flush]),P!stop after 5000->error(no_reply) end"
            );
        Add(
            "remote-error",
            "R=spawn_request('missing@localhost',fun()->error(executed) end,[monitor]),receive {spawn_reply,R,error,noconnection}->ok after 5000->error(no_disconnect) end,false=spawn_request_abandon(R)"
        );
        Add("abandon-unknown", "false=spawn_request_abandon(make_ref())");
        Add(
            "remote-unknown-option-forwarding",
            "R=spawn_request('missing@localhost',fun()->error(executed) end,[wrong,{reply_tag,remote_tag}]),receive {remote_tag,R,error,noconnection}->ok after 5000->error(no_disconnect) end"
        );
        Add(
            "remote-invalid-monitor-first-error",
            "R=spawn_request('missing@localhost',fun()->error(executed) end,[{monitor,[wrong]},{reply_tag,late}]),receive {spawn_reply,R,error,badopt}->ok after 5000->error(no_badopt) end"
        );
        Add(
            "remote-reply-mask-before-invalid",
            "R=spawn_request('missing@localhost',fun()->error(executed) end,[{reply,no},{reply,wrong}]),true=is_reference(R),receive {spawn_reply,R,_,_}->error(unexpected_reply) after 0->ok end"
        );
        Add(
            "remote-invalid-reply-first-error",
            "R=spawn_request('missing@localhost',fun()->error(executed) end,[{reply,wrong},{reply,no}]),receive {spawn_reply,R,error,badopt}->ok after 5000->error(no_badopt) end"
        );
        Add("abandon-invalid", "try spawn_request_abandon(42) of _->error(accepted) catch error:badarg->ok end");
        Add(
            "pending-demonitor-and-no-duplicate",
            "R=spawn_request(fun()->ok end,[monitor]),B=demonitor(R,[info]),receive {spawn_reply,R,ok,P}->case B of true->receive {'DOWN',R,process,P,_}->error(down_after_demonitor) after 0->ok end;false->receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end end after 5000->error(no_reply) end,receive {spawn_reply,R,_,_}->error(duplicate_reply) after 0->ok end"
        );
        Add(
            "remote-improper-options-metadata",
            "try spawn_request('missing@localhost',spawn_request_contract,empty,[],[wrong|tail]) of _->error(accepted) catch error:badarg:S->[{erlang,spawn_request,[_,_,_,_,_],L}|_]=S,{error_info,I}=lists:keyfind(error_info,1,L),#{module:=erl_erts_errors}=I,false=maps:is_key(cause,I) end"
        );
        Add(
            "remote-improper-after-invalid-control",
            "R=spawn_request('missing@localhost',fun()->error(executed) end,[{reply,wrong}|tail]),receive {spawn_reply,R,error,badopt}->ok after 5000->error(no_badopt) end"
        );
        Add(
            "local-improper-after-invalid-control",
            "try spawn_request(fun()->error(executed) end,[{reply,wrong}|tail]) of _->error(accepted) catch error:badarg:S->[{erlang,spawn_request,[_,_],L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,L) end"
        );
        Add(
            "abandon-race",
            "R=spawn_request(fun()->ok end,[monitor]),case spawn_request_abandon(R) of true->false=spawn_request_abandon(R),receive {spawn_reply,R,_,_}->error(reply_after_abandon);{'DOWN',R,_,_,_}->error(down_after_abandon) after 20->ok end;false->receive {spawn_reply,R,ok,P}->receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end after 5000->error(no_reply) end end"
        );

        return cases;
    }
}
