namespace Erlang.Differential;

public static class ProcessCodeCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ProcessCodeCheckNames.Function ? arity switch
    {
        2 => "erlang-process-code-old-self",
        3 => "erlang-process-code-async-old-self",
        _ => null
    } : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-process-code-" + name,
            "-module(process_code_contract). -export([run/0]). run()->" + body +
            ",done. wait(Parent)->Parent!{ready,self()},receive finish->kept end. local_check()->erlang:check_process_code(self(),process_code_contract).",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("current", "false=erlang:check_process_code(self(),process_code_contract)");
        Add("unknown", "false=erlang:check_process_code(self(),process_code_missing)");
        Add(
            "old-self",
            "true=erlang:delete_module(process_code_contract),true=erlang:check_process_code(self(),process_code_contract)"
        );
        Add(
            "old-empty-options",
            "true=erlang:delete_module(process_code_contract),true=erlang:check_process_code(self(),process_code_contract,[])"
        );
        Add("old-local", "true=erlang:delete_module(process_code_contract),true=local_check()");
        Add(
            "allow-gc-false",
            "true=erlang:delete_module(process_code_contract),true=erlang:check_process_code(self(),process_code_contract,[{allow_gc,false}])"
        );
        Add(
            "allow-gc-repeat",
            "true=erlang:delete_module(process_code_contract),true=erlang:check_process_code(self(),process_code_contract,[{allow_gc,true},{allow_gc,false}])"
        );
        Add(
            "async-current-self",
            "async=erlang:check_process_code(self(),process_code_contract,[{async,request}]),receive {check_process_code,request,false}->ok end"
        );
        Add(
            "async-old-self",
            "true=erlang:delete_module(process_code_contract),async=erlang:check_process_code(self(),process_code_contract,[{async,request}]),receive {check_process_code,request,true}->ok end"
        );
        Add(
            "async-arbitrary-id",
            "R={request,[first|tail]},async=erlang:check_process_code(self(),process_code_contract,[{async,R}]),receive {check_process_code,R,false}->ok end"
        );
        Add(
            "async-last-wins",
            "async=erlang:check_process_code(self(),process_code_contract,[{async,first},{allow_gc,false},{async,last}]),receive {check_process_code,last,false}->ok end,receive {check_process_code,first,_}->error(extra) after 0->ok end"
        );
        Add(
            "async-duplicate-id",
            "async=erlang:check_process_code(self(),process_code_contract,[{async,same}]),async=erlang:check_process_code(self(),process_code_contract,[{async,same}]),receive {check_process_code,same,false}->ok end,receive {check_process_code,same,false}->ok end"
        );
        Add(
            "remote-current",
            "Parent=self(),P=spawn(fun()->wait(Parent) end),receive {ready,P}->ok end,false=erlang:check_process_code(P,process_code_contract),M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok end"
        );
        Add(
            "remote-old",
            "Parent=self(),P=spawn(fun()->wait(Parent) end),receive {ready,P}->ok end,true=erlang:delete_module(process_code_contract),true=erlang:check_process_code(P,process_code_contract),M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok end"
        );
        Add(
            "async-remote-old",
            "Parent=self(),P=spawn(fun()->wait(Parent) end),receive {ready,P}->ok end,true=erlang:delete_module(process_code_contract),async=erlang:check_process_code(P,process_code_contract,[{async,request}]),receive {check_process_code,request,true}->ok end,M=monitor(process,P),P!finish,receive {'DOWN',M,process,P,normal}->ok end"
        );
        Add(
            "dead",
            "P=spawn(fun()->ok end),M=monitor(process,P),receive {'DOWN',M,process,P,_}->ok end,false=erlang:check_process_code(P,process_code_contract)"
        );
        Add(
            "async-dead",
            "P=spawn(fun()->ok end),M=monitor(process,P),receive {'DOWN',M,process,P,_}->ok end,async=erlang:check_process_code(P,process_code_contract,[{async,request}]),receive {check_process_code,request,false}->ok end"
        );
        Add(
            "pid-type",
            "try erlang:check_process_code(not_a_pid,process_code_contract) of _->error(accepted) catch error:badarg->ok end"
        );
        Add("module-type", "try erlang:check_process_code(self(),42) of _->error(accepted) catch error:badarg->ok end");
        Add(
            "options-type",
            "try erlang:check_process_code(self(),process_code_contract,wrong) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "options-improper",
            "try erlang:check_process_code(self(),process_code_contract,[{async,request}|tail]) of _->error(accepted) catch error:badarg->ok end,receive {check_process_code,request,_}->error(extra) after 0->ok end"
        );
        Add(
            "allow-gc-type",
            "try erlang:check_process_code(self(),process_code_contract,[{allow_gc,wrong}]) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "option-arity",
            "try erlang:check_process_code(self(),process_code_contract,[{async,request,extra}]) of _->error(accepted) catch error:badarg->ok end"
        );
        Add(
            "option-cause",
            "try erlang:check_process_code(self(),process_code_contract,[wrong]) of _->error(accepted) catch error:badarg:S->[{_,_,_,L}|_]=S,{error_info,Info}=lists:keyfind(error_info,1,L),bad_option=maps:get(cause,Info),erl_erts_errors=maps:get(module,Info) end"
        );
        Add(
            "option-validation-first",
            "try erlang:check_process_code(wrong,42,[wrong]) of _->error(accepted) catch error:badarg:S->[{_,_,_,L}|_]=S,{error_info,Info}=lists:keyfind(error_info,1,L),bad_option=maps:get(cause,Info) end"
        );

        return cases;
    }
}
