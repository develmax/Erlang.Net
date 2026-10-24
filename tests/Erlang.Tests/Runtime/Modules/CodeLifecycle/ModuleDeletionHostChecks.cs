using Erlang.Compiler;

namespace Erlang.Tests;

public static class ModuleDeletionHostChecks
{
    public static Task Run()
    {
        var registry = new ModuleRegistry();
        var first = new Parser("-module(module_deletion_host). -export([run/0]). run()->first.").ParseModule();
        var second = new Parser("-module(module_deletion_host). -export([run/0]). run()->second.").ParseModule();
        first.Register(registry);
        Term information = registry.ModuleInformation(ModuleDeletionHostNames.Module);
        if (registry.HasOldCode(ModuleDeletionHostNames.Module) || !registry.DeleteModule(ModuleDeletionHostNames.Missing).Equals(Term.A(ModuleCodeLifecycleNames.UnknownModule)))
            throw new InvalidOperationException(ModuleDeletionHostNames.Broken);
        try
        {
            registry.Apply(target =>
            {
                target.DeleteModule(ModuleDeletionHostNames.Module);
                second.Register(target);
                throw new InvalidOperationException(ModuleDeletionHostNames.Broken);
            });
        }
        catch (InvalidOperationException exception) when (exception.Message == ModuleDeletionHostNames.Broken)
        {
        }
        if (registry.HasOldCode(ModuleDeletionHostNames.Module) || !registry.IsModuleLoaded(ModuleDeletionHostNames.Module) || !ReferenceEquals(information, registry.ModuleInformation(ModuleDeletionHostNames.Module)))
            throw new InvalidOperationException(ModuleDeletionHostNames.Broken);
        registry.DeleteModule(ModuleDeletionHostNames.Module);
        if (!registry.HasOldCode(ModuleDeletionHostNames.Module) || registry.IsModuleLoaded(ModuleDeletionHostNames.Module) || registry.HasExport(ModuleDeletionHostNames.Module, ModuleDeletionHostNames.Run, 0))
            throw new InvalidOperationException(ModuleDeletionHostNames.Broken);
        second.Register(registry);
        Term secondInformation = registry.ModuleInformation(ModuleDeletionHostNames.Module);
        if (!registry.HasOldCode(ModuleDeletionHostNames.Module) || !registry.IsModuleLoaded(ModuleDeletionHostNames.Module) || information.Equals(secondInformation))
            throw new InvalidOperationException(ModuleDeletionHostNames.Broken);
        bool rejected = false;
        try
        {
            registry.DeleteModule(ModuleDeletionHostNames.Module);
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            rejected = true;
        }
        if (!rejected || !ReferenceEquals(secondInformation, registry.ModuleInformation(ModuleDeletionHostNames.Module)))
            throw new InvalidOperationException(ModuleDeletionHostNames.Broken);

        return Task.CompletedTask;
    }
}
