namespace Erlang.Tests;

internal static class ConnectedRpcHostTrace
{
    private const string PacketPrefix = "CONNECTED_RPC_RAW_PACKET ";
    private const string ControlPrefix = "CONNECTED_RPC_CONTROL ";
    private const string StatePrefix = "CONNECTED_RPC_CONNECTION ";
    private const string CleanupPrefix = "CONNECTED_RPC_FAILURE_CLEANUP ";

    internal static void Packet(string direction, byte[] packet)
        => Console.WriteLine(PacketPrefix + direction + " " + Convert.ToBase64String(packet));

    internal static void State(ConnectedSpawnHostEndpoint endpoint, Term control)
    {
        Console.WriteLine(ControlPrefix + control);
        Console.WriteLine(StatePrefix + endpoint.Runtime.DistributionPending.ConnectedSnapshot(endpoint.Handle!));
    }

    internal static async Task Forward(ConnectedSpawnHostEndpoint from, ConnectedSpawnHostEndpoint to)
    {
        byte[] packet = await from.TakePacket();
        Packet(from.Runtime.Node, packet);
        await to.PutPacket(packet);
    }

    internal static async Task CancelBlockedCaller(ConnectedSpawnHostEndpoint endpoint, RuntimeNodeFoundationHostActor caller)
    {
        try
        {
            if (!caller.Process.Completion.IsCompleted)
                await endpoint.Controller.Invoke(context =>
                {
                    endpoint.Runtime.Exit(context, caller.Process.Pid, Term.A(ConnectedSpawnHostNames.Kill));

                    return true;
                }).WaitAsync(ConnectedSpawnHostNames.Deadline);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(CleanupPrefix + error);
        }
    }
}
