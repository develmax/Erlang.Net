namespace Erlang;

public sealed partial class ProcessContext
{
    internal BitString AppendGuestBinary(BitString prefix, BitString complete)
    {
        if (complete.BitLength == prefix.BitLength) return prefix;
        OwnedTermInvocation506? invocation = CurrentOwned506;
        if (invocation is not { CanBorrow507: true } || !invocation.OwnsContext507(this)
            || !RuntimeActorExecutionTurn.IsOwnedBy(this))
            return BitString.FromValidatedAppend(prefix, complete);
        Cancellation.ThrowIfCancellationRequested();
        byte[] bytes = complete.ToArray();
        BitString? reused = TermHeap506.AppendWritableBinary(
            invocation,
            prefix,
            bytes,
            complete.BitLength,
            Cancellation
        );
        if (reused is not null) return reused;
        BitString raw = BitString.FromValidatedAppend(prefix, complete);
        int slot = invocation.Frame.Reserve();
        try
        {
            var result = (BitString)invocation.Frame.AdoptGuestInto507(slot, raw);
            invocation.HoldGuestResult507(result);

            return result;
        }
        finally
        {
            invocation.Frame.Clear(slot);
        }
    }
}
