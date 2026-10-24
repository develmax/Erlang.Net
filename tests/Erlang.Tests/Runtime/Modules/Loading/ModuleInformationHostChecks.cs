using Erlang.Compiler;

namespace Erlang.Tests;

public static class ModuleInformationHostChecks
{
    public static Task Run()
    {
        var registry = new ModuleRegistry();
        var empty = new Parser("-module(module_information_host).").ParseModule();
        if (empty.DeclaredFunctions.Count != 0 || empty.DeclaredExports.Count != 0 || empty.Functions.Count != 2 || empty.Exports.Count != 2)
            throw new InvalidOperationException(ModuleInformationHostNames.Broken);
        empty.Register(registry);
        Term snapshot = registry.ModuleInformation(ModuleInformationHostNames.Module);
        Term fingerprint = registry.ModuleInformation(ModuleInformationHostNames.Module, ModuleInformationHostNames.Md5);
        if (!ReferenceEquals(snapshot, registry.ModuleInformation(ModuleInformationHostNames.Module)))
            throw new InvalidOperationException(ModuleInformationHostNames.Broken);
        try
        {
            registry.Apply(target =>
            {
                target.Register(
                    ModuleInformationHostNames.Module,
                    ModuleInformationHostNames.Added,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
                target.PublishModuleCode(ModuleInformationHostNames.Module, ModuleCodeFingerprints.Owner(typeof(ModuleInformationHostChecks)));
                target.PublishModule(ModuleInformationHostNames.Failed);
                throw new InvalidOperationException(ModuleInformationHostNames.Broken);
            });
        }
        catch (InvalidOperationException exception) when (exception.Message == ModuleInformationHostNames.Broken)
        {
        }
        if (registry.IsModuleLoaded(ModuleInformationHostNames.Failed) || registry.HasExport(ModuleInformationHostNames.Module, ModuleInformationHostNames.Added, 0) || !ReferenceEquals(snapshot, registry.ModuleInformation(ModuleInformationHostNames.Module)))
            throw new InvalidOperationException(ModuleInformationHostNames.Broken);
        registry.Register(
            ModuleInformationHostNames.Module,
            ModuleInformationHostNames.Added,
            0,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        if (fingerprint.Equals(registry.ModuleInformation(ModuleInformationHostNames.Module, ModuleInformationHostNames.Md5)) || ReferenceEquals(snapshot, registry.ModuleInformation(ModuleInformationHostNames.Module)))
            throw new InvalidOperationException(ModuleInformationHostNames.Broken);
        registry.PublishModule(ModuleInformationHostNames.Bare);
        bool rejected = false;
        try
        {
            registry.ModuleInformation(ModuleInformationHostNames.Bare);
        }
        catch (ErlangException exception) when (exception.Reason is Atom { Name: ErlangErrorReasons.BadArgument })
        {
            rejected = true;
        }
        if (!rejected)
            throw new InvalidOperationException(ModuleInformationHostNames.Broken);

        return Task.CompletedTask;
    }
}
