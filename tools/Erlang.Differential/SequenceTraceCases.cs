namespace Erlang.Differential;

public static class SequenceTraceCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function switch
    {
        SequenceTraceNames.Set => "erlang-seq-trace-token-fields",
        SequenceTraceNames.Info => "erlang-seq-trace-info-defaults",
        SequenceTraceNames.Print => "erlang-seq-trace-print-call-" + arity,
        SequenceTraceNames.SystemFlag => "erlang-seq-trace-system-tracer",
        SequenceTraceNames.SystemInfo => "erlang-seq-trace-system-info",
        _ => null
    };

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string clear = "erlang:seq_trace(sequential_trace_token,[])";
        const string setup = "S=self(),T=spawn(fun()->collector(S,[]) end),false=erlang:system_flag(sequential_tracer,T)";
        const string collect = "erlang:seq_trace(sequential_trace_token,[]),T!{take,S},receive Events->";

        void Add(string name, string body) => cases.Add(new(
            "erlang-seq-trace-" + name,
            "-module(seq_trace_contract). -export([run/0,collector/2,empty/0]). run()->" + body + ",done. empty()->ok. collector(S,L)->receive {seq_trace,_,_}=E->collector(S,[E|L]);{seq_trace,_,_,_}=E->collector(S,[E|L]);{take,S}->S!lists:reverse(L) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "info-defaults",
            "[]=erlang:seq_trace_info(label),[]=erlang:seq_trace_info(serial),{send,false}=erlang:seq_trace_info(send),{'receive',false}=erlang:seq_trace_info('receive'),{print,false}=erlang:seq_trace_info(print),{spawn,false}=erlang:seq_trace_info(spawn)"
        );
        Add(
            "token-fields",
            "false=erlang:seq_trace(send,true),0=erlang:seq_trace(label,{label,42}),{0,0}=erlang:seq_trace(serial,{7,11}),S=self(),{1,{label,42},11,S,7}=" + clear + ",[]=" + clear
        );
        foreach ((string flag, int bit) in new[] { ("send", 1), ("'receive'", 2), ("print", 4), ("timestamp", 8), ("strict_monotonic_timestamp", 16), ("monotonic_timestamp", 32) })
            Add(
                "flag-" + bit,
                "false=erlang:seq_trace(" + flag + ",true),true=erlang:seq_trace(" + flag + ",true),{" + flag + ",true}=erlang:seq_trace_info(" + flag + "),S=self(),{" + bit + ",0,0,S,0}=" + clear + ",false=erlang:seq_trace(" + flag + ",false),{" + flag + ",false}=erlang:seq_trace_info(" + flag + ")"
            );
        Add(
            "print-call-1",
            "false=erlang:seq_trace_print(payload),0=erlang:seq_trace(label,label),true=erlang:seq_trace_print(payload),{serial,{0,1}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "print-call-2",
            "false=erlang:seq_trace_print(label,payload),0=erlang:seq_trace(label,label),false=erlang:seq_trace_print(other,payload),true=erlang:seq_trace_print(label,payload),{serial,{0,1}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "print-exact-label",
            "0=erlang:seq_trace(label,{1}),false=erlang:seq_trace_print({1.0},payload),true=erlang:seq_trace_print({1},payload),{serial,{0,1}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "clear-retains-clock",
            "{0,0}=erlang:seq_trace(serial,{7,11}),true=erlang:seq_trace_print(payload),S=self(),{0,0,12,S,7}=" + clear + ",0=erlang:seq_trace(label,next),{serial,{7,0}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(next,payload),{serial,{7,13}}=erlang:seq_trace_info(serial)"
        );
        foreach (string serial in new[] { "{0,-1}", "{-1,-2}", "{-576460752303423488,576460752303423487}" })
            Add(
                "serial-small-boundary-" + cases.Count,
                "{0,0}=erlang:seq_trace(serial," + serial + "),{serial," + serial + "}=erlang:seq_trace_info(serial)"
            );
        Add(
            "serial-wrap",
            "{0,0}=erlang:seq_trace(serial,{2,-1}),true=erlang:seq_trace_print(payload),{serial,{2,0}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(payload),{serial,{2,1}}=erlang:seq_trace_info(serial)"
        );
        foreach (string call in new[] { "erlang:seq_trace(42,true)", "erlang:seq_trace(wrong,true)", "erlang:seq_trace(spawn,true)", "erlang:seq_trace(send,yes)", "erlang:seq_trace(print,1)", "erlang:seq_trace(serial,42)", "erlang:seq_trace(serial,{1})", "erlang:seq_trace(serial,{1,2,3})", "erlang:seq_trace(serial,{1,2.0})", "erlang:seq_trace(serial,{0,576460752303423488})", "erlang:seq_trace(serial,{-576460752303423489,0})", "erlang:seq_trace(sequential_trace_token,{0,0,0,self(),0})", "erlang:seq_trace_info(42)", "erlang:seq_trace_info(wrong)" })
            Add("invalid-" + cases.Count, "try " + call + " of _->error(accepted) catch error:badarg->ok end");
        Add(
            "info-spawn-active-rejected",
            "erlang:seq_trace(label,0),try erlang:seq_trace_info(spawn) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "untraced-receive-clears",
            "self()!plain,0=erlang:seq_trace(label,first),true=erlang:seq_trace_print(payload),receive plain->ok end,[]=erlang:seq_trace_info(label),0=erlang:seq_trace(label,second),true=erlang:seq_trace_print(payload),{serial,{0,2}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "queued-token-snapshots",
            "erlang:seq_trace('receive',true),erlang:seq_trace(label,first),self()!first,first=erlang:seq_trace(label,second),self()!second," + clear + ",receive first->{label,first}=erlang:seq_trace_info(label) end,receive second->{label,second}=erlang:seq_trace_info(label),{serial,{0,2}}=erlang:seq_trace_info(serial) end,true=erlang:seq_trace_print(payload),{serial,{2,3}}=erlang:seq_trace_info(serial)"
        );
        Add("system-info", "{sequential_tracer,false}=erlang:system_info(sequential_tracer)");
        Add(
            "system-tracer",
            setup + ",T=erlang:system_flag(sequential_tracer,false),{sequential_tracer,false}=erlang:system_info(sequential_tracer),false=erlang:system_flag(sequential_tracer,T),{sequential_tracer,T}=erlang:system_info(sequential_tracer),T=erlang:system_flag(sequential_tracer,false),T!{take,S},receive []->ok after 5000->error(no_collector) end"
        );
        Add(
            "actual-print-events",
            setup + ",erlang:seq_trace(label,{label,42}),erlang:seq_trace(print,true),true=erlang:seq_trace_print(first),true=erlang:seq_trace_print({label,42},second),{serial,{0,2}}=erlang:seq_trace_info(serial)," + collect + "[{seq_trace,{label,42},{print,{0,1},S,[],first}},{seq_trace,{label,42},{print,{0,2},S,[],second}}]=Events after 5000->error(no_trace_events) end"
        );
        Add(
            "print-mask-counter",
            setup + ",erlang:seq_trace(label,label),true=erlang:seq_trace_print(hidden),erlang:seq_trace(print,true),true=erlang:seq_trace_print(visible)," + collect + "[{seq_trace,label,{print,{0,2},S,[],visible}}]=Events after 5000->error(no_trace_event) end"
        );
        Add(
            "tracer-self-suppression",
            "S=self(),false=erlang:system_flag(sequential_tracer,S),erlang:seq_trace(print,true),true=erlang:seq_trace_print(payload),{serial,{0,0}}=erlang:seq_trace_info(serial),receive _->error(self_trace) after 0->ok end,S=erlang:system_flag(sequential_tracer,false),true=erlang:seq_trace_print(payload),{serial,{0,1}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "tracer-death",
            setup + ",R=monitor(process,T),exit(T,kill),receive {'DOWN',R,process,T,killed}->ok after 5000->error(no_down) end,{sequential_tracer,false}=erlang:system_info(sequential_tracer),try erlang:system_flag(sequential_tracer,T) of _->error(dead_tracer) catch error:badarg->ok end"
        );
        foreach (string value in new[] { "42", "true", "[]", "make_ref()", "'missing@localhost'" })
            Add(
                "invalid-tracer-" + cases.Count,
                "try erlang:system_flag(sequential_tracer," + value + ") of _->error(accepted) catch error:badarg->ok end"
            );
        Add(
            "send-receive-lamport",
            setup + ",P=spawn(fun()->receive {go,V}->I=erlang:seq_trace_info(serial),S!{report,self(),I,V} end end),erlang:seq_trace(label,label),erlang:seq_trace(send,true),erlang:seq_trace('receive',true),P!{go,42},Report={report,P,{serial,{0,1}},42},receive Report->ok after 5000->error(no_report) end,{serial,{1,2}}=erlang:seq_trace_info(serial)," + collect + "[{seq_trace,label,{send,{0,1},S,P,{go,42}}},{seq_trace,label,{'receive',{0,1},S,P,{go,42}}},{seq_trace,label,{send,{1,2},P,S,Report}},{seq_trace,label,{'receive',{1,2},P,S,Report}}]=Events after 5000->error(no_trace_events) end"
        );
        Add(
            "selective-receive-token-only-on-match",
            setup + ",P=spawn(fun()->receive traced->S!{traced,erlang:seq_trace_info(label)} end,receive untraced->S!{cleared,erlang:seq_trace_info(label)} end end),P!untraced,erlang:seq_trace(label,label),erlang:seq_trace('receive',true),P!traced,receive {traced,{label,label}}->ok after 5000->error(no_traced) end,receive {cleared,[]}->ok after 5000->error(not_cleared) end," + collect + "[{seq_trace,label,{'receive',{0,1},S,P,traced}},{seq_trace,label,{'receive',{1,2},P,S,{traced,{label,label}}}}]=Events after 5000->error(no_trace_events) end"
        );
        foreach (string flag in new[] { "timestamp", "monotonic_timestamp", "strict_monotonic_timestamp" })
        {
            string assertion = flag == "timestamp" ? "{A,B,C}=Stamp,true=is_integer(A),true=is_integer(B),true=is_integer(C)" : flag == "monotonic_timestamp" ? "true=is_integer(Stamp)" : "{A,B}=Stamp,true=is_integer(A),true=is_integer(B)";
            Add(
                "timestamp-" + flag,
                setup + ",erlang:seq_trace(print,true),erlang:seq_trace(" + flag + ",true),true=erlang:seq_trace_print(payload)," + collect + "[{seq_trace,0,{print,{0,1},S,[],payload},Stamp}]=Events," + assertion + " after 5000->error(no_timestamp) end"
            );
        }
        Add(
            "timestamp-precedence",
            setup + ",erlang:seq_trace(print,true),erlang:seq_trace(timestamp,true),erlang:seq_trace(monotonic_timestamp,true),erlang:seq_trace(strict_monotonic_timestamp,true),true=erlang:seq_trace_print(payload)," + collect + "[{seq_trace,0,{print,{0,1},S,[],payload},{A,B,C}}]=Events,true=is_integer(A),true=is_integer(B),true=is_integer(C) after 5000->error(no_timestamp) end"
        );
        Add(
            "spawn-protocol-and-monitor-reference",
            setup + ",erlang:seq_trace(label,label),erlang:seq_trace(send,true),erlang:seq_trace('receive',true),{P,R}=spawn_monitor(seq_trace_contract,empty,[]),{serial,{0,1}}=erlang:seq_trace_info(serial)," + clear + ",T!{take,S},receive Events when is_list(Events)->[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{seq_trace_contract,empty,0},[monitor],spawn_reply,[]}}},{seq_trace,label,{'receive',{0,1},S,P,_}},{seq_trace,label,{send,{1,2},P,S,{spawn_reply,R,ok,P}}},{seq_trace,label,{'receive',{1,2},P,S,{spawn_reply,R,ok,P}}}|_]=Events after 5000->error(no_spawn_trace) end"
        );
        Add(
            "alias-delivery-token",
            setup + ",A=alias(),erlang:seq_trace(label,label),erlang:seq_trace(send,true),erlang:seq_trace('receive',true),A!payload,receive payload->{serial,{0,1}}=erlang:seq_trace_info(serial) after 5000->error(no_payload) end,true=unalias(A)," + collect + "[{seq_trace,label,{send,{0,1},S,A,payload}},{seq_trace,label,{'receive',{0,1},S,S,payload}}]=Events after 5000->error(no_trace_events) end"
        );
        Add(
            "inactive-alias-live-owner-clock",
            "A=alias(),true=unalias(A),erlang:seq_trace(label,label),A!ignored,{serial,{0,1}}=erlang:seq_trace_info(serial),receive ignored->error(delivered) after 0->ok end"
        );
        Add(
            "dead-pid-does-not-advance",
            "P=spawn(fun()->ok end),R=monitor(process,P),receive {'DOWN',R,process,P,_}->ok after 5000->error(no_down) end,erlang:seq_trace(label,label),P!ignored,{serial,{0,0}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "dead-alias-owner-does-not-advance",
            "S=self(),P=spawn(fun()->Alias=alias(),S!{alias,Alias},receive stop->ok end end),A=receive {alias,Ref}->Ref after 5000->error(no_alias) end,R=monitor(process,P),P!stop,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end,erlang:seq_trace(label,label),A!ignored,{serial,{0,0}}=erlang:seq_trace_info(serial)"
        );
        Add(
            "local-name-tuple-send-destination",
            setup + ",P=spawn(fun()->receive payload->S!reply end end),true=register(seq_trace_target,P),Target={seq_trace_target,node()},erlang:seq_trace(label,label),erlang:seq_trace(send,true),Target!payload,receive reply->ok after 5000->error(no_reply) end," + collect + "[{seq_trace,label,{send,{0,1},S,P,payload}},{seq_trace,label,{send,{1,2},P,S,reply}}]=Events after 5000->error(no_trace_events) end"
        );
        Add(
            "strict-timestamp-order",
            setup + ",erlang:seq_trace(print,true),erlang:seq_trace(strict_monotonic_timestamp,true),true=erlang:seq_trace_print(first),true=erlang:seq_trace_print(second)," + collect + "[{seq_trace,0,{print,{0,1},S,[],first},{N1,U1}},{seq_trace,0,{print,{0,2},S,[],second},{N2,U2}}]=Events,true=(N2>=N1),true=(U2>U1) after 5000->error(no_trace_events) end"
        );

        Add(
            "spawn-opt-callback-payload",
            setup + ",F=fun()->receive stop->ok end end,erlang:seq_trace(label,label),erlang:seq_trace(send,true),erlang:seq_trace('receive',true),{P,R}=spawn_opt(F,[link,monitor]),{serial,{0,1}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(hidden),{serial,{47,48}}=erlang:seq_trace_info(serial)," + clear + ",P!stop,T!{take,S},receive Events when is_list(Events)->[{seq_trace,label,{send,{0,1},S,P,{spawn_request,R,S,_,{erlang,apply,2},[link,monitor],spawn_reply,[F,[]]}}},{seq_trace,label,{'receive',{0,1},S,P,_}},{seq_trace,label,{send,{1,2},P,S,{spawn_reply,R,ok,P}}},{seq_trace,label,{'receive',{1,2},P,S,{spawn_reply,R,ok,P}}}|_]=Events after 5000->error(no_spawn_trace) end"
        );
        Add(
            "unmatched-receive-preserves-token",
            "self()!unmatched,erlang:seq_trace(label,label),receive matching->error(unexpected) after 0->ok end,{label,label}=erlang:seq_trace_info(label),receive unmatched->ok end,[]=erlang:seq_trace_info(label)"
        );

        return cases;
    }
}
