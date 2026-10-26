namespace Erlang.Differential;

public static class SequenceExitCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ErlangModuleNames.Exit && arity is 2 or 3
        ? "erlang-seq-exit-explicit-self-" + arity
        : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();
        const string trap = "process_flag(trap_exit,true),S=self()";
        const string token = "erlang:seq_trace(label,child),erlang:seq_trace(serial,{3,10})";
        const string receive = "receive {'EXIT',P,R}->";
        const string reset = "erlang:seq_trace(sequential_trace_token,[])";

        void Add(string name, string body) => cases.Add(new(
            "erlang-seq-exit-" + name,
            "-module(seq_exit_contract). -export([run/0,collector/2]). run()->" + body + ",process_flag(trap_exit,false),done. collector(S,L)->receive {seq_trace,_,_}=E->collector(S,[E|L]);{seq_trace,_,_,_}=E->collector(S,[E|L]);{take,S}->S!lists:reverse(L) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        foreach ((string name, string reason) in new[] { ("normal", "normal"), ("atom", "reason"), ("tuple", "{reason,[1,2]}"), ("binary", "<<1,2,3>>") })
            Add(
                "linked-" + name,
                trap + ",R = " + reason + ",P=spawn_link(fun()->receive go->" + token + ",exit(R) end end),_=erlang:send_after(0,P,go)," + receive + "{label,child}=erlang:seq_trace_info(label),{serial,{3,11}}=erlang:seq_trace_info(serial),{0,child,11,P,3}=" + reset + " after 5000->error(no_exit) end"
            );
        Add(
            "linked-clock-merges-max",
            trap + ",R=normal,P=spawn_link(fun()->receive go->" + token + ",ok end end),erlang:seq_trace(label,parent),erlang:seq_trace(serial,{4,100}),_=erlang:send_after(0,P,go)," + receive + "{serial,{3,11}}=erlang:seq_trace_info(serial),true=erlang:seq_trace_print(after_exit),{serial,{11,101}}=erlang:seq_trace_info(serial) after 5000->error(no_exit) end"
        );
        Add(
            "linked-clock-merges-child",
            trap + ",R=normal,P=spawn_link(fun()->receive go->" + token + ",ok end end),erlang:seq_trace(label,parent),erlang:seq_trace(serial,{4,5}),_=erlang:send_after(0,P,go)," + receive + "true=erlang:seq_trace_print(after_exit),{serial,{11,12}}=erlang:seq_trace_info(serial) after 5000->error(no_exit) end"
        );
        Add(
            "linked-wrap",
            trap + ",R=normal,P=spawn_link(fun()->receive go->erlang:seq_trace(label,child),erlang:seq_trace(serial,{3,-1}),ok end end),_=erlang:send_after(0,P,go)," + receive + "{serial,{3,0}}=erlang:seq_trace_info(serial) after 5000->error(no_exit) end"
        );
        Add(
            "linked-flags",
            trap + ",R=normal,P=spawn_link(fun()->receive go->" + token + ",erlang:seq_trace(send,true),erlang:seq_trace('receive',true),erlang:seq_trace(print,true),ok end end),_=erlang:send_after(0,P,go)," + receive + "{send,true}=erlang:seq_trace_info(send),{'receive',true}=erlang:seq_trace_info('receive'),{print,true}=erlang:seq_trace_info(print),{7,child,11,P,3}=" + reset + " after 5000->error(no_exit) end"
        );
        Add(
            "linked-untraced-clears",
            trap + ",P=spawn_link(fun()->receive go->ok end end),erlang:seq_trace(label,parent),true=erlang:seq_trace_print(seed),_=erlang:send_after(0,P,go),receive {'EXIT',P,normal}->[]=erlang:seq_trace_info(label),[]=erlang:seq_trace_info(serial) after 5000->error(no_exit) end,erlang:seq_trace(label,next),true=erlang:seq_trace_print(next),{serial,{0,2}}=erlang:seq_trace_info(serial)"
        );
        foreach (int arity in new[] { 2, 3 })
            Add(
                "explicit-self-" + arity,
                trap + ",erlang:seq_trace(label,parent),erlang:seq_trace(serial,{3,10}),true=erlang:exit(S,reason" + (arity == 3 ? ",[]" : "") + "),{serial,{3,10}}=erlang:seq_trace_info(serial),receive {'EXIT',S,reason}->[]=erlang:seq_trace_info(label) after 5000->error(no_exit) end,erlang:seq_trace(label,next),true=erlang:seq_trace_print(next),{serial,{3,11}}=erlang:seq_trace_info(serial)"
            );
        foreach (int arity in new[] { 2, 3 })
            Add(
                "exit-signal-self-" + arity,
                trap + ",erlang:seq_trace(label,parent),erlang:seq_trace(serial,{3,10}),true=erlang:exit_signal(S,reason" + (arity == 3 ? ",[]" : "") + "),{serial,{3,10}}=erlang:seq_trace_info(serial),receive {'EXIT',S,reason}->[]=erlang:seq_trace_info(label) after 5000->error(no_exit) end"
            );
        Add(
            "explicit-normal-self-traps",
            trap + ",erlang:seq_trace(label,parent),true=exit(S,normal),receive {'EXIT',S,normal}->[]=erlang:seq_trace_info(label) after 5000->error(no_exit) end"
        );
        Add(
            "dead-recipient-no-advance",
            trap + ",{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,_}->ok end,erlang:seq_trace(label,parent),erlang:seq_trace(serial,{3,10}),true=exit(P,reason),{serial,{3,10}}=erlang:seq_trace_info(serial),{0,parent,10,S,3}=" + reset
        );
        Add(
            "link-noproc-clears",
            trap + ",{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,_}->ok end,erlang:seq_trace(label,parent),true=link(P),receive {'EXIT',P,noproc}->[]=erlang:seq_trace_info(label) after 5000->error(no_exit) end"
        );
        Add(
            "explicit-kill-monitor-preserves",
            trap + ",{P,M}=spawn_monitor(fun()->process_flag(trap_exit,true),receive stop->ok end end),erlang:seq_trace(label,parent),erlang:seq_trace(serial,{3,10}),true=exit(P,kill),receive {'DOWN',M,process,P,killed}->{label,parent}=erlang:seq_trace_info(label),{serial,{3,10}}=erlang:seq_trace_info(serial) after 5000->error(no_down) end"
        );
        Add(
            "linked-real-send-receive-events",
            trap + ",T=spawn(fun()->collector(S,[]) end),false=erlang:system_flag(sequential_tracer,T),R=normal,P=spawn_link(fun()->receive go->" + token + ",erlang:seq_trace(send,true),erlang:seq_trace('receive',true),ok end end),_=erlang:send_after(0,P,go)," + receive + "{label,child}=erlang:seq_trace_info(label) after 5000->error(no_exit) end," + reset + ",T!{take,S},receive [{seq_trace,child,{send,{3,11},P,S,{'EXIT',P,normal}}},{seq_trace,child,{'receive',{3,11},P,S,{'EXIT',P,normal}}}]->ok after 5000->error(no_trace) end,erlang:system_flag(sequential_tracer,false)"
        );
        Add(
            "explicit-no-send-receive-events",
            trap + ",T=spawn(fun()->collector(S,[]) end),false=erlang:system_flag(sequential_tracer,T),erlang:seq_trace(label,parent),erlang:seq_trace(send,true),erlang:seq_trace('receive',true),true=exit(S,reason),receive {'EXIT',S,reason}->[]=erlang:seq_trace_info(label) end,T!{take,S},receive []->ok after 5000->error(unexpected_trace) end,erlang:system_flag(sequential_tracer,false)"
        );

        Add(
            "unmatched-exit-does-not-install",
            trap + ",{P,M}=spawn_opt(fun()->receive go->" + token + ",ok end end,[link,monitor]),erlang:seq_trace(label,parent),erlang:seq_trace(serial,{4,100}),_=erlang:send_after(0,P,go),receive {'DOWN',M,process,P,normal}->{label,parent}=erlang:seq_trace_info(label),{serial,{4,100}}=erlang:seq_trace_info(serial) after 5000->error(no_down) end,receive {'EXIT',P,normal}->{label,child}=erlang:seq_trace_info(label),{serial,{3,11}}=erlang:seq_trace_info(serial) after 5000->error(no_exit) end"
        );
        Add(
            "ignored-normal-does-not-install",
            trap + ",P=spawn(fun()->" + token + ",receive go->{label,child}=erlang:seq_trace_info(label),{serial,{3,10}}=erlang:seq_trace_info(serial),S!checked end end),erlang:seq_trace(label,parent),true=exit(P,normal),_=erlang:send_after(0,P,go),receive checked->{label,child}=erlang:seq_trace_info(label),{serial,{3,11}}=erlang:seq_trace_info(serial) after 5000->error(no_check) end"
        );
        Add(
            "killed-child-forwards-own-token",
            trap + ",P=spawn_link(fun()->" + token + ",S!ready,receive stop->ok end end),receive ready->{label,child}=erlang:seq_trace_info(label),{serial,{3,11}}=erlang:seq_trace_info(serial) end,child=erlang:seq_trace(label,parent),true=exit(P,kill),receive {'EXIT',P,killed}->{label,child}=erlang:seq_trace_info(label),{serial,{3,12}}=erlang:seq_trace_info(serial) after 5000->error(no_exit) end"
        );

        Add(
            "linked-multiple-recipient-serials",
            trap + ",A=spawn(fun()->process_flag(trap_exit,true),receive {'EXIT',P,normal}->{serial,{3,N}}=erlang:seq_trace_info(serial),S!{a,P,N} end end),B=spawn(fun()->process_flag(trap_exit,true),receive {'EXIT',P,normal}->{serial,{3,N}}=erlang:seq_trace_info(serial),S!{b,P,N} end end),P=spawn(fun()->link(A),link(B)," + token + ",ok end),N1=receive {a,P,NA}->NA after 5000->error(no_a) end,N2=receive {b,P,NB}->NB after 5000->error(no_b) end,[11,12]=lists:sort([N1,N2])"
        );
        Add(
            "dying-tracer-self-counter-suppression",
            trap + ",P=spawn_link(fun()->receive go->false=erlang:system_flag(sequential_tracer,self())," + token + ",erlang:seq_trace(send,true),ok end end),_=erlang:send_after(0,P,go),receive {'EXIT',P,normal}->{serial,{3,11}}=erlang:seq_trace_info(serial),{1,child,11,P,3}=" + reset + " after 5000->error(no_exit) end"
        );

        return cases.AsReadOnly();
    }
}
