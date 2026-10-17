namespace Erlang.Compiler;

internal static class SourceCallResolution
{
    public static string? ExternalModule(ModuleDefinition? module, string name, int arity)
    {
        if (module?.CallBindings.ImportedModule(name, arity) is { } imported)
            return imported;

        return AutoImportSignatures.Contains(name, arity) && module?.CallBindings.IsSuppressed(name, arity) != true ? ErlangModuleNames.Module : null;
    }

    public static bool IsGuardAutoImport(ModuleDefinition? module, string name, int arity)
    {
        if (module?.Functions.Any(function => function.Name == name && function.Arity == arity) == true)
            return false;

        return ExternalModule(module, name, arity) == ErlangModuleNames.Module;
    }

    public static bool IsGuardFilter(ModuleDefinition? module, string name, int arity) => module?.CallBindings.ImportedModule(name, arity) is null && module?.Functions.Any(function => function.Name == name && function.Arity == arity) != true;

    public static void ValidateDeclarations(ModuleDefinition module)
    {
        foreach (var import in module.CallBindings.Imports)
        {
            if (module.Functions.Any(function => function.Name == import.Name && function.Arity == import.Arity))
                throw new CompileException(CompilerDiagnosticCodes.FunctionDefinition, SourceCallBindingDiagnostics.ImportLocal(import.Name, import.Arity), 0);
        }
    }
}
