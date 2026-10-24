namespace Erlang.Tests;

internal static class DdllErrorHost416Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DdllErrorHost416Names.Failure);
    }

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            RuntimeBootstrapDdllError416.Register(runtime, registry);
            int count = registry.Exports.Count();
            RuntimeBootstrapDdllError416.Register(runtime, registry);
            Require(count == registry.Exports.Count());
        });
        await foreign.InitializeModulesAsync(_ => { });
        int before = foreign.Modules.Exports.Count();
        bool denied = false;
        try
        {
            RuntimeBootstrapDdllError416.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            denied = true;
        }
        Require(denied && before == foreign.Modules.Exports.Count());
        await using var actor = new RuntimeNodeFoundationHostActor(foreign);
        await actor.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                _ = await runtime.Modules.Call(
                    context,
                    DdllErrorOriginal416Names.Internal,
                    DdllErrorOriginal416Names.Function,
                    Nil.Value
                );
            }
            catch (ArgumentException)
            {
                refused = true;
            }
            Require(refused);

            return true;
        });
        await using var conflict = new ProcessRuntime();
        await conflict.InitializeModulesAsync(registry =>
        {
            registry.Register(
                DdllErrorOriginal416Names.Internal,
                DdllErrorOriginal416Names.Function,
                1,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            int count = registry.Exports.Count();
            bool refused = false;
            try
            {
                RuntimeBootstrapDdllError416.Register(conflict, registry);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && count == registry.Exports.Count());
        });
    }
}
