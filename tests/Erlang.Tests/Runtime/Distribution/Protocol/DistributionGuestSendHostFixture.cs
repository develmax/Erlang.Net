using Erlang;
using System.Diagnostics;

namespace Erlang.Tests;

internal sealed class DistributionGuestSendHostFixture : IAsyncDisposable
{
    private DistributionGuestSendHostFixture(DistributionLifecycleHostFixture inner, ulong flags)
    {
        Inner = inner;
        Flags = flags;
    }

    internal DistributionLifecycleHostFixture Inner
    {
        get;
    }
    internal ProcessRuntime Runtime => Inner.Runtime;
    internal RuntimeNodeFoundationHostActor Producer => Inner.Producer;
    internal DistributionLifecycleHostConnection Connection { get; private set; } = null!;
    internal ulong Flags
    {
        get;
    }

    internal static async Task<DistributionGuestSendHostFixture> Create(ulong extraFlags = 0)
    {
        var inner = await DistributionLifecycleHostFixture.Create();
        var fixture = new DistributionGuestSendHostFixture(inner, DistributionPassThroughFlags.Mandatory | extraFlags);
        try
        {
            inner.Runtime.DistributionPending.CloseConnected(inner.Connection.Host);
            var controller = inner.CreateActor();
            Term host = inner.Runtime.DistributionPending.BeginPending(Term.A(inner.Connection.Remote.Node));
            Term opaque = await controller.InvokeAsync(async context => await DistributionControllerAdapters.SetNode(
                context,
                Term.A(inner.Connection.Remote.Node),
                context.Self,
                Term.Tuple(new Integer(fixture.Flags), Term.I(DistributionGuestSendHostNames.RemoteCreation))
            ));
            await controller.Invoke(context => DistributionControllerAdapters.SetInputHandler(context, opaque, context.Self));
            fixture.Connection = new(
                host,
                opaque,
                controller,
                inner.Connection.Remote
            );

            return fixture;
        }
        catch
        {
            await inner.DisposeAsync();
            throw;
        }
    }

    internal async Task<DistributionDecodedProcessPacket> Take()
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < DistributionGuestSendHostNames.Deadline)
        {
            var packet = await Connection.Controller.Invoke(context =>
                Runtime.DistributionPending.TakeConnectedOutput(Connection.Host, context, out _));
            if (packet is not null)
            {
                DistributionGuestSendHostChecks.Check(packet.SegmentCount == 1);

                return DistributionPassThroughCodec.DecodeProcessPacket(
                    packet.Segments[0].ToArray(),
                    negotiatedFlags: Flags,
                    allowAlternativePidSend: true
                );
            }
            await Task.Yield();
        }

        throw new TimeoutException(DistributionGuestSendHostNames.Failure);
    }

    internal void CheckEmpty() => DistributionGuestSendHostChecks.Check(
        Runtime.DistributionPending.ConnectedSnapshot(Connection.Host).QueuedPackets == 0
    );

    public async ValueTask DisposeAsync()
    {
        try
        {
            Runtime.DistributionPending.CloseConnected(Connection.Host);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or ObjectDisposedException)
        {
            // A deliberately retired connection or halted runtime has already closed this channel.
        }
        await Inner.DisposeAsync();
    }
}
