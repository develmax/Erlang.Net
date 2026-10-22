using Erlang;
namespace Erlang.Tests;

internal static class InternalMonitor425HostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapInternalTrace425.Register(runtime, registry));
        await using var foreign = new ProcessRuntime();
        await foreign.InitializeModulesAsync(registry => RuntimeBootstrapInternalTrace425.Register(foreign, registry));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        Term strong = await InternalTrace425HostAccess.Create(actor, Term.A(InternalMonitor425HostNames.Undefined));
        Term foreignStrong = await InternalTrace425HostAccess.Create(other, Term.A(InternalMonitor425HostNames.Undefined));
        Term weak = ((TupleTerm)strong).Items[1];
        await actor.InvokeAsync(async context =>
        {
            var read = InternalProcess425HostAccess.Body(runtime, InternalMonitor425HostNames.Monitor, 1);
            var change = InternalProcess425HostAccess.Body(runtime, InternalMonitor425HostNames.Monitor, 3);
            foreach (Term descriptor in new[] { strong, weak })
            {
                bool readUnsupported = false, changeUnsupported = false;
                try
                {
                    await read(context, new Term[] { descriptor });
                }
                catch (NotSupportedException)
                {
                    readUnsupported = true;
                }
                try
                {
                    await change(context, new Term[] { descriptor, Term.A(InternalMonitor425HostNames.Undefined), Nil.Value });
                }
                catch (NotSupportedException)
                {
                    changeUnsupported = true;
                }
                InternalProcess425HostAccess.Require(readUnsupported && changeUnsupported);
            }
            foreach (Term descriptor in new Term[] { runtime.MakeReference(), Term.Tuple(Term.A(InternalMonitor425HostNames.Bad), Term.I(0)), foreignStrong })
                await BadArgument(context, read, descriptor);
            InternalProcess425HostAccess.Require((await read(context, new Term[] { Term.A(InternalMonitor425HostNames.Legacy) })).Equals(Term.A(InternalMonitor425HostNames.Undefined)));

            return true;
        });
        await InternalTrace425HostAccess.Call(actor, InternalMonitor425HostNames.Destroy, strong);
        await actor.InvokeAsync(async context =>
        {
            var read = InternalProcess425HostAccess.Body(runtime, InternalMonitor425HostNames.Monitor, 1);
            await BadArgument(context, read, strong);
            await BadArgument(context, read, weak);

            return true;
        });
    }

    private static async Task BadArgument(ProcessContext context, ErlangFunction body, Term descriptor)
    {
        bool bad = false;
        try
        {
            await body(context, new Term[] { descriptor });
        }
        catch (ErlangException error)
        {
            bad = error.Reason.Equals(Term.A(InternalMonitor425HostNames.BadArgument));
        }
        InternalProcess425HostAccess.Require(bad);
    }
}
