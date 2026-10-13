namespace Erlang.Differential;

public static class HibernateCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ProcessHibernateNames.Function ? arity switch
    {
        0 => "erlang-hibernate-zero-queued",
        3 => "erlang-hibernate-three-transfer",
        _ => null
    } : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-hibernate-" + name,
            "-module(hibernate_contract). -export([run/0,resume/1,again/1,finish/1]). run()->" + body + ",done. resume(S)->receive wake->S!{resumed,self(),get(mark)} end. again(S)->receive first->S!{again,self()} end,erlang:hibernate(hibernate_contract,finish,[S]),S!unreachable_again. finish(S)->receive second->S!{finished,self()} end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string down = "receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_down) end";
        Add(
            "zero-queued",
            "put(mark,kept),self()!wake,ok=erlang:hibernate(),kept=get(mark),receive wake->ok after 0->error(consumed) end"
        );
        Add(
            "zero-idle",
            "S=self(),{P,R}=spawn_monitor(fun()->put(mark,kept),S!{ready,self()},ok=erlang:hibernate(),kept=get(mark),receive wake->S!{awake,self()} end end),receive {ready,P}->ok after 5000->error(no_ready) end,receive {awake,P}->error(early) after 20->ok end,P!wake,receive {awake,P}->ok after 5000->error(no_awake) end," + down
        );
        Add(
            "zero-after-retained",
            "S=self(),{P,R}=spawn_monitor(fun()->try S!{ready,self()},ok=erlang:hibernate(),receive wake->S!body end after S!cleanup end end),receive {ready,P}->ok after 5000->error(no_ready) end,P!wake,receive body->ok after 5000->error(no_body) end,receive cleanup->ok after 5000->error(no_cleanup) end," + down
        );
        Add(
            "three-transfer",
            "S=self(),{P,R}=spawn_monitor(fun()->put(mark,kept),S!{ready,self()},erlang:hibernate(hibernate_contract,resume,[S]),S!unreachable end),receive {ready,P}->ok after 5000->error(no_ready) end,P!wake,receive {resumed,P,kept}->ok after 5000->error(no_resume) end," + down + ",receive unreachable->error(returned) after 0->ok end"
        );
        Add(
            "three-queued",
            "S=self(),{P,R}=spawn_monitor(fun()->put(mark,kept),self()!wake,erlang:hibernate(hibernate_contract,resume,[S]) end),receive {resumed,P,kept}->ok after 5000->error(no_resume) end," + down
        );
        Add(
            "three-clears-handlers-after",
            "S=self(),{P,R}=spawn_monitor(fun()->put(mark,kept),try S!{ready,self()},erlang:hibernate(hibernate_contract,resume,[S]) of _->S!returned catch _:_ ->S!caught after S!cleanup end,S!unreachable end),receive {ready,P}->ok after 5000->error(no_ready) end,P!wake,receive {resumed,P,kept}->ok after 5000->error(no_resume) end," + down + ",receive cleanup->error(after_ran);caught->error(caught);returned->error(returned);unreachable->error(returned) after 0->ok end"
        );
        Add(
            "three-repeated",
            "S=self(),{P,R}=spawn_monitor(fun()->erlang:hibernate(hibernate_contract,again,[S]) end),P!first,receive {again,P}->ok after 5000->error(no_again) end,P!second,receive {finished,P}->ok after 5000->error(no_finish) end," + down + ",receive unreachable_again->error(returned) after 0->ok end"
        );
        foreach (string call in new[] { "erlang:hibernate(42,resume,[])", "erlang:hibernate(hibernate_contract,42,[])", "erlang:hibernate(hibernate_contract,resume,wrong)", "erlang:hibernate(hibernate_contract,resume,[a|tail])" })
            Add(
                "invalid-" + cases.Count,
                "put(mark,none),try try " + call + " after put(mark,cleanup) end of _->error(accepted) catch error:badarg->cleanup=get(mark) end"
            );
        foreach (string count in new[] { "257", "258" })
            Add(
                "arity-limit-" + count,
                "try erlang:hibernate(hibernate_contract,resume,lists:duplicate(" + count + ",x)) of _->error(accepted) catch error:system_limit->ok end"
            );
        Add(
            "long-improper-arity-precedence",
            "A=lists:duplicate(257,x)++tail,try erlang:hibernate(hibernate_contract,resume,A) of _->error(accepted) catch error:system_limit->ok end"
        );
        Add(
            "missing-export-after-wake",
            "{P,R}=spawn_monitor(fun()->self()!wake,try erlang:hibernate(hibernate_contract,missing,[]) catch _:_ ->error(caught_old) end end),receive {'DOWN',R,process,P,{undef,_}}->ok after 5000->error(no_undef) end"
        );
        Add(
            "arity-256-after-wake",
            "{P,R}=spawn_monitor(fun()->self()!wake,try erlang:hibernate(hibernate_contract,resume,lists:duplicate(256,x)) catch _:_ ->error(caught_old) end end),receive {'DOWN',R,process,P,{system_limit,_}}->ok after 5000->error(no_system_limit) end"
        );
        Add(
            "pending-message-order",
            "self()!first,self()!second,ok=erlang:hibernate(),receive first->ok after 0->error(first_missing) end,ok=erlang:hibernate(),receive second->ok after 0->error(second_missing) end"
        );
        Add(
            "trap-exit-message-wakes",
            "S=self(),{P,R}=spawn_monitor(fun()->process_flag(trap_exit,true),S!{ready,self()},ok=erlang:hibernate(),receive {'EXIT',S,wake}->S!{awake,self()} end end),receive {ready,P}->ok after 5000->error(no_ready) end,exit(P,wake),receive {awake,P}->ok after 5000->error(no_awake) end," + down
        );
        Add(
            "killed-idle",
            "S=self(),{P,R}=spawn_monitor(fun()->S!{ready,self()},erlang:hibernate(hibernate_contract,resume,[S]) end),receive {ready,P}->ok after 5000->error(no_ready) end,exit(P,kill),receive {'DOWN',R,process,P,killed}->ok after 5000->error(no_kill) end"
        );

        return cases;
    }
}
