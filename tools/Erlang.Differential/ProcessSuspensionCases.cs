namespace Erlang.Differential;

public static class ProcessSuspensionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function switch
    {
        ProcessSuspensionNames.Suspend when arity == 1 => "erlang-suspension-counted",
        ProcessSuspensionNames.Suspend when arity == 2 => "erlang-suspension-unless",
        ProcessSuspensionNames.Resume when arity == 1 => "erlang-suspension-counted",
        _ => null
    };

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-suspension-" + name,
            "-module(suspension_contract). -export([run/0,worker/1]). run()->" + body + ",done. worker(Parent)->receive ping->Parent!pong,worker(Parent);stop->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        const string start = "Self=self(),P=spawn(fun()->worker(Self) end),";
        const string end = ",P!ping,receive pong->ok after 1000->error(no_resume) end,P!stop";
        Add(
            "counted",
            start + "true=erlang:suspend_process(P),true=erlang:suspend_process(P),P!ping,true=erlang:resume_process(P),receive pong->error(early) after 0->ok end,true=erlang:resume_process(P),receive pong->ok after 1000->error(no_resume) end,P!stop"
        );
        Add(
            "unless",
            start + "true=erlang:suspend_process(P,[unless_suspending]),false=erlang:suspend_process(P,[unless_suspending]),true=erlang:resume_process(P)" + end
        );
        Add(
            "async-tag",
            start + "true=erlang:suspend_process(P,[{asynchronous,{tag,42}}]),receive {{tag,42},suspended}->ok after 1000->error(no_ack) end,true=erlang:resume_process(P)" + end
        );
        Add(
            "async-last-tag",
            start + "true=erlang:suspend_process(P,[{asynchronous,first},{asynchronous,last},asynchronous]),receive {last,suspended}->ok after 1000->error(no_ack) end,receive {first,_}->error(extra_ack) after 0->ok end,true=erlang:resume_process(P)" + end
        );
        Add(
            "async-unless",
            start + "true=erlang:suspend_process(P),false=erlang:suspend_process(P,[unless_suspending,{asynchronous,tag}]),receive {tag,suspended}->ok after 1000->error(no_ack) end,true=erlang:resume_process(P)" + end
        );
        Add(
            "async-sync",
            start + "true=erlang:suspend_process(P,[asynchronous]),true=erlang:suspend_process(P),true=erlang:resume_process(P),true=erlang:resume_process(P)" + end
        );
        Add(
            "owner-exit",
            "Self=self(),P=spawn(fun()->worker(Self) end),{Owner,Ref}=spawn_monitor(fun()->true=erlang:suspend_process(P),Self!paused,receive finish->ok end end),receive paused->ok end,Owner!finish,receive {'DOWN',Ref,process,Owner,normal}->ok end" + end
        );
        Add(
            "multiple-owners",
            start + "true=erlang:suspend_process(P),{Owner,Ref}=spawn_monitor(fun()->true=erlang:suspend_process(P),Self!paused,receive finish->ok end end),receive paused->ok end,true=erlang:resume_process(P),P!ping,receive pong->error(early) after 0->ok end,Owner!finish,receive {'DOWN',Ref,process,Owner,normal}->ok end,receive pong->ok after 1000->error(no_resume) end,P!stop"
        );
        Add(
            "kill",
            start + "Ref=monitor(process,P),true=erlang:suspend_process(P),exit(P,kill),receive {'DOWN',Ref,process,P,killed}->ok after 1000->error(no_kill) end"
        );
        Add(
            "pid-timers",
            start + "true=erlang:suspend_process(P),T=erlang:send_after(100,P,ping),false=erlang:read_timer(T),receive after 150->ok end,false=erlang:read_timer(T),false=erlang:cancel_timer(T),true=erlang:resume_process(P),false=erlang:read_timer(T),false=erlang:cancel_timer(T),receive pong->ok after 1000->error(no_timer) end,P!stop"
        );
        Add(
            "name-timers",
            start + "true=register(suspension_timer_target,P),true=erlang:suspend_process(P),T=erlang:send_after(0,suspension_timer_target,ping),receive after 50->ok end,false=erlang:read_timer(T),true=erlang:resume_process(P),receive pong->ok after 1000->error(no_timer) end,P!stop"
        );
        Add("resume-unowned", start + "try erlang:resume_process(P) of _->error(accepted) catch error:badarg->ok end,P!stop");
        foreach (string value in new[] { "self()", "atom", "42", "[]", "make_ref()" })
        {
            Add("bad-suspend-" + cases.Count, "try erlang:suspend_process(" + value + ") of _->error(accepted) catch error:badarg->ok end");
            Add("bad-resume-" + cases.Count, "try erlang:resume_process(" + value + ") of _->error(accepted) catch error:badarg->ok end");
        }
        foreach (string options in new[] { "wrong", "[wrong]", "[asynchronous|tail]", "[{asynchronous}]", "[{asynchronous,tag,extra}]" })
            Add(
                "bad-options-" + cases.Count,
                start + "try erlang:suspend_process(P," + options + ") of _->error(accepted) catch error:badarg:S->[{erlang,suspend_process,[P," + options + "],L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badopt}}=lists:keyfind(error_info,1,L) end,P!stop"
            );
        Add(
            "self-before-options",
            "try erlang:suspend_process(self(),[wrong]) of _->error(accepted) catch error:badarg:S->[{erlang,suspend_process,_,L}|_]=S,{error_info,Info}=lists:keyfind(error_info,1,L),false=maps:is_key(cause,Info) end"
        );
        const string dead = "{P,Ref}=spawn_monitor(fun()->ok end),receive {'DOWN',Ref,process,P,normal}->ok end,";
        Add("dead-sync", dead + "try erlang:suspend_process(P) of _->error(accepted) catch error:badarg->ok end");
        Add(
            "dead-async",
            dead + "true=erlang:suspend_process(P,[asynchronous]),try erlang:resume_process(P) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "dead-async-tag",
            dead + "true=erlang:suspend_process(P,[{asynchronous,tag}]),receive {tag,badarg}->ok after 1000->error(no_ack) end"
        );

        Add(
            "existing-pid-timer",
            start + "T=erlang:send_after(200,P,ping),true=erlang:suspend_process(P),false=erlang:read_timer(T),false=erlang:cancel_timer(T),true=erlang:resume_process(P),false=erlang:read_timer(T),receive pong->ok after 1000->error(no_timer) end,P!stop"
        );
        Add(
            "wrapped-pid-timer",
            "Self=self(),P=spawn(fun()->receive {timeout,T,ping}->Self!{timer,T} end end),true=erlang:suspend_process(P),T=erlang:start_timer(50,P,ping),false=erlang:read_timer(T),false=erlang:cancel_timer(T),true=erlang:resume_process(P),receive {timer,T}->ok after 1000->error(no_timer) end,false=erlang:read_timer(T)"
        );

        return cases;
    }
}
