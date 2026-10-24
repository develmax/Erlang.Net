namespace Erlang.Tests;

internal sealed class DistChannel429Host : IAsyncDisposable
{
    internal ProcessRuntime Runtime { get; } = new(2);
    internal RuntimeNodeFoundationHostActor? Kernel { get; private set; }
    internal RuntimeNodeFoundationHostActor? Requester { get; private set; }
    internal RuntimeNodeFoundationHostActor? Controller { get; private set; }

    internal static async Task<DistChannel429Host> Create()
    {
        var value = new DistChannel429Host();
        try
        {
            await value.Runtime.InitializeModulesAsync(registry =>
         {
             RuntimeBootstrapDistributionProfile429.Register(value.Runtime, registry);
             RuntimeBootstrapDistChannel429.Register(value.Runtime, registry);
         });
            value.Kernel = new(value.Runtime);
            value.Requester = new(value.Runtime);
            value.Controller = new(value.Runtime);
            await value.Kernel.InvokeAsync(async c =>
            {
                await value.Runtime.Modules.Call(
                    c,
                    DistChannel429HostNames.Runtime,
                    DistChannel429HostNames.Register,
                    Term.A(DistChannel429HostNames.Kernel),
                    c.Self
                );

                return await value.Runtime.Modules.Call(
                    c,
                    DistChannel429HostNames.Runtime,
                    DistChannel429HostNames.SetNode,
                    Term.A(DistChannel429HostNames.Local),
                    Term.I(1)
                );
            });
            await value.Requester.InvokeAsync(async c => await Bind(c, 1));
            await value.Controller.InvokeAsync(async c => await Bind(c, 2));

            return value;
        }
        catch (Exception primary)
        {
            try
            {
                await value.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    internal static ValueTask<Term> Bind(ProcessContext c, int lane) => c.Runtime.Modules.Call(
        c,
        DistChannel429HostNames.Runtime,
        DistChannel429HostNames.Flag,
        Term.A(DistChannel429HostNames.Scheduler),
        Term.I(lane)
    );

    internal static ValueTask<Term> CreateChannel(ProcessContext c, string node, Pid controller) => c.Runtime.Modules.Call(
        c,
        DistChannel429HostNames.Module,
        DistChannel429HostNames.Function,
        Term.A(node),
        controller,
        Term.Tuple(new Integer(DistributionCapabilityProfile429.Mandatory), Term.I(1))
    );

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        foreach (var actor in new[] { Controller, Requester, Kernel }) if (actor is not null) try
            {
                await actor.DisposeAsync();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        try
        {
            await Runtime.DisposeAsync();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
