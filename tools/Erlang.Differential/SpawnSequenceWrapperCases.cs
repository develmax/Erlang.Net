namespace Erlang.Differential;

public static class SpawnSequenceWrapperCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string clear = "erlang:seq_trace(sequential_trace_token,[])";
        const string label = "erlang:seq_trace(label,label)";

        void Add(string name, string body) => cases.Add(new(
            "erlang-spawn-sequence-wrapper-" + name,
            "-module(spawn_sequence_wrapper_contract). -export([run/0,hold/0,collector/2]). run()->" + body + ",done. hold()->receive stop->ok end. collector(S,L)->receive {seq_trace,_,_}=E->collector(S,[E|L]);{take,S}->S!lists:reverse(L) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "spawn-mfa-tagged-clock",
            label + ",P=spawn(spawn_sequence_wrapper_contract,hold,[]),{serial,{0,1}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{47,48}}=erlang:seq_trace_info(serial)," + clear + ",P!stop"
        );
        Add(
            "spawn-link-tagged-clock",
            label + ",P=spawn_link(spawn_sequence_wrapper_contract,hold,[]),{serial,{0,1}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{47,48}}=erlang:seq_trace_info(serial)," + clear + ",P!stop"
        );
        Add(
            "spawn-monitor-tagged-clock",
            label + ",{P,R}=spawn_monitor(spawn_sequence_wrapper_contract,hold,[]),true=erlang:seq_trace_print(hidden),{serial,{47,48}}=erlang:seq_trace_info(serial)," + clear + ",P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end"
        );
        Add(
            "spawn-opt-callback-tagged-clock",
            label + ",{P,R}=spawn_opt(fun()->receive stop->ok end end,[monitor]),true=erlang:seq_trace_print(hidden),{serial,{47,48}}=erlang:seq_trace_info(serial)," + clear + ",P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end"
        );
        Add(
            "spawn-opt-explicit-serial-clock",
            label + ",erlang:seq_trace(serial,{0,10}),P=spawn_opt(spawn_sequence_wrapper_contract,hold,[],[]),{serial,{0,11}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{207,208}}=erlang:seq_trace_info(serial),erlang:seq_trace(serial,{5,6}),true=erlang:seq_trace_print(reset),{serial,{5,7}}=erlang:seq_trace_info(serial)," + clear + ",P!stop"
        );
        Add(
            "spawn-opt-small-boundary-wrap",
            label + ",erlang:seq_trace(serial,{0,576460752303423485}),P=spawn_opt(spawn_sequence_wrapper_contract,hold,[],[]),{serial,{0,576460752303423486}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{-1,0}}=erlang:seq_trace_info(serial)," + clear + ",P!stop"
        );
        const string setup = "S=self(),T=spawn(fun()->collector(S,[]) end),false=erlang:system_flag(sequential_tracer,T),erlang:seq_trace(label,label),erlang:seq_trace(send,true),erlang:seq_trace('receive',true)";
        const string collect = ",erlang:seq_trace(sequential_trace_token,[]),P!stop,T!{take,S},receive Events->";
        Add(
            "async-node-mfa-entry-payload",
            setup + ",R=spawn_request(node(),spawn_sequence_wrapper_contract,hold,[],[]),{serial,{0,1}}=erlang:seq_trace_info(serial),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{2,3}}=erlang:seq_trace_info(serial)" + collect + "[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{erts_internal,spawn_init,1},[],spawn_reply,[{spawn_sequence_wrapper_contract,hold,[]}]}}},{seq_trace,label,{'receive',{0,1},S,P,_}},{seq_trace,label,{send,{1,2},P,S,_}},{seq_trace,label,{'receive',{1,2},P,S,_}}]=Events after 5000->error(no_events) end after 5000->error(no_reply) end"
        );
        Add(
            "async-node-callback-entry-payload",
            setup + ",F=fun()->receive stop->ok end end,R=spawn_request(node(),F,[]),receive {spawn_reply,R,ok,P}->{serial,{1,2}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{2,3}}=erlang:seq_trace_info(serial)" + collect + "[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{erts_internal,spawn_init,1},[],spawn_reply,[{erlang,apply,[F,[]]}]}}},{seq_trace,label,{'receive',{0,1},S,P,_}},{seq_trace,label,{send,{1,2},P,S,_}},{seq_trace,label,{'receive',{1,2},P,S,_}}]=Events after 5000->error(no_events) end after 5000->error(no_reply) end"
        );

        return cases.AsReadOnly();
    }
}
