namespace Erlang.Differential;

public static class ModuleLoadingCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        (ModuleLoadingContractNames.ModuleLoaded, 1) => ModuleLoadingContractNames.CurrentFixture,
        (ModuleLoadingContractNames.Loaded, 0) => ModuleLoadingContractNames.LoadedFixture,
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-module-loading-" + name,
            "-module(module_loading_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add(
            "current",
            "true=erlang:module_loaded(module_loading_contract),true=erlang:module_loaded(erlang),true=module_loaded(module_loading_contract)"
        );
        Add(
            "unknown",
            "false=module_loaded(module_loading_not_present),false=erlang:module_loaded(''),false=erlang:module_loaded('name with spaces')"
        );
        Add(
            "list",
            "L=erlang:loaded(),true=is_list(L),true=lists:member(module_loading_contract,L),true=lists:member(erlang,L),true=lists:all(fun(X)->is_atom(X) end,L),true=(length(L)=:=length(lists:usort(L)))"
        );
        Add(
            "list-no-autoload",
            "false=module_loaded(module_loading_not_present),L=erlang:loaded(),false=lists:member(module_loading_not_present,L),false=module_loaded(module_loading_not_present)"
        );
        Add(
            "query-pure",
            "P=self(),true=module_loaded(module_loading_contract),undefined=get(module_loading_query_mark),put(module_loading_query_mark,kept),L1=erlang:loaded(),L2=erlang:loaded(),true=(lists:usort(L1)=:=lists:usort(L2)),kept=get(module_loading_query_mark),P=self()"
        );
        Add(
            "child-current",
            "Parent=self(),{P,R}=spawn_monitor(fun()->Parent!{self(),module_loaded(module_loading_contract),lists:member(module_loading_contract,erlang:loaded())} end),receive {P,true,true}->ok after 1000->error(missing) end,receive {'DOWN',R,process,P,normal}->ok after 1000->error(missing) end"
        );
        int index = 0;
        foreach (string value in new[] { "0", "-1", "576460752303423488", "1.0", "[]", "[erlang]", "<<\"erlang\">>", "{}", "#{a=>1}", "self()", "make_ref()", "fun()->ok end" })
            Add("bad-type-" + index++, "ok=try erlang:module_loaded(" + value + ") of _->unexpected catch error:badarg->ok end");

        return cases;
    }
}
