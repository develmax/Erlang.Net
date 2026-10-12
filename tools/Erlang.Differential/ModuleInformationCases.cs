namespace Erlang.Differential;

public static class ModuleInformationCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("module", "module_information_contract=module_info(module)"),
        Create("qualified", "module_information_contract=erlang:get_module_info(module_information_contract,module)"),
        Create(
            "all",
            "[{module,module_information_contract},{exports,E},{attributes,A},{compile,C},{md5,M}]=module_info(),true=is_list(E),true=is_list(A),true=is_list(C),true=is_binary(M)"
        ),
        Create("bif-all", "true=(module_info()=:=erlang:get_module_info(module_information_contract))"),
        Create("exports", "E=lists:sort(module_info(exports)),[{module_info,0},{module_info,1},{run,0}]=E"),
        Create(
            "functions",
            "secret=hidden(),F=lists:sort(module_info(functions)),[{hidden,0},{module_info,0},{module_info,1},{run,0}]=F"
        ),
        Create(
            "private",
            "false=lists:member({hidden,0},module_info(exports)),true=lists:member({hidden,0},module_info(functions)),secret=hidden()"
        ),
        Create("md5", "M=module_info(md5),16=byte_size(M),<<V:128>>=M,[{vsn,[V]}]=module_info(attributes)"),
        Create("compile", "C=module_info(compile),true=is_list(C),true=lists:all(fun(X)->is_tuple(X) end,C)"),
        Create("native", "false=module_info(native),[]=module_info(native_addresses)"),
        Create("nifs", "[]=module_info(nifs)"),
        Create("dynamic", "module_information_contract=apply(module_information_contract,module_info,[module])"),
        Create("named", "F=fun module_info/1,module_information_contract=F(module)"),
        Create("repeat", "A=module_info(),true=(A=:=module_info())"),
        Create("bad-key", "try module_info(unknown) of _->error(accepted) catch error:badarg->ok end"),
        Create("bad-key-type", "try module_info(42) of _->error(accepted) catch error:badarg->ok end"),
        Create("bad-module", "try erlang:get_module_info(module_information_missing) of _->error(accepted) catch error:badarg->ok end"),
        Create("bad-module-type", "try erlang:get_module_info([]) of _->error(accepted) catch error:badarg->ok end")
    ];

    public static string? FixtureName(string function, int arity) =>
        function == "get_module_info" ? arity switch
        {
            1 => "erlang-module-information-bif-all",
            2 => "erlang-module-information-qualified",
            _ => null
        } : null;

    private static CompiledModuleCase Create(string name, string body) => new(
        "erlang-module-information-" + name,
        "-module(module_information_contract). -export([run/0]). run()->" + body + ",done. hidden()->secret.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
