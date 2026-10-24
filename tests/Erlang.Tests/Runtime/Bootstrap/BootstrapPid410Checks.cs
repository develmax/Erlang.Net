namespace Erlang.Tests;

internal static class BootstrapPid410Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(BootstrapPid410Names.Failure);
    }

    internal static async Task Run()
    {
        if (!OperatingSystem.IsWindows()) return;
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapFileSystem.Register(runtime, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        // The actual foreign live initializer has the same wire identity as our
        // initializer; never replace it with a newly constructed DTO stand-in.
        Require(foreign.InitializationPid.Equals(runtime.InitializationPid));
        await actor.InvokeAsync(async context =>
        {
            async Task<Term> Query(Pid pid) => await runtime.Modules.Call(
                context,
                BootstrapPid410Names.Module,
                BootstrapPid410Names.Function,
                pid
            );
            Require((await Query(runtime.InitializationPid)).Equals(Term.A(ErlangBooleanAtoms.True)));
            Require((await Query(context.Self)).Equals(Term.A(ErlangBooleanAtoms.False)));
            bool refused = false;
            try
            {
                _ = await Query(foreign.InitializationPid);
            }
            catch (ErlangException error)
            {
                refused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
            }
            Require(refused);
            // External DTO, deliberately different creation but same current
            // DistEntry node. Native checks the entry pointer, not creation.
            var oldCreation = new Pid(runtime.InitializationPid.Node,
                ulong.MaxValue, unchecked(runtime.InitializationPid.Creation + 1));
            Require((await Query(oldCreation)).Equals(Term.A(ErlangBooleanAtoms.False)));

            return true;
        });
        Pid ended = actor.Process.Pid;
        await actor.DisposeAsync();
        await using var observer = new RuntimeNodeFoundationHostActor(runtime);
        await observer.InvokeAsync(async context =>
        {
            Require((await runtime.Modules.Call(
                context,
                BootstrapPid410Names.Module,
                BootstrapPid410Names.Function,
                ended
            )).Equals(Term.A(ErlangBooleanAtoms.False)));

            return true;
        });
    }
}
