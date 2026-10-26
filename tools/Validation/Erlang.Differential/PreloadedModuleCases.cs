namespace Erlang.Differential;

public static class PreloadedModuleCases
{
    public const string ContractFunction = "pre_loaded";
    public const string ContractFixture = "erlang-preloaded-proper-list";

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("proper-list", "true=is_list(pre_loaded())"),
        Create("atoms", "true=lists:all(fun is_atom/1,pre_loaded())"),
        Create("unique", "L=pre_loaded(),true=(length(L)=:=length(lists:usort(L)))"),
        Create("erlang", "true=lists:member(erlang,pre_loaded())"),
        Create("init", "true=lists:member(init,pre_loaded())"),
        Create("loaded-subset", "L=erlang:loaded(),true=lists:all(fun(M)->lists:member(M,L) end,pre_loaded())"),
        Create("compiled-user-excluded", "true=module_loaded(preloaded_contract),false=lists:member(preloaded_contract,pre_loaded())"),
        Create(
            "unknown-excluded",
            "false=module_loaded(preloaded_unknown),false=lists:member(preloaded_unknown,pre_loaded()),false=module_loaded(preloaded_unknown)"
        ),
        Create("repeat-stable", "A=pre_loaded(),B=pre_loaded(),true=(A=:=B)"),
        Create("dictionary-pure", "undefined=get(preloaded_mark),put(preloaded_mark,kept),_=pre_loaded(),kept=get(preloaded_mark)"),
        Create("qualified", "true=(pre_loaded()=:=erlang:pre_loaded())"),
        Create("dynamic", "F=pre_loaded,true=(pre_loaded()=:=apply(erlang,F,[]))"),
        Create(
            "child",
            "Parent=self(),L=pre_loaded(),{P,R}=spawn_monitor(fun()->Parent!{self(),pre_loaded()} end),receive {P,L}->ok after 1000->error(missing) end,receive {'DOWN',R,process,P,normal}->ok after 1000->error(missing) end"
        ),
        Create("nested", "F=fun()->pre_loaded() end,true=(F()=:=pre_loaded())")
    ];

    public static string? FixtureName(string function, int arity) =>
        function == ContractFunction && arity == 0 ? ContractFixture : null;

    private static CompiledModuleCase Create(string name, string body) => new(
        "erlang-preloaded-" + name,
        "-module(preloaded_contract). -export([run/0]). run()->" + body + ",done.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
