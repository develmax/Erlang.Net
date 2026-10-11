namespace Erlang.Differential;

public static class RuntimeTimerCases
{
    private static CompiledModuleCase Case(string name, string body) => new(
        "runtime-timer-" + name,
        "-module(oracle_runtime_timer). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case(
            "send-after",
            "T=erlang:send_after(0,self(),delivered),receive delivered->ok after 2000->missing end,false=erlang:read_timer(T),false=erlang:cancel_timer(T)"
        ),
        Case(
            "start-timer",
            "T=erlang:start_timer(0,self(),{payload,42}),receive {timeout,T,{payload,42}}->ok after 2000->missing end,false=erlang:read_timer(T)"
        ),
        Case(
            "read-cancel",
            "T=erlang:send_after(60000,self(),cancelled),N=erlang:read_timer(T),true=is_integer(N),true=N>=0,true=N=<60000,C=erlang:cancel_timer(T),true=is_integer(C),true=C>=0,true=C=<N,false=erlang:cancel_timer(T),false=erlang:read_timer(T),receive cancelled->unexpected after 0->ok end"
        ),
        Case(
            "read-does-not-cancel",
            "T=erlang:start_timer(100,self(),delivered),N=erlang:read_timer(T),true=is_integer(N),receive {timeout,T,delivered}->ok after 2000->missing end"
        ),
        Case("unknown-reference", "T=make_ref(),false=erlang:read_timer(T),false=erlang:cancel_timer(T)"),
        Case(
            "dead-recipient",
            "{P,M}=spawn_monitor(fun()->ok end),receive {'DOWN',M,process,P,normal}->ok end,T=erlang:send_after(60000,P,unused),false=erlang:read_timer(T),U=erlang:start_timer(60000,P,unused),false=erlang:cancel_timer(U)"
        ),
        Case(
            "recipient-death-cancels",
            "{P,M}=spawn_monitor(fun()->receive finish->ok end end),T=erlang:send_after(60000,P,unused),P!finish,receive {'DOWN',M,process,P,normal}->ok end,false=erlang:read_timer(T),false=erlang:cancel_timer(T)"
        ),
        Case(
            "creator-death-survives",
            "Parent=self(),{P,M}=spawn_monitor(fun()->T=erlang:send_after(100,Parent,delivered),Parent!{timer,T} end),receive {timer,T}->ok end,receive {'DOWN',M,process,P,normal}->ok end,receive delivered->ok after 2000->missing end,false=erlang:read_timer(T)"
        ),
        Case(
            "named-delivery",
            "T=erlang:send_after(100,oracle_timer_target,delivered),true=register(oracle_timer_target,self()),receive delivered->ok after 2000->missing end,true=unregister(oracle_timer_target),false=erlang:read_timer(T)"
        ),
        Case(
            "name-resolved-at-delivery",
            "P=spawn(fun()->receive finish->ok end end),true=register(oracle_timer_target,P),T=erlang:start_timer(100,oracle_timer_target,delivered),true=unregister(oracle_timer_target),true=register(oracle_timer_target,self()),receive {timeout,T,delivered}->ok after 2000->missing end,true=unregister(oracle_timer_target),P!finish"
        ),
        Case(
            "absent-name",
            "T=erlang:send_after(60000,unregistered_timer_target,unused),N=erlang:cancel_timer(T),true=is_integer(N)"
        ),
        Case(
            "cancel-from-other-process",
            "Parent=self(),{P,M}=spawn_monitor(fun()->T=erlang:start_timer(60000,Parent,unused),Parent!{timer,T},receive finish->ok end end),receive {timer,T}->ok end,N=erlang:cancel_timer(T),true=is_integer(N),P!finish,receive {'DOWN',M,process,P,normal}->ok end,false=erlang:read_timer(T)"
        ),
        Case(
            "cancel-does-not-flush-delivered",
            "T=erlang:start_timer(0,self(),delivered),receive {timeout,T,delivered}->self()!{saved,T} after 2000->missing end,false=erlang:cancel_timer(T),receive {saved,T}->ok end"
        ),
        Case(
            "independent-references",
            "T=erlang:send_after(60000,self(),first),U=erlang:send_after(60000,self(),second),true=T=/=U,true=is_integer(erlang:cancel_timer(T)),true=is_integer(erlang:read_timer(U)),true=is_integer(erlang:cancel_timer(U))"
        ),
        Case(
            "bad-time",
            "try erlang:send_after(-1,self(),unused) of _->unexpected catch error:badarg->ok end,try erlang:start_timer(1.0,self(),unused) of _->unexpected catch error:badarg->ok end"
        ),
        Case(
            "bad-recipient",
            "try erlang:send_after(0,{wrong},unused) of _->unexpected catch error:badarg->ok end,try erlang:start_timer(0,42,unused) of _->unexpected catch error:badarg->ok end"
        ),
        Case(
            "bad-reference",
            "try erlang:read_timer(wrong) of _->unexpected catch error:badarg->ok end,try erlang:cancel_timer(42) of _->unexpected catch error:badarg->ok end"
        ),
        Case(
            "large-relative-duration",
            "T=erlang:send_after(4294967296,self(),unused),N=erlang:cancel_timer(T),true=is_integer(N),true=N>2147483647,true=N=<4294967296"
        ),
        Case("empty-message", "T=erlang:start_timer(0,self(),[]),receive {timeout,T,[]}->ok after 2000->missing end"),
        Case(
            "pid-registration-change",
            "true=register(oracle_timer_target,self()),T=erlang:send_after(100,self(),delivered),true=unregister(oracle_timer_target),receive delivered->ok after 2000->missing end,false=erlang:read_timer(T)"
        )
    ];
}
