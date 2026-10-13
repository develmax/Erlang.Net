namespace Erlang.Differential;

public static class SequenceDestinationCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string setup = "S=self(),T=spawn(fun()->collector(S,[]) end),false=erlang:system_flag(sequential_tracer,T)";
        const string enable = "erlang:seq_trace(label,label),erlang:seq_trace(send,true)";
        const string collect = "erlang:seq_trace(sequential_trace_token,[]),T!{take,S},receive Events->";
        const string reply = "receive reply->ok after 5000->error(no_reply) end";
        const string events = "[{seq_trace,label,{send,{0,1},S,P,payload}},{seq_trace,label,{send,{1,2},P,S,reply}}]=Events after 5000->error(no_trace_events) end";

        void Add(string name, string body) => cases.Add(new(
            "erlang-seq-destination-" + name,
            "-module(seq_destination_contract). -export([run/0,collector/2]). run()->" + body + ",done. collector(S,L)->receive {seq_trace,_,_}=E->collector(S,[E|L]);{seq_trace,_,_,_}=E->collector(S,[E|L]);{take,S}->S!lists:reverse(L) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add(
            "direct-pid",
            setup + ",P=spawn(fun()->receive payload->S!reply end end)," + enable + ",payload=(P!payload)," + reply + "," + collect + events
        );
        Add(
            "registered-atom",
            setup + ",P=spawn(fun()->receive payload->S!reply end end),true=register(seq_destination_target,P)," + enable + ",payload=(seq_destination_target!payload)," + reply + "," + collect + events
        );
        Add(
            "local-name-tuple",
            setup + ",P=spawn(fun()->receive payload->S!reply end end),true=register(seq_destination_target,P),Target={seq_destination_target,node()}," + enable + ",payload=(Target!payload)," + reply + "," + collect + events
        );
        Add(
            "local-name-tuple-send-options",
            setup + ",P=spawn(fun()->receive payload->S!reply end end),true=register(seq_destination_target,P),Target={seq_destination_target,node()}," + enable + ",ok=erlang:send(Target,payload,[nosuspend])," + reply + "," + collect + events
        );
        Add(
            "alias-reference",
            setup + ",A=alias()," + enable + ",erlang:seq_trace('receive',true),payload=(A!payload),receive payload->{serial,{0,1}}=erlang:seq_trace_info(serial) after 5000->error(no_payload) end,true=unalias(A)," + collect + "[{seq_trace,label,{send,{0,1},S,A,payload}},{seq_trace,label,{'receive',{0,1},S,S,payload}}]=Events after 5000->error(no_trace_events) end"
        );
        Add(
            "missing-local-tuple",
            setup + ",undefined=whereis(seq_destination_missing)," + enable + ",payload=({seq_destination_missing,node()}!payload),{serial,{0,0}}=erlang:seq_trace_info(serial)," + collect + "[]=Events after 5000->error(no_collector) end"
        );
        Add(
            "missing-local-atom",
            setup + ",undefined=whereis(seq_destination_missing)," + enable + ",try seq_destination_missing!payload of _->error(accepted) catch error:badarg->ok end,{serial,{0,0}}=erlang:seq_trace_info(serial)," + collect + "[]=Events after 5000->error(no_collector) end"
        );
        Add(
            "invalid-local-tuple",
            setup + "," + enable + ",try {42,node()}!payload of _->error(accepted) catch error:badarg->ok end,try {seq_destination_missing,node(),extra}!payload of _->error(accepted) catch error:badarg->ok end,{serial,{0,0}}=erlang:seq_trace_info(serial)," + collect + "[]=Events after 5000->error(no_collector) end"
        );

        return cases;
    }
}
