namespace Erlang.Differential;

public static class SystemMonitorCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == SystemMonitorNames.Function ? arity switch
    {
        0 => "erlang-system-monitor-query",
        1 => "erlang-system-monitor-clear-one",
        2 => "erlang-system-monitor-configure",
        _ => null
    } : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-system-monitor-" + name,
            "-module(system_monitor_contract). -export([run/0,worker/1,drain/1]). run()->" + body + ",done. worker(S)->receive {drain,N}->drain(N),S!{drained,self()},worker(S);stop->ok end. drain(0)->ok;drain(N)->receive item->drain(N-1);[]->drain(N-1) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("query", "undefined=erlang:system_monitor()");
        Add("clear-one", "undefined=erlang:system_monitor(undefined),undefined=erlang:system_monitor()");
        Add(
            "configure",
            "S=self(),O=[{long_message_queue,{1,3}}],undefined=erlang:system_monitor(S,O),{S,O}=erlang:system_monitor(),{S,O}=erlang:system_monitor(undefined)"
        );
        Add(
            "tuple-one",
            "S=self(),O=[{long_message_queue,{0,2}}],undefined=erlang:system_monitor({S,O}),{S,O}=erlang:system_monitor(),{S,O}=erlang:system_monitor(undefined)"
        );
        Add(
            "replace-global",
            "S=self(),O=[{long_message_queue,{0,2}}],P=spawn(fun()->receive stop->ok end end),undefined=erlang:system_monitor(S,O),{S,O}=erlang:system_monitor(P,[{long_message_queue,{2,4}}]),{P,[{long_message_queue,{2,4}}]}=erlang:system_monitor(),erlang:system_monitor(undefined),P!stop"
        );
        Add(
            "empty-options-clears",
            "S=self(),O=[{long_message_queue,{0,2}}],undefined=erlang:system_monitor(S,O),{S,O}=erlang:system_monitor(wrong,[]),undefined=erlang:system_monitor()"
        );
        Add(
            "undefined-discards-options",
            "S=self(),O=[{long_message_queue,{0,2}}],undefined=erlang:system_monitor(S,O),{S,O}=erlang:system_monitor(undefined,[wrong|tail]),undefined=erlang:system_monitor()"
        );
        Add(
            "queue-disabled-retains-owner",
            "S=self(),undefined=erlang:system_monitor(S,[{long_message_queue,false}]),{S,[]}=erlang:system_monitor(),{S,[]}=erlang:system_monitor(undefined)"
        );
        Add(
            "physical-disabled",
            "S=self(),undefined=erlang:system_monitor(S,[{long_gc,false},{long_schedule,false},{large_heap,false},{busy_port,false},{busy_dist_port,false}]),{S,[]}=erlang:system_monitor(),erlang:system_monitor(undefined)"
        );
        Add(
            "duplicate-last-wins",
            "S=self(),undefined=erlang:system_monitor(S,[{long_message_queue,{0,2}},{long_message_queue,{1,3}}]),{S,[{long_message_queue,{1,3}}]}=erlang:system_monitor(),erlang:system_monitor(undefined)"
        );
        Add(
            "physical-overwritten-disabled",
            "S=self(),undefined=erlang:system_monitor(S,[{long_gc,0},{long_gc,false},busy_port,{busy_port,false}]),{S,[]}=erlang:system_monitor(),erlang:system_monitor(undefined)"
        );
        Add(
            "max-thresholds",
            "S=self(),O=[{long_message_queue,{9223372036854775807,18446744073709551615}}],undefined=erlang:system_monitor(S,O),{S,O}=erlang:system_monitor(),erlang:system_monitor(undefined)"
        );
        Add(
            "owner-death",
            "{P,R}=spawn_monitor(fun()->receive stop->ok end end),undefined=erlang:system_monitor(P,[{long_message_queue,{0,2}}]),P!stop,receive {'DOWN',R,process,P,normal}->undefined=erlang:system_monitor() after 5000->error(no_down) end"
        );
        Add(
            "dead-owner",
            "{P,R}=spawn_monitor(fun()->ok end),receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end,{'EXIT',{badarg,_}}=catch erlang:system_monitor(P,[{long_message_queue,{0,2}}]),undefined=erlang:system_monitor()"
        );
        Add(
            "foreign-owner",
            "P=binary_to_term(<<131,103,100,0,10,\"other@host\",0,0,0,1,0,0,0,0,0>>),{'EXIT',{badarg,_}}=catch erlang:system_monitor(P,[{long_message_queue,{0,2}}]),undefined=erlang:system_monitor()"
        );
        foreach (string option in new[] { "wrong", "long_message_queue", "{long_message_queue,{0,0}}", "{long_message_queue,{1,1}}", "{long_message_queue,{-1,2}}", "{long_message_queue,{0,false}}", "{long_message_queue,{9223372036854775808,18446744073709551615}}", "{long_message_queue,{0,18446744073709551616}}", "{busy_port,wrong}", "{long_gc,true}" })
            Add(
                "invalid-option-" + cases.Count,
                "{'EXIT',{badarg,_}}=catch erlang:system_monitor(self(),[" + option + "]),undefined=erlang:system_monitor()"
            );
        Add(
            "improper-options",
            "{'EXIT',{badarg,_}}=catch erlang:system_monitor(self(),[{long_message_queue,{0,2}}|tail]),undefined=erlang:system_monitor()"
        );
        Add("invalid-one", "{'EXIT',{badarg,_}}=catch erlang:system_monitor(self()),undefined=erlang:system_monitor()");
        Add(
            "nonlist-before-clear",
            "{'EXIT',{badarg,_}}=catch erlang:system_monitor(undefined,wrong),undefined=erlang:system_monitor()"
        );
        Add(
            "invalid-preserves-settings",
            "S=self(),O=[{long_message_queue,{0,2}}],undefined=erlang:system_monitor(S,O),{'EXIT',{badarg,_}}=catch erlang:system_monitor(S,[{long_message_queue,{1,4}},wrong]),{S,O}=erlang:system_monitor(),erlang:system_monitor(undefined)"
        );
        Add(
            "queue-high-low",
            "S=self(),P=spawn(system_monitor_contract,worker,[S]),undefined=erlang:system_monitor(S,[{long_message_queue,{0,3}}]),P!item,P!item,P!item,receive {monitor,P,long_message_queue,true}->ok after 5000->error(no_high) end,P!{drain,3},receive {monitor,P,long_message_queue,false}->ok after 5000->error(no_low) end,receive {drained,P}->ok after 5000->error(no_drain) end,receive {monitor,P,long_message_queue,_}->error(duplicate) after 0->ok end,erlang:system_monitor(undefined),P!stop"
        );
        Add(
            "queue-hysteresis",
            "S=self(),P=spawn(system_monitor_contract,worker,[S]),undefined=erlang:system_monitor(S,[{long_message_queue,{1,3}}]),P!item,P!item,P!item,receive {monitor,P,long_message_queue,true}->ok after 5000->error(no_high) end,P!{drain,1},receive {drained,P}->ok after 5000->error(no_drain) end,receive {monitor,P,long_message_queue,false}->error(early_low) after 0->ok end,P!item,receive {monitor,P,long_message_queue,true}->error(duplicate_high) after 0->ok end,P!{drain,3},receive {monitor,P,long_message_queue,false}->ok after 5000->error(no_low) end,receive {drained,P}->ok after 5000->error(no_drain) end,erlang:system_monitor(undefined),P!stop"
        );
        Add(
            "real-nil-counted",
            "S=self(),P=spawn(system_monitor_contract,worker,[S]),undefined=erlang:system_monitor(S,[{long_message_queue,{0,2}}]),P![],P![],receive {monitor,P,long_message_queue,true}->ok after 5000->error(no_high) end,P!{drain,2},receive {monitor,P,long_message_queue,false}->ok after 5000->error(no_low) end,receive {drained,P}->ok after 5000->error(no_drain) end,erlang:system_monitor(undefined),P!stop"
        );
        Add(
            "self-notification-suppressed",
            "S=self(),undefined=erlang:system_monitor(S,[{long_message_queue,{0,2}}]),S!first,S!second,receive first->ok end,receive second->ok end,receive {monitor,S,long_message_queue,_}->error(self_notification) after 0->ok end,erlang:system_monitor(undefined)"
        );
        Add(
            "rearm-after-low",
            "S=self(),P=spawn(system_monitor_contract,worker,[S]),undefined=erlang:system_monitor(S,[{long_message_queue,{0,2}}]),P!item,P!item,receive {monitor,P,long_message_queue,true}->ok after 5000->error(no_high) end,P!{drain,2},receive {monitor,P,long_message_queue,false}->ok after 5000->error(no_low) end,receive {drained,P}->ok after 5000->error(no_drain) end,P!item,P!item,receive {monitor,P,long_message_queue,true}->ok after 5000->error(no_second_high) end,P!{drain,2},receive {monitor,P,long_message_queue,false}->ok after 5000->error(no_second_low) end,receive {drained,P}->ok after 5000->error(no_drain) end,erlang:system_monitor(undefined),P!stop"
        );

        Add(
            "preserves-sequence-token",
            "S=self(),P=spawn(system_monitor_contract,worker,[S]),undefined=erlang:system_monitor(S,[{long_message_queue,{0,2}}]),P!item,P!item,0=erlang:seq_trace(label,label),true=erlang:seq_trace_print(seed),receive {monitor,P,long_message_queue,true}->{label,label}=erlang:seq_trace_info(label),{serial,{0,1}}=erlang:seq_trace_info(serial) after 5000->error(no_high) end,true=erlang:seq_trace_print(after_monitor),{serial,{0,2}}=erlang:seq_trace_info(serial),erlang:seq_trace(sequential_trace_token,[]),erlang:system_monitor(undefined),P!stop"
        );

        return cases.AsReadOnly();
    }
}
