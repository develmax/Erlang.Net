namespace Erlang.Differential;

public static class SpawnRequestSequenceCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string clear = "erlang:seq_trace(sequential_trace_token,[])";
        const string setup = "S=self(),T=spawn(fun()->collector(S,[]) end),false=erlang:system_flag(sequential_tracer,T),erlang:seq_trace(label,label),erlang:seq_trace(send,true),erlang:seq_trace('receive',true)";
        const string take = "erlang:seq_trace(sequential_trace_token,[]),T!{take,S},receive Events when is_list(Events)->";

        void Add(string name, string body) => cases.Add(new(
            "erlang-spawn-request-seq-" + name,
            "-module(spawn_request_seq_contract). -export([run/0,hold/0,report/1,collector/2]). run()->" + body + ",done. hold()->receive stop->ok end. report(S)->{serial,Before}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(child),{serial,After}=erlang:seq_trace_info(serial),erlang:seq_trace(sequential_trace_token,[]),S!{child_serials,self(),Before,After},receive stop->ok end. collector(S,L)->receive {seq_trace,_,_}=E->collector(S,[E|L]);{seq_trace,_,_,_}=E->collector(S,[E|L]);{take,S}->S!lists:reverse(L) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "pre-reply-clock",
            "erlang:seq_trace(label,label),R=spawn_request(spawn_request_seq_contract,hold,[]),{serial,{0,1}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(before_reply),{serial,{0,2}}=erlang:seq_trace_info(serial),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(after_reply),{serial,{2,3}}=erlang:seq_trace_info(serial)," + clear + ",P!stop after 5000->error(no_reply) end"
        );
        Add(
            "unmatched-reply",
            "erlang:seq_trace(label,label),R=spawn_request(spawn_request_seq_contract,hold,[]),receive unrelated->error(unexpected) after 0->ok end,{label,label}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial)," + clear + ",P!stop after 5000->error(no_reply) end"
        );
        Add(
            "receive-keeps-higher-clock",
            "erlang:seq_trace(label,label),R=spawn_request(spawn_request_seq_contract,hold,[]),true=erlang:seq_trace_print(first),true=erlang:seq_trace_print(second),{serial,{0,3}}=erlang:seq_trace_info(serial),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(after_reply),{serial,{2,4}}=erlang:seq_trace_info(serial)," + clear + ",P!stop after 5000->error(no_reply) end"
        );
        Add(
            "custom-tag-protocol",
            setup + ",Tag={custom,7},Options=[monitor,{reply_tag,Tag}],R=spawn_request(spawn_request_seq_contract,hold,[],Options),{serial,{0,1}}=erlang:seq_trace_info(serial),receive {Tag,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial)," + clear + ",P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end," + take + "[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{erts_internal,spawn_init,1},Options,Tag,[{spawn_request_seq_contract,hold,[]}]}}},{seq_trace,label,{'receive',{0,1},S,P,_}},{seq_trace,label,{send,{1,2},P,S,{Tag,R,ok,P}}},{seq_trace,label,{'receive',{1,2},P,S,{Tag,R,ok,P}}}]=Events after 5000->error(no_events) end after 5000->error(no_reply) end"
        );
        Add(
            "callback-protocol",
            setup + ",F=fun()->receive stop->ok end end,R=spawn_request(F),{serial,{0,1}}=erlang:seq_trace_info(serial),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial)," + clear + ",P!stop," + take + "[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{erts_internal,spawn_init,1},[],spawn_reply,[{erlang,apply,[F,[]]}]}}},{seq_trace,label,{'receive',{0,1},S,P,_}},{seq_trace,label,{send,{1,2},P,S,{spawn_reply,R,ok,P}}},{seq_trace,label,{'receive',{1,2},P,S,{spawn_reply,R,ok,P}}}]=Events after 5000->error(no_events) end after 5000->error(no_reply) end"
        );
        Add(
            "monitor-reference",
            "erlang:seq_trace(label,label),R=spawn_request(fun()->receive stop->ok end end,[monitor]),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial)," + clear + ",P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(wrong_reference) end after 5000->error(no_reply) end"
        );
        foreach (string mode in new[] { "no", "error_only" })
            Add(
                "suppressed-" + mode,
                setup + ",Options=[monitor,{reply," + mode + "}],R=spawn_request(spawn_request_seq_contract,report,[S],Options),{serial,{0,1}}=erlang:seq_trace_info(serial),receive {spawn_reply,R,_,_}->error(unexpected_reply) after 0->ok end,{serial,{0,1}}=erlang:seq_trace_info(serial),receive {child_serials,P,{1,2},{1,3}}->[]=erlang:seq_trace_info(label),P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(wrong_reference) end," + take + "[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{erts_internal,spawn_init,1},Options,spawn_reply,[{spawn_request_seq_contract,report,[S]}]}}},{seq_trace,label,{'receive',{0,1},S,P,_}}]=Events after 5000->error(no_events) end after 5000->error(no_child_serials) end"
            );
        Add(
            "error-preserves-on-receive",
            "erlang:seq_trace(label,label),true=erlang:seq_trace_print(before_error),R=spawn_request(fun()->error(executed) end,[wrong]),{serial,{0,1}}=erlang:seq_trace_info(serial),receive unrelated->error(unexpected) after 0->ok end,{label,label}=erlang:seq_trace_info(label),receive {spawn_reply,R,error,badopt}->{label,label}=erlang:seq_trace_info(label) after 5000->error(no_error) end,erlang:seq_trace(label,again),true=erlang:seq_trace_print(after_error),{serial,{0,2}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "suppressed-error-preserves-token",
            "erlang:seq_trace(label,label),true=erlang:seq_trace_print(before_error),R=spawn_request(fun()->error(executed) end,[wrong,{reply,no}]),receive {spawn_reply,R,_,_}->error(unexpected_reply) after 0->ok end,{label,label}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial),false=spawn_request_abandon(R)"
        );

        return cases.AsReadOnly();
    }
}
