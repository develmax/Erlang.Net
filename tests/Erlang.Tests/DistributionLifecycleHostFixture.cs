using Erlang;
using System.Diagnostics;

namespace Erlang.Tests;

internal sealed class DistributionLifecycleHostFixture : IAsyncDisposable
{
    private readonly List<RuntimeNodeFoundationHostActor> actors = new();
    private readonly List<DistributionLifecycleHostConnection> connections = new();
    private bool disposed;
    internal ProcessRuntime Runtime { get; } = new(maximumProcesses: DistributionPublicationLifecycleHostNames.MaximumProcesses);
    internal RuntimeNodeFoundationHostActor Producer { get; private set; } = null!;
    internal DistributionLifecycleHostConnection Connection { get; private set; } = null!;
    internal bool ExpectCleanupFailure
    {
        get; set;
    }

    internal static async Task<DistributionLifecycleHostFixture> Create()
    {
        var fixture = new DistributionLifecycleHostFixture();
        var kernel = fixture.CreateActor();
        await kernel.InvokeAsync(async context =>
        {
            fixture.Runtime.Register(ConnectedDistributionHostNames.Kernel, context.Self);

            return await fixture.Runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ConnectedIngressHostNames.SetNode,
                Term.A(DistributionPublicationLifecycleHostNames.Local),
                Term.I(0)
            );
        });
        fixture.Connection = await fixture.CreateConnection(DistributionPublicationLifecycleHostNames.Remote);
        fixture.Producer = fixture.CreateActor();

        return fixture;
    }

    internal RuntimeNodeFoundationHostActor CreateActor()
    {
        var actor = new RuntimeNodeFoundationHostActor(Runtime);
        actors.Add(actor);

        return actor;
    }

    internal async Task<DistributionLifecycleHostConnection> CreateConnection(string node)
    {
        var controller = CreateActor();
        Term host = Runtime.DistributionPending.BeginPending(Term.A(node));
        Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
            context,
            Term.A(node),
            context.Self,
            Term.Tuple(
                new Integer(DistributionPassThroughFlags.Mandatory | DistributionPassThroughFlags.ProcessMonitors),
                Term.I(DistributionPublicationLifecycleHostNames.Creation)
            )
        ));
        await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, context.Self));
        var connection = new DistributionLifecycleHostConnection(
            host,
            opaque,
            controller,
            new Pid(node, DistributionPublicationLifecycleHostNames.RemoteId, DistributionPublicationLifecycleHostNames.Creation)
        );
        connections.Add(connection);

        return connection;
    }

    internal int Fill(DistributionLifecycleHostConnection connection)
    {
        var message = DistributionPassThroughCodec.Send(connection.Remote, Term.A(DistributionPublicationLifecycleHostNames.Filler));
        int count = 0;
        while (true)
        {
            try
            {
                Runtime.DistributionPending.EnqueueConnectedOutput(connection.Host, message);
                count++;
            }
            catch (InvalidOperationException)
            {
                return count;
            }
        }
    }

    internal async Task Refill(DistributionLifecycleHostConnection connection, int capacity)
    {
        var deadline = Stopwatch.StartNew();
        while (Runtime.DistributionPending.ConnectedSnapshot(connection.Host).QueuedPackets < capacity)
        {
            Fill(connection);
            Check(deadline.Elapsed < DistributionPublicationLifecycleHostNames.Deadline);
            await Task.Yield();
        }
    }

    internal async Task<DistributionDecodedProcessPacket> Take(DistributionLifecycleHostConnection connection)
    {
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            var buffer = await connection.Controller.Invoke(context => Runtime.DistributionPending.TakeConnectedOutput(connection.Host, context, out _));
            if (buffer is not null)
            {
                Check(buffer.SegmentCount == 1);

                return DistributionPassThroughCodec.DecodeProcessPacket(buffer.Segments[0].ToArray());
            }
            Check(deadline.Elapsed < DistributionPublicationLifecycleHostNames.Deadline);
            await Task.Yield();
        }
    }

    internal async Task DrainFiller(DistributionLifecycleHostConnection connection, int count)
    {
        for (int index = 0; index < count; index++)
            Check((await Take(connection)).Payload?.Equals(Term.A(DistributionPublicationLifecycleHostNames.Filler)) == true);
    }

    internal static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DistributionPublicationLifecycleHostNames.Failure);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;
        disposed = true;
        var failures = new List<Exception>();
        foreach (var connection in connections)
        {
            try
            {
                Runtime.DistributionPending.CloseConnected(connection.Host);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // An already retired or halted connection has no remaining output channel to close.
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        foreach (var actor in actors.AsEnumerable().Reverse())
        {
            try
            {
                await actor.DisposeAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        try
        {
            await Runtime.DisposeAsync().AsTask().WaitAsync(DistributionPublicationLifecycleHostNames.Deadline);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
        if (ExpectCleanupFailure)
        {
            var flattened = new AggregateException(failures).Flatten().InnerExceptions;
            Check(flattened.Count > 0 && flattened.All(exception => exception is InvalidOperationException &&
                exception.Message == DistributionPublicationLifecycleHostNames.InjectedFailure));
        }
        else if (failures.Count != 0)
            throw new AggregateException(failures);
    }
}
