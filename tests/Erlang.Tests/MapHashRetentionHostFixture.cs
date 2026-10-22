using Erlang;

namespace Erlang.Tests;

internal sealed class MapHashRetentionHostFixture : IAsyncDisposable
{
    internal ProcessRuntime Runtime { get; } = new();
    internal RuntimeNodeFoundationHostActor Reader { get; private set; } = null!;
    private RuntimeNodeFoundationHostActor kernel = null!;

    internal static async Task<MapHashRetentionHostFixture> Create()
    {
        var fixture = new MapHashRetentionHostFixture();
        try
        {
            fixture.Reader = new(fixture.Runtime);
            fixture.kernel = new(fixture.Runtime);
            await fixture.kernel.InvokeAsync(async context =>
            {
                fixture.Runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);

                return await fixture.Runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ConnectedIngressHostNames.SetNode,
                    Term.A(MapHashRetentionHostNames.Local),
                    new Integer(MapHashRetentionHostNames.LocalCreation)
                );
            });

            return fixture;
        }
        catch (Exception setup)
        {
            try
            {
                await fixture.DisposeAsync();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(setup, cleanup);
            }
            throw;
        }
    }

    internal async Task<Term> AdoptAndHash(string kind)
    {
        await using var controller = new RuntimeNodeFoundationHostActor(Runtime);
        Atom node = Term.A(ProcessNodeDefaults.Local);
        _ = Runtime.DistributionPending.BeginPending(node);
        _ = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            node,
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), new Integer(MapHashRetentionHostNames.RemoteCreation))
        ));
        Term key = kind switch
        {
            IdentifierLifetimeHostNames.Port => new PortTerm(ProcessNodeDefaults.Local, 1, MapHashRetentionHostNames.RemoteCreation),
            IdentifierLifetimeHostNames.Reference or IdentifierLifetimeHostNames.Function =>
                new ReferenceTerm(ProcessNodeDefaults.Local, new uint[] { 1, 2, 3 }, MapHashRetentionHostNames.RemoteCreation),
            _ => new Pid(ProcessNodeDefaults.Local, 1, MapHashRetentionHostNames.RemoteCreation)
        };
        MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.Observed(Runtime, key) is null);
        Term root = kind == IdentifierLifetimeHostNames.Function ? MapHashRetentionHostAccess.Carrier(key) : key;
        using (var scope = new ProplistOwnHashScope(Runtime))
            _ = scope.Hash(root);
        MapHashRetentionHostAccess.Check(IdentifierLifetimeHostAccess.IsIdentity(IdentifierLifetimeHostAccess.Observed(Runtime, key)));

        return root;
    }

    internal Task StopKernel() => kernel.DisposeAsync().AsTask();

    internal Task<Term> ReadCreation() => Reader.InvokeAsync(async context => await Runtime.Modules.Call(
        context,
        IdentifierLifetimeHostNames.NativeModule,
        IdentifierLifetimeHostNames.Getter
    ));

    public async ValueTask DisposeAsync()
    {
        var errors = new List<Exception>();
        try
        {
            foreach (var actor in new[] { kernel, Reader })
                if (actor is not null)
                    try
                    {
                        await actor.DisposeAsync().AsTask().WaitAsync(MapHashRetentionHostNames.Deadline);
                    }
                    catch (Exception error)
                    {
                        errors.Add(error);
                    }
        }
        finally
        {
            try
            {
                await Runtime.DisposeAsync().AsTask().WaitAsync(MapHashRetentionHostNames.Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (errors.Count != 0)
            throw new AggregateException(errors);
    }
}
