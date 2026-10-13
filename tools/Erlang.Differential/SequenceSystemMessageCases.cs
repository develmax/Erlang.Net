namespace Erlang.Differential;

public static class SequenceSystemMessageCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string token = "0=erlang:seq_trace(label,label),true=erlang:seq_trace_print(seed),{serial,{0,1}}=erlang:seq_trace_info(serial)";
        const string preserved = "{label,label}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(after_reply),{serial,{0,2}}=erlang:seq_trace_info(serial)";
        const string live = "P=spawn(fun()->worker() end)";
        const string dead = "{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,_}->ok after 5000->error(no_down) end";

        void Add(string name, string body) => cases.Add(new(
            "erlang-seq-system-" + name,
            "-module(seq_system_message_contract). -export([run/0]). run()->" + body + ",done. worker()->receive {send,S,V}->S!V,worker();stop->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add(
            "gc-sync-live",
            live + "," + token + ",true=erlang:garbage_collect(P)," + preserved + ",P!stop"
        );
        Add(
            "gc-async-live",
            live + ",I=make_ref()," + token + ",async=erlang:garbage_collect(P,[{async,I}]),receive {garbage_collect,I,true}->" + preserved + " after 5000->error(no_gc_reply) end,P!stop"
        );
        Add(
            "gc-async-self",
            "I=make_ref()," + token + ",async=erlang:garbage_collect(self(),[{async,I}]),receive {garbage_collect,I,true}->" + preserved + " after 5000->error(no_gc_reply) end"
        );
        Add(
            "gc-sync-dead",
            dead + "," + token + ",false=erlang:garbage_collect(P)," + preserved
        );
        Add(
            "gc-async-dead",
            dead + ",I=make_ref()," + token + ",async=erlang:garbage_collect(P,[{async,I}]),receive {garbage_collect,I,false}->" + preserved + " after 5000->error(no_gc_reply) end"
        );
        Add(
            "info-owner-dictionary",
            live + "," + token + ",{dictionary,[]}=process_info(P,dictionary)," + preserved + ",P!stop"
        );
        Add(
            "info-owner-messages",
            live + "," + token + ",{messages,[]}=process_info(P,messages)," + preserved + ",P!stop"
        );
        Add(
            "code-check-sync",
            live + "," + token + ",false=erlang:check_process_code(P,seq_system_message_contract)," + preserved + ",P!stop"
        );
        Add(
            "code-check-async",
            live + ",I=make_ref()," + token + ",async=erlang:check_process_code(P,seq_system_message_contract,[{async,I}]),receive {check_process_code,I,false}->" + preserved + " after 5000->error(no_code_reply) end,P!stop"
        );
        Add(
            "saved-calls-owner-rpc",
            live + "," + token + ",0=process_flag(P,save_calls,8)," + preserved + ",P!stop"
        );
        Add(
            "suspend-async-live",
            live + ",I=make_ref()," + token + ",true=erlang:suspend_process(P,[{asynchronous,I}]),receive {I,suspended}->" + preserved + " after 5000->error(no_suspend_reply) end,true=erlang:resume_process(P),P!stop"
        );
        Add(
            "suspend-async-dead",
            dead + ",I=make_ref()," + token + ",true=erlang:suspend_process(P,[{asynchronous,I}]),receive {I,badarg}->" + preserved + " after 5000->error(no_suspend_reply) end"
        );
        Add(
            "send-after-expiry",
            token + ",_=erlang:send_after(0,self(),payload),receive payload->" + preserved + " after 5000->error(no_timer) end"
        );
        Add(
            "start-timer-expiry",
            token + ",T=erlang:start_timer(0,self(),payload),receive {timeout,T,payload}->" + preserved + " after 5000->error(no_timer) end"
        );
        Add(
            "read-timer-async-live",
            "T=erlang:send_after(60000,self(),unused)," + token + ",ok=erlang:read_timer(T,[{async,true}]),receive {read_timer,T,V} when is_integer(V)->" + preserved + " after 5000->error(no_timer_reply) end,_=erlang:cancel_timer(T)"
        );
        Add(
            "cancel-timer-async-live",
            "T=erlang:send_after(60000,self(),unused)," + token + ",ok=erlang:cancel_timer(T,[{async,true}]),receive {cancel_timer,T,V} when is_integer(V)->" + preserved + " after 5000->error(no_timer_reply) end"
        );
        Add(
            "read-timer-async-missing",
            "T=make_ref()," + token + ",ok=erlang:read_timer(T,[{async,true}]),receive {read_timer,T,false}->" + preserved + " after 5000->error(no_timer_reply) end"
        );
        Add(
            "monitor-down-process",
            live + ",M=monitor(process,P)," + token + ",_=erlang:send_after(0,P,stop),receive {'DOWN',M,process,P,normal}->" + preserved + " after 5000->error(no_down) end"
        );
        Add(
            "monitor-down-noproc",
            dead + "," + token + ",I=monitor(process,P),receive {'DOWN',I,process,P,noproc}->" + preserved + " after 5000->error(no_down) end"
        );
        Add(
            "ordinary-message-still-clears",
            live + ",S=self()," + token + ",_=erlang:send_after(0,P,{send,S,payload}),receive payload->[]=erlang:seq_trace_info(label),[]=erlang:seq_trace_info(serial) after 5000->error(no_payload) end,0=erlang:seq_trace(label,again),true=erlang:seq_trace_print(after_plain),{serial,{0,2}}=erlang:seq_trace_info(serial),P!stop"
        );

        return cases.AsReadOnly();
    }
}
