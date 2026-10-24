namespace Erlang.Tests;

internal sealed class ConnectedSpawnHostEndpoint : IAsyncDisposable
{
    internal ProcessRuntime Runtime { get; } = new();
    internal RuntimeNodeFoundationHostActor Kernel { get; }
    internal RuntimeNodeFoundationHostActor Controller { get; private set; }
    internal TupleTerm? Handle { get; private set; }

    internal ConnectedSpawnHostEndpoint()
    {
        Kernel = new(Runtime);
        Controller = new(Runtime);
    }

    internal async Task Initialize(string node)
    {
        if (!Runtime.Modules.HasExport(ConnectedSpawnHostNames.Erlang, ConnectedSpawnHostNames.SetNode, 2))
            CoreModules.Register(Runtime.Modules);
        await Kernel.InvokeAsync(async context =>
        {
            Runtime.Register(ConnectedSpawnHostNames.Kernel, context.Self);

            return await Runtime.Modules.Call(
                context,
                ConnectedSpawnHostNames.Erlang,
                ConnectedSpawnHostNames.SetNode,
                Term.A(node),
                Term.I(0)
            );
        });
    }

    internal async Task Connect(ConnectedSpawnHostEndpoint remote, ulong flags = ConnectedSpawnHostNames.Flags)
    {
        Handle = Runtime.DistributionPending.BeginPending(Term.A(remote.Runtime.Node));
        await Controller.Invoke(context =>
        {
            Runtime.DistributionPending.AdoptConnected(
                Handle,
                context,
                flags,
                remote.Kernel.Process.Pid.Creation
            );
            Runtime.DistributionPending.SetConnectedInputHandler(Handle, context, context.Self);

            return true;
        });
    }

    internal async Task Reconnect(ConnectedSpawnHostEndpoint remote)
    {
        await Controller.DisposeAsync();
        Controller = new(Runtime);
        await Connect(remote);
    }

    internal async Task<byte[]> TakePacket()
    {
        using var deadline = new CancellationTokenSource(ConnectedSpawnHostNames.Deadline);
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            HostDistributionBuffer? buffer = await Controller.Invoke(context => Runtime.DistributionPending.TakeConnectedOutput(Handle!, context, out _));
            if (buffer is not null)
                return buffer.Segments.SelectMany(segment => segment.ToArray()).ToArray();
            await Task.Delay(ConnectedSpawnHostNames.PollMilliseconds, deadline.Token);
        }
    }

    internal Task<Term> PutPacket(byte[] packet)
        => Controller.Invoke(context => DistributionInputAdapter.PutData(context, Handle!, new BitString(packet)));

    public async ValueTask DisposeAsync()
    {
        await Controller.DisposeAsync();
        await Kernel.DisposeAsync();
        await Runtime.DisposeAsync();
    }
}
