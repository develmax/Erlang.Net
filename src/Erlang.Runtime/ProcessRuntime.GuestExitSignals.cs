namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask SendGuestExitSignalAsync(
        ProcessContext sender,
        Term destination,
        Term reason,
        bool legacy,
        bool priority
    )
    {
        if (destination is not Pid remote || remote.Node == Node)
        {
            SendExitSignal(
                sender,
                destination,
                reason,
                legacy,
                priority
            );

            return;
        }
        // No local dictionary existence test can establish whether the remote recipient exists.
        _ = await DistributionPending.SendConnectedPidExitAsync(
            sender,
            remote,
            reason,
            priority
        ).ConfigureAwait(false);
    }
}
