using System.Numerics;

namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal async ValueTask<RuntimeMemoryPayload> CaptureMemoryPayloadAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        Mailbox[] mailboxes;
        BigInteger allocated, used;
        lock (gate)
        {
            allocated = processSlots.AllocatedPayloadBytes;
            used = processSlots.UsedPayloadBytes;
            mailboxes = processes.Values.Select(context => context.Mailbox).ToArray();
        }
        foreach (Mailbox mailbox in mailboxes)
        {
            cancellation.ThrowIfCancellationRequested();
            BigInteger payload = mailbox.CaptureOwnedPayloadBytes();
            allocated += payload;
            used += payload;
        }
        var native = await CaptureNativeAllocationsAsync(cancellation);

        return new(allocated, used, native.Allocations.BackingRequestBytes);
    }
}
