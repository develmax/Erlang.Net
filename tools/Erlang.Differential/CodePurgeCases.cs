namespace Erlang.Differential;

public static class CodePurgeCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ModuleCodeLifecycleNames.PurgeModule && arity == 1 ? "erlang-code-purge-deleted-tail" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body, Term expected) => cases.Add(new(
            "erlang-code-purge-" + name,
            "-module(code_purge_contract). -export([run/0]). run()->" + body + ". hidden()->kept. loop()->loop().",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), expected)
        ));

        Add(
            "deleted-tail",
            "true=erlang:delete_module(code_purge_contract),erlang:purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        Add(
            "heap-named",
            "F=fun hidden/0,put(held,F),true=erlang:delete_module(code_purge_contract),erlang:purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        Add(
            "heap-anonymous",
            "F=fun()->kept end,put(held,F),true=erlang:delete_module(code_purge_contract),erlang:purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        Add(
            "unqualified-tail",
            "true=erlang:delete_module(code_purge_contract),purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        Add(
            "old-receive",
            "Parent=self(),spawn(fun()->Parent!ready,receive stop->ok end end),receive ready->ok end,true=erlang:delete_module(code_purge_contract),erlang:purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        Add(
            "old-trap-exit",
            "Parent=self(),spawn(fun()->process_flag(trap_exit,true),Parent!ready,receive stop->ok end end),receive ready->ok end,true=erlang:delete_module(code_purge_contract),erlang:purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        Add(
            "old-local-tail",
            "Parent=self(),spawn(fun()->Parent!ready,loop() end),receive ready->ok end,true=erlang:delete_module(code_purge_contract),erlang:purge_module(code_purge_contract)",
            Term.A(ErlangBooleanAtoms.True)
        );
        foreach (var value in new[] { "code_purge_contract", "code_purge_missing", "[]", "42", "1.0", "{}", "#{}", "<<>>", "fun hidden/0", "erlang:make_fun(erlang,self,0)", "self()", "make_ref()" })
            Add(
                "badarg-" + cases.Count,
                "try erlang:purge_module(" + value + ") of _->error(accepted) catch error:badarg->done end",
                Term.A(CompiledContractOutcomeNames.Completion)
            );
        Add(
            "error-info",
            "try erlang:purge_module(code_purge_missing) of _->error(accepted) catch error:badarg:S->[{erlang,purge_module,[code_purge_missing],Info}|_]=S,{error_info,#{module:=erl_erts_errors}}=lists:keyfind(error_info,1,Info),done end",
            Term.A(CompiledContractOutcomeNames.Completion)
        );

        return cases;
    }
}
