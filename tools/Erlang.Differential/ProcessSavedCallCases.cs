namespace Erlang.Differential;

public static class ProcessSavedCallCases
{
    public static string? FixtureName(string function, int arity) => function == ErlangModuleNames.ProcessFlag && arity == 3 ? "erlang-saved-calls-self-three" : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-saved-calls-" + name,
            "-module(saved_call_contract). -export([run/0,worker/1]). run()->" + body + ",done. " +
            "worker(P)->P!{ready,self()},receive run->V=erlang:monotonic_time(),P!{finished,self(),V},receive stop->ok end;stop->ok end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("default-disabled", "{last_calls,false}=process_info(self(),last_calls)");
        Add(
            "self-two",
            "0=process_flag(save_calls,8),{last_calls,L}=process_info(self(),last_calls),true=is_list(L),true=length(L)=<8,8=process_flag(save_calls,0),{last_calls,false}=process_info(self(),last_calls)"
        );
        Add(
            "self-three",
            "0=erlang:process_flag(self(),save_calls,8),8=erlang:process_flag(self(),save_calls,3),3=erlang:process_flag(self(),save_calls,0),{last_calls,false}=process_info(self(),last_calls)"
        );
        Add("same-capacity-replaces", "0=process_flag(save_calls,4),4=process_flag(save_calls,4),4=process_flag(save_calls,0)");
        Add("maximum-capacity", "0=process_flag(save_calls,10000),10000=erlang:process_flag(self(),save_calls,0)");
        Add(
            "zero-disabled",
            "0=process_flag(save_calls,0),0=erlang:process_flag(self(),save_calls,0),{last_calls,false}=process_info(self(),last_calls)"
        );
        foreach (string value in new[] { "-1", "10001", "1152921504606846976", "1.0", "atom", "[]", "{1}", "self()", "make_ref()" })
        {
            Add(
                "bad-two-value-" + cases.Count,
                "try process_flag(save_calls," + value + ") of _->error(accepted) catch error:badarg->ok end,{last_calls,false}=process_info(self(),last_calls)"
            );
            Add(
                "bad-three-value-" + cases.Count,
                "try erlang:process_flag(self(),save_calls," + value + ") of _->error(accepted) catch error:badarg->ok end,{last_calls,false}=process_info(self(),last_calls)"
            );
        }
        foreach (string flag in new[] { "trap_exit", "priority", "wrong", "42", "[]" })
            Add(
                "three-only-save-calls-" + cases.Count,
                "try erlang:process_flag(self()," + flag + ",true) of _->error(accepted) catch error:badarg->ok end"
            );
        foreach (string pid in new[] { "42", "atom", "[]", "make_ref()", "{self()}", "<<1>>" })
            Add(
                "bad-target-first-" + cases.Count,
                "try erlang:process_flag(" + pid + ",wrong,wrong) of _->error(accepted) catch error:badarg:S->[{erlang,process_flag,_,L}|_]=S,{error_info,#{module:=erl_erts_errors,cause:=badtype}}=lists:keyfind(error_info,1,L) end"
            );
        const string ready = "Self=self(),{P,Ref}=spawn_monitor(fun()->worker(Self) end),receive {ready,P}->ok after 1000->error(no_ready) end,";
        const string stop = ",P!stop,receive {'DOWN',Ref,process,P,normal}->ok after 1000->error(no_down) end";
        Add(
            "remote-replacement-empty",
            ready + "0=erlang:process_flag(P,save_calls,8),{last_calls,[]}=process_info(P,last_calls),8=erlang:process_flag(P,save_calls,8),{last_calls,[]}=process_info(P,last_calls),8=erlang:process_flag(P,save_calls,0),{last_calls,false}=process_info(P,last_calls)" + stop
        );
        Add(
            "remote-events-bounded",
            ready + "0=erlang:process_flag(P,save_calls,8),P!run,receive {finished,P,V}->true=is_integer(V) after 1000->error(no_finished) end,{last_calls,L}=process_info(P,last_calls),true=length(L)=<8,true=lists:member('receive',L),true=lists:member(send,L),8=erlang:process_flag(P,save_calls,0)" + stop
        );
        Add(
            "remote-ring-one",
            ready + "0=erlang:process_flag(P,save_calls,1),P!run,receive {finished,P,_}->ok after 1000->error(no_finished) end,{last_calls,[send]}=process_info(P,last_calls),1=erlang:process_flag(P,save_calls,0)" + stop
        );
        Add(
            "remote-invalid-preserves",
            ready + "0=erlang:process_flag(P,save_calls,8),try erlang:process_flag(P,save_calls,10001) of _->error(accepted) catch error:badarg->ok end,8=erlang:process_flag(P,save_calls,0)" + stop
        );
        Add(
            "paused-control-update",
            ready + "true=erlang:suspend_process(P),0=erlang:process_flag(P,save_calls,8),{last_calls,[]}=process_info(P,last_calls),8=erlang:process_flag(P,save_calls,0),true=erlang:resume_process(P)" + stop
        );
        Add(
            "dead-target",
            "{P,Ref}=spawn_monitor(fun()->ok end),receive {'DOWN',Ref,process,P,normal}->ok end,try erlang:process_flag(P,save_calls,8) of _->error(accepted) catch error:badarg:S->[{erlang,process_flag,_,L}|_]=S,{error_info,#{cause:=badtype}}=lists:keyfind(error_info,1,L) end"
        );
        Add(
            "last-calls-list-query",
            "[{last_calls,false},{trap_exit,false},{last_calls,false}]=process_info(self(),[last_calls,trap_exit,last_calls])"
        );

        return cases;
    }
}
