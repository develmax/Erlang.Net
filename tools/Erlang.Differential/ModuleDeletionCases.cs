namespace Erlang.Differential;

public static class ModuleDeletionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("current", "false=erlang:check_old_code(module_deletion_contract)"),
        Create(
            "unknown",
            "false=erlang:check_old_code(module_deletion_missing),undefined=erlang:delete_module(module_deletion_missing),false=erlang:check_old_code(module_deletion_missing)"
        ),
        Create("delete", "true=erlang:delete_module(module_deletion_contract),true=erlang:check_old_code(module_deletion_contract)"),
        Create(
            "loaded",
            "true=erlang:delete_module(module_deletion_contract),false=erlang:module_loaded(module_deletion_contract),true=lists:member(module_deletion_contract,erlang:loaded())"
        ),
        Create(
            "exports",
            "true=erlang:delete_module(module_deletion_contract),false=erlang:function_exported(module_deletion_contract,run,0),false=erlang:function_exported(module_deletion_contract,module_info,0)"
        ),
        Create(
            "qualified-after-delete",
            "true=erlang:delete_module(module_deletion_contract),try module_deletion_contract:run() of _->error(accepted) catch error:undef->ok end"
        ),
        Create("local", "true=erlang:delete_module(module_deletion_contract),kept=hidden()"),
        Create("fun", "F=fun hidden/0,true=erlang:delete_module(module_deletion_contract),kept=F()"),
        Create("anonymous", "F=fun()->kept end,true=erlang:delete_module(module_deletion_contract),kept=F()"),
        Create(
            "metadata",
            "true=erlang:delete_module(module_deletion_contract),try erlang:get_module_info(module_deletion_contract) of _->error(accepted) catch error:badarg->ok end"
        ),
        Create(
            "repeat",
            "true=erlang:delete_module(module_deletion_contract),try erlang:delete_module(module_deletion_contract) of _->error(accepted) catch error:badarg->ok end,true=erlang:check_old_code(module_deletion_contract)"
        ),
        Create("delete-type", "try erlang:delete_module([]) of _->error(accepted) catch error:badarg->ok end"),
        Create("old-type", "try erlang:check_old_code(42) of _->error(accepted) catch error:badarg->ok end")
    ];

    public static string? FixtureName(string function, int arity) => arity == 1 ? function switch
    {
        ModuleCodeLifecycleNames.DeleteModule => "erlang-module-deletion-delete",
        ModuleCodeLifecycleNames.CheckOldCode => "erlang-module-deletion-current",
        _ => null
    } : null;

    private static CompiledModuleCase Create(string name, string body) => new(
        "erlang-module-deletion-" + name,
        "-module(module_deletion_contract). -export([run/0]). run()->" + body + ",done. hidden()->kept.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
