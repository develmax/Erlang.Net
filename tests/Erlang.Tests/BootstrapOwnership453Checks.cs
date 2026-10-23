using Erlang;

namespace Erlang.Tests;

internal static class BootstrapOwnership453Checks
{
    internal static async Task Run()
    {
        await using (var canonical = new ProcessRuntime())
            await canonical.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapFileSystem.Register(canonical, registry);
                RuntimeBootstrapFileSystem.Register(canonical, registry);
            });
        foreach (bool sourceOwned in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            object owner = new();
            int calls = 0;
            await runtime.InitializeModulesAsync(registry =>
            {
                ErlangFunction substitute = (_, _) =>
                {
                    calls++;

                    return ValueTask.FromResult<Term>(Term.A(BootstrapOwnership453Names.Unexpected));
                };
                foreach ((string function, int arity) in new[]
                {
                    (KernelBootOriginal453Names.SpawnSystem, 3), (BootstrapOwnership453Names.IsSystem, 1)
                })
                {
                    if (sourceOwned)
                        registry.RegisterSource(
                            KernelBootOriginal453Names.Internal,
                            function,
                            arity,
                            substitute,
                            owner
                        );
                    else
                        registry.Register(
                            KernelBootOriginal453Names.Internal,
                            function,
                            arity,
                            substitute
                        );
                }
                bool refused = false;
                try
                {
                    RuntimeBootstrapFileSystem.Register(runtime, registry);
                }
                catch (InvalidOperationException error) when (error.Message == BootstrapOwnership453Names.Conflict)
                {
                    refused = true;
                }
                KernelBootOriginal453Access.Require(refused && calls == 0);
                foreach ((string function, int arity) in new[]
                {
                    (KernelBootOriginal453Names.SpawnSystem, 3), (BootstrapOwnership453Names.IsSystem, 1)
                })
                    KernelBootOriginal453Access.Require(ReferenceEquals(registry.SourceOwner(
                        KernelBootOriginal453Names.Internal, function, arity), sourceOwned ? owner : null));
            });
        }
    }
}
