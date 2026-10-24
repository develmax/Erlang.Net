using Erlang;

namespace Erlang.Tests;

internal sealed class IdentifierLifetimeHostFixture : IAsyncDisposable
{
    internal ProcessRuntime Runtime { get; } = new(maximumProcesses: IdentifierLifetimeHostNames.MaximumProcesses);
    internal RuntimeNodeFoundationHostActor Reader { get; private set; } = null!;
    private RuntimeNodeFoundationHostActor kernel = null!;

    internal static async Task<IdentifierLifetimeHostFixture> Create()
    {
        var fixture = new IdentifierLifetimeHostFixture();
        try
        {
            fixture.Reader = new(fixture.Runtime);
            fixture.kernel = new(fixture.Runtime);
            await fixture.kernel.InvokeAsync(async context =>
            {
                fixture.Runtime.Register(IdentifierLifetimeHostNames.Kernel, context.Self);

                return await fixture.Runtime.Modules.Call(
                    context,
                    IdentifierLifetimeHostNames.ErlangModule,
                    IdentifierLifetimeHostNames.SetNode,
                    Term.A(IdentifierLifetimeHostNames.Local),
                    new Integer(IdentifierLifetimeHostNames.LocalCreation)
                );
            });

            return fixture;
        }
        catch (Exception creationError)
        {
            try
            {
                await fixture.DisposeAsync();
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException(creationError, cleanupError);
            }
            throw;
        }
    }

    internal async Task<Term> AdoptAndImport(string kind)
    {
        await using var controller = new RuntimeNodeFoundationHostActor(Runtime);
        var node = Term.A(ProcessNodeDefaults.Local);
        _ = Runtime.DistributionPending.BeginPending(node);
        _ = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            node,
            context.Self,
            Term.Tuple(new Integer(DistributionPassThroughFlags.Mandatory), new Integer(IdentifierLifetimeHostNames.RemoteCreation))
        ));
        Term dto = kind switch
        {
            IdentifierLifetimeHostNames.Port => new PortTerm(ProcessNodeDefaults.Local, 1, IdentifierLifetimeHostNames.RemoteCreation),
            IdentifierLifetimeHostNames.Reference or IdentifierLifetimeHostNames.Function =>
                new ReferenceTerm(ProcessNodeDefaults.Local, new uint[] { 1, 2, 3 }, IdentifierLifetimeHostNames.RemoteCreation),
            _ => new Pid(ProcessNodeDefaults.Local, 1, IdentifierLifetimeHostNames.RemoteCreation)
        };
        Term imported = Runtime.ImportExternalTerm(ExternalTermFormat.Encode(dto)).Term;
        if (kind != IdentifierLifetimeHostNames.Function)
            return imported;
        Term box = Term.Tuple(new MapTerm(new[] { new KeyValuePair<Term, Term>(Term.A(IdentifierLifetimeHostNames.Reference), Cons.From(new[] { imported })) }));
        var function = new FunctionTerm(
            new LocalFunctionIdentity(
                Term.A(IdentifierLifetimeHostNames.Module),
                null,
                0,
                0,
                new BitString(new byte[IdentifierLifetimeHostNames.FunctionUniqueBytes])
            ),
            0,
            new[] { box },
            (_, _) => ValueTask.FromResult(box)
        );
        using var reservation = Runtime.ReserveExternalTerm(function);

        return function;
    }

    internal Task StopKernel() => kernel.DisposeAsync().AsTask();

    internal Task<Term> ReadCreation() => Reader.InvokeAsync(async context => await Runtime.Modules.Call(context,
        IdentifierLifetimeHostNames.NativeModule, IdentifierLifetimeHostNames.Getter));

    internal static Term Key(Term root)
    {
        if (root is not FunctionTerm function)
            return root;
        var tuple = (TupleTerm)function.Environment[0];
        var map = (MapTerm)tuple.Items[0];

        return ((Cons)map.Entries.Single().Value).Head;
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        try
        {
            foreach (var actor in new[] { kernel, Reader })
                if (actor is not null)
                    try
                    {
                        await actor.DisposeAsync().AsTask().WaitAsync(IdentifierLifetimeHostNames.Deadline);
                    }
                    catch (Exception error)
                    {
                        failures.Add(error);
                    }
        }
        finally
        {
            try
            {
                await Runtime.DisposeAsync().AsTask().WaitAsync(IdentifierLifetimeHostNames.Deadline);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(failures);
    }
}
